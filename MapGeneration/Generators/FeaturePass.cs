public class FeaturePass : IGenerationPass
{
    public string Name => "FeaturePass";

    public void Execute(GenerationContext ctx)
    {
        #region snow restoration
        RestoreSnow(ctx);
        #endregion

        #region shallow ocean
        MarkOceanShallow(ctx);
        #endregion

        #region beaches
        MarkBeaches(ctx);
        #endregion

        #region beach dark
        MarkBeachDark(ctx);
        #endregion
    }

    private void RestoreSnow(GenerationContext ctx)
    {
        const double snowThreshold = 0.78; // elevation above which mountain peaks get snow
        int w = ctx.width, h = ctx.height;

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                if (ctx.mapData[x, y] != TileId.Mountain && ctx.mapData[x, y] != TileId.MountainDeep) continue;
                if (ctx.elevation[x, y] <= snowThreshold) continue;
                ctx.mapData[x, y] = TileId.Snow;
            }
        }
    }

    private void MarkOceanShallow(GenerationContext ctx)
    {
        int[] ddx = { 0, 0, 1, -1 };
        int[] ddy = { 1, -1, 0, 0 };
        int w = ctx.width, h = ctx.height;

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                if (ctx.mapData[x, y] != TileId.Ocean) continue;
                bool nearLand = false;
                for (int d = 0; d < 4; d++)
                {
                    int nx = x + ddx[d], ny = y + ddy[d];
                    if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                    if (ctx.mapData[nx, ny] != TileId.Ocean && ctx.mapData[nx, ny] != TileId.Border)
                    {
                        nearLand = true;
                        break;
                    }
                }
                if (nearLand) ctx.mapData[x, y] = TileId.OceanShallow;
            }
        }
    }

    private void MarkBeaches(GenerationContext ctx)
    {
        int[] ddx = { 0, 0, 1, -1 };
        int[] ddy = { 1, -1, 0, 0 };
        int w = ctx.width, h = ctx.height;

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                if (ctx.mapData[x, y] != TileId.Plains && ctx.mapData[x, y] != TileId.Forest) continue;
                for (int d = 0; d < 4; d++)
                {
                    int nx = x + ddx[d], ny = y + ddy[d];
                    if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                    TileId n = ctx.mapData[nx, ny];
                    if (n == TileId.Ocean || n == TileId.OceanShallow || n == TileId.River || n == TileId.Lake || n == TileId.LakeShallow)
                    {
                        ctx.mapData[x, y] = TileId.Beach;
                        break;
                    }
                }
            }
        }
    }

    private void MarkBeachDark(GenerationContext ctx)
    {
        int[] ddx = { 0, 0, 1, -1 };
        int[] ddy = { 1, -1, 0, 0 };
        int w = ctx.width, h = ctx.height;

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                if (ctx.mapData[x, y] != TileId.Beach) continue;
                int count = 0;
                for (int d = 0; d < 4; d++)
                {
                    int nx = x + ddx[d], ny = y + ddy[d];
                    if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                    TileId n = ctx.mapData[nx, ny];
                    if (n == TileId.Beach || n == TileId.OceanShallow) count++;
                }
                if (count >= 3) ctx.mapData[x, y] = TileId.BeachDark;
            }
        }
    }
}
