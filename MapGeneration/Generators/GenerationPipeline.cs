using System.Diagnostics;

public class GenerationPipeline
{
    #region fields
    private List<IGenerationPass> passes { get; set; } = new();
    #endregion

    #region essential functions
    public void AddPass(IGenerationPass pass) => passes.Add(pass);

    public void Run(GenerationContext ctx)
    {
        foreach (IGenerationPass pass in passes)
        {
            Stopwatch sw = Stopwatch.StartNew();
            pass.Execute(ctx);
            sw.Stop();
            Map.outputBuffer.Add($"{pass.Name} — {sw.ElapsedMilliseconds}ms");
        }
    }
    #endregion
}
