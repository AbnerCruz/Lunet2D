# Gate — tipos inteiros e composição de parâmetros

Base: 56f0beb169c9f9bf74a51f1b53340f0d0f69221e. Escopo: fase inicial do estudo de 24/11/2026. A branch é candidata, não uma API publicada.

- [x] Inspecionar RectangleF, SpriteBatch, DebugDraw, Texture2D, GraphicsDevice, RenderTarget2D, referências Roslyn e projetos Android.
- [x] Implementar RectangleF(Vector2, Vector2), Draw de destino e preenchimento compartilhado.
- [x] Preparar sobrecargas Size, referência System.Drawing.Primitives e testes de compilação dinâmica e equivalência.
- [x] Atualizar exemplos e documentação XML.
- [ ] CI verde (net10.0 e build Android).
- [ ] Gerar e conferir referência de API distribuída.
- [ ] Verificar autocomplete e resolução dos símbolos na IDE.
- [ ] Executar Preview offline no Android, em projeto novo, incluindo o exemplo Player e Size/Point por alias; conferir toque, movimento, cor e recorte.
- [ ] Exportar APK do mesmo projeto, instalar e rodar offline; verificar dimensões, carregamento de metadados e renderização.
- [ ] Registrar aparelho, versão, revisão e resultado observado. Trimming é desativado nos hosts atuais; standalone também desativa AOT. Não alegar compatibilidade com modos não testados.
- [ ] Aprovar o gate antes de publicar as sobrecargas Size na main. Se falhar, investigar sem introduzir System.Drawing.Common.

A prioridade -1 das novas sobrecargas concorrentes mantém as chamadas antigas com default e new sem tipo. Para destino de textura, usar RectangleF explícito ou destination:. Nenhuma conversão implícita de Vector2 para int. GridPoint mantém seu domínio. Circle preenchido, Polygon preenchido, DrawOutline e novos tipos próprios ficam fora do escopo. Sem merge ou release antes de validação.
