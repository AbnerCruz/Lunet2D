# Primeiros passos

Todo jogo Lunet é uma classe que herda de `Game`. O Lunet chama seus métodos no tempo certo; você só escreve o que o jogo faz.

## O esqueleto

```csharp
using Lunet;
using Lunet.Graphics;

public class MeuJogo : Game
{
    protected override void Update(GameTime time)
    {
        // lógica: mover, colidir, pontuar
    }

    protected override void Draw(GameTime time)
    {
        Graphics.Clear(Color.CornflowerBlue);
    }
}
```

- `Update` roda em passo fixo, sempre com o mesmo intervalo, então a física é previsível.
- `Draw` roda uma vez por quadro exibido, e só desenha.
- Não guarde estado dentro de `Draw`.

## Um primeiro personagem

```csharp
using System.Numerics;
using Lunet;
using Lunet.Graphics;

public sealed class Player
{
    Vector2 pos = new(100, 100);
    readonly Vector2 size = new(32, 64);
    public RectangleF Bounds => new(pos, size);

    public void Update(Vector2 move) => pos += move;
    public void Draw(SpriteBatch batch) => batch.Draw(Bounds, Color.Red);
}
```

O jogo chama Update e Draw do personagem. Abra o SpriteBatch com Begin antes de desenhar e feche com End depois. Bounds é derivado da posição atual e serve também para colisões. Não é necessário criar uma textura individual para o retângulo.

## Dicas

- Toque em uma linha do editor e use o menu para abrir a documentação do símbolo.
- Erros em tempo de execução aparecem no console do jogo, sem fechar o app.
