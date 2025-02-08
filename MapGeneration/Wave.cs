public class Wave
{
    public List<(double x, double y)> Points { get; set; } = new List<(double x, double y)>();
    public double Direction { get; set; }
    public double Speed { get; set; }
    public double Curvature { get; set; }
    public int Length { get; set; }
    public double Intensity { get; set; } = 0.0;
    public HashSet<(int x, int y)> PreviousPoints { get; set; } = new HashSet<(int x, int y)>();
    public bool IsNight { get; set; }
    public bool IsDarkening { get; set; }
}