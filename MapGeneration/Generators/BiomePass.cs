public class BiomePass : IGenerationPass
{
    public string Name => "BiomePass";

    public void Execute(GenerationContext ctx)
    {
        #region biome assignment
        const double waterThreshold = 0.35;
        const double snowThreshold  = 0.78;

        for (int x = 0; x < ctx.width; x++)
        {
            for (int y = 0; y < ctx.height; y++)
            {
                double elev = ctx.elevation[x, y];
                if (elev < waterThreshold) { ctx.mapData[x, y] = TileId.Ocean; continue; }
                if (elev > snowThreshold)  { ctx.mapData[x, y] = TileId.Snow;  continue; }

                ctx.mapData[x, y] = (ctx.temperatureData[x, y], ctx.humidityData[x, y]) switch
                {
                    (5, >= 3) => TileId.Forest,
                    (4, >= 4) => TileId.Forest,
                    (3, >= 4) => TileId.Forest,
                    (2, 5)    => TileId.Forest,
                    _         => TileId.Plains,
                };
            }
        }
        #endregion

        #region boundary smoothing
        SmoothBiomes(ctx);
        #endregion
    }

    private void SmoothBiomes(GenerationContext ctx)
    {
        int w = ctx.width, h = ctx.height;
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                TileId cur = ctx.mapData[x, y];
                if (cur == TileId.Ocean || cur == TileId.Border) continue;

                int sameCount = 0;
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                        if (ctx.mapData[nx, ny] == cur) sameCount++;
                    }
                }
                if (sameCount >= 2) continue;
                ctx.mapData[x, y] = MajorityNeighbor(ctx, x, y);
            }
        }
    }

    private TileId MajorityNeighbor(GenerationContext ctx, int x, int y)
    {
        int w = ctx.width, h = ctx.height;
        int plains = 0, forest = 0, ocean = 0, snow = 0;
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = x + dx, ny = y + dy;
                if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                switch (ctx.mapData[nx, ny])
                {
                    case TileId.Plains: plains++; break;
                    case TileId.Forest: forest++; break;
                    case TileId.Ocean:  ocean++;  break;
                    case TileId.Snow:   snow++;   break;
                }
            }
        }
        if (forest >= plains && forest >= ocean && forest >= snow) return TileId.Forest;
        if (plains >= ocean  && plains >= snow)                    return TileId.Plains;
        if (ocean  >= snow)                                        return TileId.Ocean;
        return TileId.Snow;
    }
}
