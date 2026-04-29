public class Wave
{
    public List<(double x, double y)> Points { get; set; } = [];
    public double Direction { get; set; }
    public double Speed { get; set; }
    public double Curvature { get; set; }
    public int Length { get; set; }
    public double Intensity { get; set; } = 0.0;
}