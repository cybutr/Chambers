public class NoiseStack
{
    #region essential functions
    public double[,] Generate(GenerationContext ctx)
    {
        float noiseScale = (float)ctx.conf.NoiseScale;
        double[,] continental = Perlin.GenerateSimplexFBM(ctx.width, ctx.height, noiseScale * 1.0f, 4, 0.5f, 2.0f, ctx.rng.Next());
        double[,] regional    = Perlin.GenerateSimplexFBM(ctx.width, ctx.height, noiseScale * 0.3f, 3, 0.5f, 2.0f, ctx.rng.Next());
        double[,] detail      = Perlin.GenerateSimplexFBM(ctx.width, ctx.height, noiseScale * 0.1f, 2, 0.5f, 2.0f, ctx.rng.Next());

        double[,] result = new double[ctx.width, ctx.height];
        double min = double.MaxValue;
        double max = double.MinValue;

        for (int x = 0; x < ctx.width; x++)
        {
            for (int y = 0; y < ctx.height; y++)
            {
                double v = continental[x, y] * 0.5 + regional[x, y] * 0.3 + detail[x, y] * 0.2;
                result[x, y] = v;
                if (v < min) min = v;
                if (v > max) max = v;
            }
        }

        double range = max - min;
        if (range < 1e-10) return result;

        for (int x = 0; x < ctx.width; x++)
            for (int y = 0; y < ctx.height; y++)
                result[x, y] = (result[x, y] - min) / range;

        return result;
    }
    #endregion
}
