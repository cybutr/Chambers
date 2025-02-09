public class Weather
{
    public WeatherType CurrentWeather { get; set; }
    public WeatherType NextWeather { get; set; }
    public double Intensity { get; set; }
    public double IntensityTarget { get; set; }
    public double IntensityChangeSpeed { get; set; }
    public double Temperature { get; set; }
    public double Humidity { get; set; }
    public double Pressure { get; set; }
    public double WindSpeed { get; set; }
    public double WindDirection { get; set; }
    public double TimeOfDay { get; set; }
    public double Season { get; set; }
}
public enum WeatherType
{
    Clear = 1,
    Rain = 2,
    Snow = 3,
    Thunderstorm = 4,
    Fog = 5,
    Overcast = 6,
    Hail = 7,
    Sleet = 8,
    Drizzle = 9,
    BlowingSnow = 10,
    Sandstorm = 11
}
public enum CloudType
{
    Cumulus = 1,
    Stratus = 2,
    Cirrus = 3,
    Cumulonimbus = 4,
    Nimbostratus = 5,
    Altocumulus = 6
}
public class CloudLayer
{
    public double BaseAltitude { get; set; }
    public CloudType Type { get; set; }
    public double Coverage { get; set; } // 0-1 cloud coverage
    public bool IsEnabled { get; set; }
}