public class Cloud
{
    public int X { get; set; }
    public int Y { get; set; }
    public double Speed { get; set; }
    public double Direction { get; set; }
    public double Precipitation { get; set; }
    public CloudType Type { get; set; }

    public void Move()
    {
        X += (int)(Speed * Math.Cos(Direction));
        Y += (int)(Speed * Math.Sin(Direction));
    }
}