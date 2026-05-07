public class FramePass : IGenerationPass
{
    public string Name => "FramePass";

    public void Execute(GenerationContext ctx)
    {
        #region border frame
        int w = ctx.width, h = ctx.height;

        for (int x = 0; x < w; x++)
        {
            ctx.mapData[x, 0]     = TileId.Border;
            ctx.mapData[x, h - 1] = TileId.Border;
        }
        for (int y = 0; y < h; y++)
        {
            ctx.mapData[0,     y] = TileId.Border;
            ctx.mapData[w - 1, y] = TileId.Border;
        }
        #endregion
    }
}
