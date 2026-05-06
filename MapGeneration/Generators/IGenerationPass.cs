public interface IGenerationPass
{
    string Name { get; }
    void Execute(GenerationContext ctx);
}
