public class GenerationContext
{
    #region fields
    public int width { get; set; }
    public int height { get; set; }
    public TileId[,] mapData { get; set; }
    public double[,] elevation { get; set; }
    public int[,] temperatureData { get; set; }
    public int[,] humidityData { get; set; }
    public double[,] noise { get; set; }
    public double[,] tempatureNoise { get; set; }
    public double[,] humidityNoise { get; set; }
    public Random rng { get; set; }
    public Config conf { get; set; }
    #endregion
}
