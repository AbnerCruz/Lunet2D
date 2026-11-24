using System.Numerics;
using Lunet.Compiler;
using Lunet.Graphics;
using Size = System.Drawing.Size;
using Point = System.Drawing.Point;

namespace Lunet.Tests;

[Collection("Frame allocation")]
public class ComposedGraphicsTests
{
    [Fact]
    public void RectanglePreservesFractionalCoordinatesAndOldGeometry()
    {
        var pos = new Vector2(-2.25f, 3.5f);
        var size = new Vector2(12.75f, 4.125f);
        RectangleF rect = new(pos, size);
        Assert.Equal(new RectangleF(pos.X, pos.Y, size.X, size.Y), rect);
        Assert.Equal(pos, rect.Position);
        Assert.Equal(size, rect.Size);
        Assert.Equal(default(RectangleF), new RectangleF(default, default));
        Assert.True(rect.Contains(pos));
    }

    [Fact]
    public void SizeOverloadsPreservePixelsFiltersAndResolution()
    {
        var backend = new RecordingBackend();
        var device = new GraphicsDevice(backend, 100, 100);
        var size = new Size(3, 2);
        using var scalar = Texture2D.CreateSolid(device, 3, 2, Color.Red);
        using var grouped = Texture2D.CreateSolid(device, size, Color.Red);
        Assert.Equal((scalar.Width, scalar.Height, scalar.Filter), (grouped.Width, grouped.Height, grouped.Filter));
        Assert.Equal(backend.Pixels[scalar.Handle].Rgba, backend.Pixels[grouped.Handle].Rgba);
        var pixels = Enumerable.Range(0, 24).Select(x => (byte)x).ToArray();
        using var a = Texture2D.FromPixels(device, 3, 2, pixels, TextureFilter.Point);
        using var b = Texture2D.FromPixels(device, size, pixels, filter: TextureFilter.Point);
        Assert.Equal(backend.Pixels[a.Handle].Rgba, backend.Pixels[b.Handle].Rgba);
        Assert.Equal(a.Filter, b.Filter);
        using var t1 = new RenderTarget2D(device, 3, 2, TextureFilter.Point);
        using var t2 = new RenderTarget2D(device, size, TextureFilter.Point);
        Assert.Equal((t1.Width, t1.Height, t1.Texture.Filter), (t2.Width, t2.Height, t2.Texture.Filter));
        device.Resize(300, 200);
        device.SetVirtualResolution(120, 80);
        var viewport = device.Viewport;
        device.SetVirtualResolution(new Size(120, 80));
        Assert.Equal(viewport, device.Viewport);
        Assert.Equal(120, device.VirtualWidth);
        Assert.Equal(80, device.VirtualHeight);
        var point = new Point(1, 2);
        Assert.Equal(1, point.X);
    }

    private static void SameFailure(Action scalar, Action grouped)
    {
        var a = Record.Exception(scalar);
        var b = Record.Exception(grouped);
        Assert.NotNull(a);
        Assert.NotNull(b);
        Assert.Equal(a.GetType(), b.GetType());
        Assert.Equal(a.Message, b.Message);
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(-1, 2)]
    [InlineData(2, -1)]
    [InlineData(int.MaxValue, 2)]
    public void SizeOverloadsPreserveValidation(int w, int h)
    {
        var device = new GraphicsDevice(new RecordingBackend(), 100, 100);
        var size = new Size(w, h);
        SameFailure(() => Texture2D.CreateSolid(device, w, h, Color.Red), () => Texture2D.CreateSolid(device, size, Color.Red));
        SameFailure(() => Texture2D.FromPixels(device, w, h, Array.Empty<byte>()), () => Texture2D.FromPixels(device, size, Array.Empty<byte>()));
        SameFailure(() => new RenderTarget2D(device, w, h), () => new RenderTarget2D(device, size));
        if (w <= 0 || h <= 0)
            SameFailure(() => device.SetVirtualResolution(w, h), () => device.SetVirtualResolution(size));
    }

    [Fact]
    public void DrawMatchesCanonicalPathWithCameraClipAndInterleavedTextures()
    {
        var backend = new RecordingBackend();
        var device = new GraphicsDevice(backend, 100, 100);
        device.Resize(200, 200);
        using var texture = Texture2D.CreateSolid(device, 4, 4, Color.White);
        var batch = new SpriteBatch(device);
        var camera = new Camera2D { Position = new(20, 40), Zoom = 1.5f };
        var rect = new RectangleF(new Vector2(5, 8), new Vector2(16, 20));
        var floor = new RectangleF(0, 60, 100, 40);
        void Render(bool composed)
        {
            batch.Begin(camera, blend: BlendState.Additive, sampler: SamplerState.PointClamp, clip: new RectangleF(0, 0, 80, 80));
            if (composed)
            {
                batch.Draw(floor, Color.Green);
                batch.Draw(rect, Color.Red);
                batch.Draw(texture, rect, Color.White);
                batch.Draw(rect, Color.Blue);
            }
            else
            {
                batch.Draw(device.WhiteTexture, floor, null, Color.Green, 0f, Vector2.Zero);
                batch.FillRect(rect, Color.Red);
                batch.Draw(texture, rect, null, Color.White, 0f, Vector2.Zero);
                batch.Draw(device.WhiteTexture, rect, null, Color.Blue, 0f, Vector2.Zero);
            }
            batch.End();
        }
        Render(false);
        var expected = backend.Batches.ToArray();
        var states = backend.States.ToArray();
        var textures = backend.LiveTextures.Count;
        backend.Batches.Clear();
        backend.States.Clear();
        Render(true);
        Assert.Equal(3, expected.Length);
        Assert.Equal(expected.Length, backend.Batches.Count);
        Assert.Equal(states, backend.States.ToArray());
        Assert.Equal(textures, backend.LiveTextures.Count);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i].Texture, backend.Batches[i].Texture);
            Assert.Equal(expected[i].QuadCount, backend.Batches[i].QuadCount);
            Assert.Equal(expected[i].Vertices, backend.Batches[i].Vertices);
        }
        Assert.Throws<InvalidOperationException>(() => batch.Draw(rect, Color.Red));
    }

    [Fact]
    public void FilledRectangleDoesNotAllocateAfterWarmup()
    {
        var device = new GraphicsDevice(new NoOpBackend(), 100, 100);
        var batch = new SpriteBatch(device);
        var rect = new RectangleF(2, 3, 4, 5);
        using var texture = Texture2D.CreateSolid(device, 4, 4, Color.White);
        void Frame()
        {
            batch.Begin();
            batch.Draw(rect, Color.Red);
            batch.Draw(texture, rect, Color.White);
            batch.End();
        }
        for (int i = 0; i < 100; i++) Frame();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++) Frame();
        Assert.Equal(0L, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    [Fact]
    public void DynamicCompilerAcceptsAliasesAndOldAndNewOverloads()
    {
        var compiler = new GameCompiler(new LoadedAssembliesReferenceProvider(typeof(Game).Assembly));
        var source = """
            using System.Numerics;
            using Lunet;
            using Lunet.Graphics;
            using Size = System.Drawing.Size;
            using Point = System.Drawing.Point;
            public sealed class Player
            {
                Vector2 pos = new(100, 100);
                readonly Vector2 size = new(32, 64);
                RectangleF rect => new(pos, size);
                public void Update(Vector2 move) => pos += move;
                public void Draw(SpriteBatch batch) => batch.Draw(rect, Color.Red);
                public void Check(GraphicsDevice device, SpriteBatch batch, Texture2D texture)
                {
                    Size pixels = new(2, 3);
                    Point point = new(1, 2);
                    using var solid = Texture2D.CreateSolid(device, pixels, Color.Red);
                    using var old = Texture2D.CreateSolid(device, 2, 3, Color.Red);
                    using var raw = Texture2D.FromPixels(device, size: pixels, rgba: new byte[24]);
                    using var oldRaw = Texture2D.FromPixels(device, 2, 3, new byte[24]);
                    using var target = new RenderTarget2D(device, pixels, filter: TextureFilter.Point);
                    using var oldTarget = new RenderTarget2D(device, 2, 3);
                    device.SetVirtualResolution(pixels);
                    device.SetVirtualResolution(2, 3);
                    batch.Draw(texture, new Vector2(point.X, point.Y), Color.White);
                    batch.Draw(texture, default, Color.White);
                    batch.Draw(texture, new(2, 3), Color.White);
                    batch.Draw(texture, destination: new(1, 2, 3, 4), color: Color.White);
                    batch.Draw(texture, rect, Color.White);
                    batch.FillRect(rect, Color.Red);
                    batch.Draw(rect, Color.Red);
                    batch.Draw(texture, position: default, color: Color.White);
                }
            }
            """;
        var result = compiler.Compile("ComposedGraphicsProbe", [new SourceFile("Player.cs", source)]);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
    }
}
