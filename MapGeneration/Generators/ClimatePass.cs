public class ClimatePass : IGenerationPass
{
    public string Name => "ClimatePass";

    public void Execute(GenerationContext ctx)
    {
        #region coast distance BFS
        int[,] dist = ComputeCoastDistance(ctx);
        #endregion

        #region zone assignment
        int maxDist = Math.Max(15, Math.Min(ctx.width, ctx.height) / 4);
        double halfH = Math.Max(1.0, ctx.height / 2.0);

        for (int x = 0; x < ctx.width; x++)
        {
            for (int y = 0; y < ctx.height; y++)
            {
                double lat     = Math.Abs(y - halfH) / halfH;
                double rawTemp = Math.Clamp(1.0 - lat * 0.7 - ctx.elevation[x, y] * 0.3, 0.0, 1.0);
                ctx.temperatureData[x, y] = rawTemp switch
                {
                    < 0.2 => 1,
                    < 0.4 => 2,
                    < 0.6 => 3,
                    < 0.8 => 4,
                    _     => 5,
                };

                double coastInfluence = 1.0 - Math.Clamp(dist[x, y] / (double)maxDist, 0.0, 1.0);
                ctx.humidityData[x, y] = coastInfluence switch
                {
                    > 0.8 => 5,
                    > 0.6 => 4,
                    > 0.4 => 3,
                    > 0.2 => 2,
                    _     => 1,
                };
            }
        }
        #endregion
    }

    private int[,] ComputeCoastDistance(GenerationContext ctx)
    {
        const double waterThreshold = 0.35;
        int[,] dist = new int[ctx.width, ctx.height];
        for (int x = 0; x < ctx.width; x++)
            for (int y = 0; y < ctx.height; y++)
                dist[x, y] = int.MaxValue;

        Queue<(int x, int y)> queue = new();

        for (int x = 0; x < ctx.width; x++)
        {
            for (int y = 0; y < ctx.height; y++)
            {
                if (ctx.elevation[x, y] >= waterThreshold) continue;
                dist[x, y] = 0;
                queue.Enqueue((x, y));
            }
        }

        int[] dx = [0, 0, 1, -1];
        int[] dy = [1, -1, 0, 0];

        while (queue.Count > 0)
        {
            (int cx, int cy) = queue.Dequeue();
            for (int i = 0; i < 4; i++)
            {
                int nx = cx + dx[i];
                int ny = cy + dy[i];
                if (nx < 0 || nx >= ctx.width || ny < 0 || ny >= ctx.height) continue;
                if (dist[nx, ny] != int.MaxValue) continue;
                dist[nx, ny] = dist[cx, cy] + 1;
                queue.Enqueue((nx, ny));
            }
        }

        return dist;
    }
}
