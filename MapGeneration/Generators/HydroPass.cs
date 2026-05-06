public class HydroPass : IGenerationPass
{
    public string Name => "HydroPass";

    public void Execute(GenerationContext ctx)
    {
        #region river sources
        int w = ctx.width, h = ctx.height;
        List<(int x, int y)> candidates = [];

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                double elev = ctx.elevation[x, y];
                if (elev < 0.50 || elev > 0.72) continue;
                TileId t = ctx.mapData[x, y];
                if (t == TileId.Ocean || t == TileId.Mountain || t == TileId.MountainDeep || t == TileId.Snow || t == TileId.Border) continue;
                candidates.Add((x, y));
            }
        }

        if (candidates.Count == 0) return;

        int sourceCount = Math.Max(3, ctx.width * ctx.height / 800);

        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = ctx.rng.Next(i + 1);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }
        #endregion

        #region river tracing
        int take = Math.Min(sourceCount, candidates.Count);
        for (int i = 0; i < take; i++)
            TraceRiver(ctx, candidates[i].x, candidates[i].y);

        MarkShallowEdges(ctx, TileId.River, TileId.RiverShallow);
        MarkShallowEdges(ctx, TileId.Lake, TileId.LakeShallow);
        #endregion
    }

    private void TraceRiver(GenerationContext ctx, int sx, int sy)
    {
        int w = ctx.width, h = ctx.height;
        HashSet<(int, int)> visited = [];
        int[] ddx = { 0, 0, 1, -1 };
        int[] ddy = { 1, -1, 0, 0 };
        int cx = sx, cy = sy;
        ctx.mapData[cx, cy] = TileId.River;

        for (int iter = 0; iter < w + h; iter++)
        {
            if (ctx.mapData[cx, cy] == TileId.Ocean || ctx.mapData[cx, cy] == TileId.Border) break;
            visited.Add((cx, cy));

            int bestNx = -1, bestNy = -1;
            double lowestElev = double.MaxValue;

            for (int i = 0; i < 4; i++)
            {
                int nx = cx + ddx[i], ny = cy + ddy[i];
                if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                if (visited.Contains((nx, ny))) continue;
                if (ctx.elevation[nx, ny] < lowestElev)
                {
                    lowestElev = ctx.elevation[nx, ny];
                    bestNx = nx;
                    bestNy = ny;
                }
            }

            if (bestNx == -1 || lowestElev >= ctx.elevation[cx, cy])
            {
                FormLake(ctx, cx, cy);
                return;
            }

            if (ctx.mapData[bestNx, bestNy] == TileId.Ocean || ctx.mapData[bestNx, bestNy] == TileId.Border) break;
            ctx.mapData[bestNx, bestNy] = TileId.River;
            cx = bestNx;
            cy = bestNy;
        }
    }

    private void FormLake(GenerationContext ctx, int x, int y)
    {
        int w = ctx.width, h = ctx.height;
        const int lakeRadius = 5; // max BFS radius from river termination point
        Queue<(int, int)> queue = new();
        HashSet<(int, int)> lakeVisited = [];
        int[] ddx = { 0, 0, 1, -1 };
        int[] ddy = { 1, -1, 0, 0 };

        queue.Enqueue((x, y));
        lakeVisited.Add((x, y));

        while (queue.Count > 0)
        {
            (int cx, int cy) = queue.Dequeue();
            if (ctx.mapData[cx, cy] != TileId.Ocean && ctx.mapData[cx, cy] != TileId.Border && ctx.mapData[cx, cy] != TileId.River)
                ctx.mapData[cx, cy] = TileId.Lake;

            for (int i = 0; i < 4; i++)
            {
                int nx = cx + ddx[i], ny = cy + ddy[i];
                if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                if (lakeVisited.Contains((nx, ny))) continue;
                if (ctx.mapData[nx, ny] == TileId.Ocean || ctx.mapData[nx, ny] == TileId.Border) continue;
                if (Math.Sqrt((nx - x) * (nx - x) + (ny - y) * (ny - y)) > lakeRadius) continue;
                lakeVisited.Add((nx, ny));
                queue.Enqueue((nx, ny));
            }
        }
    }

    private void MarkShallowEdges(GenerationContext ctx, TileId waterTile, TileId shallowTile)
    {
        int w = ctx.width, h = ctx.height;
        int[] ddx = { 0, 0, 1, -1 };
        int[] ddy = { 1, -1, 0, 0 };

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                if (ctx.mapData[x, y] != waterTile) continue;
                for (int i = 0; i < 4; i++)
                {
                    int nx = x + ddx[i], ny = y + ddy[i];
                    if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                    TileId nt = ctx.mapData[nx, ny];
                    if (nt != waterTile && nt != shallowTile && nt != TileId.Ocean && nt != TileId.Border)
                    {
                        ctx.mapData[x, y] = shallowTile;
                        break;
                    }
                }
            }
        }
    }
}
