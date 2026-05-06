public class MountainPass : IGenerationPass
{
    public string Name => "MountainPass";

    public void Execute(GenerationContext ctx)
    {
        #region mountain threshold
        const double mountainThreshold = 0.65; // tiles at this elevation and above become Mountain
        int w = ctx.width, h = ctx.height;

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                if (ctx.mapData[x, y] == TileId.Border) continue;
                if (ctx.mapData[x, y] == TileId.Ocean) continue;
                if (ctx.elevation[x, y] < mountainThreshold) continue;
                ctx.mapData[x, y] = TileId.Mountain;
            }
        }
        #endregion

        #region mountain deep
        MarkMountainDeep(ctx);
        #endregion
    }

    private void MarkMountainDeep(GenerationContext ctx)
    {
        int w = ctx.width, h = ctx.height;
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                if (ctx.mapData[x, y] != TileId.Mountain) continue;
                int neighborCount = 0;
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                        if (ctx.mapData[nx, ny] == TileId.Mountain || ctx.mapData[nx, ny] == TileId.Snow) neighborCount++;
                    }
                }
                if (neighborCount >= 7) ctx.mapData[x, y] = TileId.MountainDeep;
            }
        }
    }
}
