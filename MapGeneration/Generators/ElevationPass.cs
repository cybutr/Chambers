public class ElevationPass : IGenerationPass
{
    public string Name => "ElevationPass";
    public void Execute(GenerationContext ctx)
    {
        ctx.elevation = new NoiseStack().Generate(ctx);
        ctx.noise = ctx.elevation;
    }
}
