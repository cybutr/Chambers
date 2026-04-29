using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Internal;
using static Internal.GUI;

public partial class Map
{
    #region essentials
    public Weather weather {get; set;} = new Weather();
    public double deltaTime {get; set;} = 0.5;
    [JsonIgnore] private double _gustTimer { get; set; } = 0.0;
    [JsonIgnore] private double _gustStrength { get; set; } = 0.0;
    [JsonIgnore] private double _weatherFactor { get; set; } = 40.0;
    #endregion
    #region weather state
    public void InitializeWeather()
    {
        int weatherRng = rng.Next(0, 2);
        weather.CurrentWeather = weatherRng switch
        {
            0 => WeatherType.Clear,
            1 => WeatherType.Overcast,
            _ => WeatherType.Clear
        };
        weather.NextWeather = GetSeasonWeatherType();
        weather.Intensity = 0.0;
        weather.IntensityTarget = 0.0;
        weather.IntensityChangeSpeed = 0.1;
        dayNight = new DayNightCycle { TimeOfDay = 12.0, Season = rng.NextDouble() * 4.0 };
        weather.Temperature = GetTemperature(dayNight.Season, dayNight.TimeOfDay, weather.CurrentWeather, avarageTempature);
        weather.Humidity = GetHumidity(weather.CurrentWeather, weather.Temperature, dayNight.TimeOfDay, dayNight.Season, avarageHumidity);
        weather.Pressure = GetPressure();
        weather.WindSpeed = rng.NextDouble() * 12.0 + 10.0;
        weather.WindDirection = rng.Next(361);
    }
    public void UpdateWeather()
    {
        if (conf.DoTimeCycle) dayNight.Advance(deltaTime);
        weather.Intensity += (weather.IntensityTarget - weather.Intensity) * weather.IntensityChangeSpeed * deltaTime;

        if (ShouldChangeWeather())
        {
            weather.CurrentWeather = weather.NextWeather;
            weather.NextWeather = GetSeasonWeatherType();
            weather.IntensityTarget = rng.NextDouble();
            InitializeMinTimeBetweenChanges();
        }

        weather.Temperature = GetTemperature(dayNight.Season, dayNight.TimeOfDay, weather.CurrentWeather, avarageTempature);
        weather.Humidity = GetHumidity(weather.CurrentWeather, weather.Temperature, dayNight.TimeOfDay, dayNight.Season, avarageHumidity);
        weather.Pressure = GetPressure();
        UpdateWind();
        UpdateCloudShadows();
        UpdateTime();
        UpdateSeason();
    }
    public double TimeSinceLastWeatherChange {get; set;} = 0.0;
    public double MinTimeBetweenChanges {get; set;}
    private WeatherType GetSeasonWeatherType()
    {
        double temperature = weather.Temperature;
        double humidity    = weather.Humidity;
        double pressure    = weather.Pressure;
        double season      = dayNight.Season;

        WeatherType weatherType = WeatherType.Clear;

        Dictionary<WeatherType, double> weatherProbabilities = new()
        {
            { WeatherType.Clear,       0.30 },
            { WeatherType.Rain,        0.10 },
            { WeatherType.Snow,        0.10 },
            { WeatherType.Thunderstorm,0.05 },
            { WeatherType.Fog,         0.05 },
            { WeatherType.Overcast,    0.10 },
            { WeatherType.Hail,        0.05 },
            { WeatherType.Sleet,       0.05 },
            { WeatherType.Drizzle,     0.10 },
            { WeatherType.BlowingSnow, 0.05 },
            { WeatherType.Sandstorm,   0.05 }
        };

        if (temperature < 0)
        {
            weatherProbabilities[WeatherType.Snow]        += 0.3;
            weatherProbabilities[WeatherType.BlowingSnow] += 0.1;
            weatherProbabilities[WeatherType.Rain]        -= 0.1;
            weatherProbabilities[WeatherType.Thunderstorm]-= 0.05;
        }
        else if (temperature > 25)
        {
            weatherProbabilities[WeatherType.Thunderstorm] += 0.1;
            weatherProbabilities[WeatherType.Sandstorm]    += 0.1;
        }
        else if (temperature > 15)
        {
            weatherProbabilities[WeatherType.Rain]         += 0.1;
            weatherProbabilities[WeatherType.Thunderstorm] += 0.05;
        }

        if (humidity > 80)
        {
            weatherProbabilities[WeatherType.Rain]    += 0.2;
            weatherProbabilities[WeatherType.Drizzle] += 0.1;
            weatherProbabilities[WeatherType.Fog]     += 0.1;
            weatherProbabilities[WeatherType.Overcast] += 0.1;
        }
        else if (humidity < 30)
        {
            weatherProbabilities[WeatherType.Clear]     += 0.1;
            weatherProbabilities[WeatherType.Sandstorm] += 0.1;
        }

        if (pressure < 1000)
        {
            weatherProbabilities[WeatherType.Rain]        += 0.1;
            weatherProbabilities[WeatherType.Thunderstorm]+= 0.1;
            weatherProbabilities[WeatherType.Hail]        += 0.05;
            weatherProbabilities[WeatherType.Overcast]    += 0.1;
        }
        else if (pressure > 1020) weatherProbabilities[WeatherType.Clear] += 0.2;

        if (season < 1.0)
        {
            weatherProbabilities[WeatherType.Rain]    += 0.1;
            weatherProbabilities[WeatherType.Drizzle] += 0.1;
        }
        else if (season < 2.0)
        {
            weatherProbabilities[WeatherType.Thunderstorm] += 0.1;
            weatherProbabilities[WeatherType.Clear]        += 0.1;
        }
        else if (season < 3.0)
        {
            weatherProbabilities[WeatherType.Overcast] += 0.1;
            weatherProbabilities[WeatherType.Fog]      += 0.1;
        }
        else
        {
            weatherProbabilities[WeatherType.Snow]        += 0.2;
            weatherProbabilities[WeatherType.BlowingSnow] += 0.1;
            weatherProbabilities[WeatherType.Sleet]       += 0.1;
        }

        switch (weather.CurrentWeather)
        {
            case WeatherType.Rain:
                weatherProbabilities[WeatherType.Thunderstorm] += 0.2;
                weatherProbabilities[WeatherType.Rain]         += 0.1;
                weatherProbabilities[WeatherType.Drizzle]      += 0.05;
                break;
            case WeatherType.Thunderstorm:
                weatherProbabilities[WeatherType.Rain]  += 0.1;
                weatherProbabilities[WeatherType.Clear] += 0.05;
                break;
            case WeatherType.Clear:
                weatherProbabilities[WeatherType.Clear] += 0.1;
                weatherProbabilities[WeatherType.Rain]  += 0.05;
                break;
            case WeatherType.Overcast:
                weatherProbabilities[WeatherType.Rain]  += 0.1;
                weatherProbabilities[WeatherType.Clear] += 0.05;
                break;
            case WeatherType.Snow:
                weatherProbabilities[WeatherType.Snow]        += 0.2;
                weatherProbabilities[WeatherType.BlowingSnow] += 0.1;
                break;
            case WeatherType.BlowingSnow:
                weatherProbabilities[WeatherType.Snow]        += 0.1;
                weatherProbabilities[WeatherType.BlowingSnow] += 0.1;
                break;
            case WeatherType.Fog:
                weatherProbabilities[WeatherType.Fog]   += 0.1;
                weatherProbabilities[WeatherType.Clear] += 0.05;
                break;
        }

        foreach (WeatherType key in weatherProbabilities.Keys.ToArray())
            if (weatherProbabilities[key] < 0) weatherProbabilities[key] = 0;

        double totalProbability = weatherProbabilities.Values.Sum();
        if (totalProbability == 0)
        {
            weatherProbabilities = new()
            {
                { WeatherType.Clear,       0.30 },
                { WeatherType.Rain,        0.10 },
                { WeatherType.Snow,        0.10 },
                { WeatherType.Thunderstorm,0.05 },
                { WeatherType.Fog,         0.05 },
                { WeatherType.Overcast,    0.10 },
                { WeatherType.Hail,        0.05 },
                { WeatherType.Sleet,       0.05 },
                { WeatherType.Drizzle,     0.10 },
                { WeatherType.BlowingSnow, 0.05 },
                { WeatherType.Sandstorm,   0.05 }
            };
            totalProbability = weatherProbabilities.Values.Sum();
        }

        double rand = rng.NextDouble() * totalProbability;
        double cumulative = 0.0;

        foreach (KeyValuePair<WeatherType, double> pair in weatherProbabilities)
        {
            cumulative += pair.Value;
            if (rand <= cumulative)
            {
                weatherType = pair.Key;
                break;
            }
        }

        return weatherType;
    }
    private void InitializeMinTimeBetweenChanges()
    {
        MinTimeBetweenChanges = weather.CurrentWeather switch
        {
            WeatherType.Clear       => 95.0,
            WeatherType.Rain        => 125.0,
            WeatherType.Snow        => 255.0,
            WeatherType.Thunderstorm=> 260.0,
            WeatherType.Fog         => 140.0,
            WeatherType.Overcast    => 90.0,
            WeatherType.Hail        => 90.0,
            WeatherType.Sleet       => 120.0,
            WeatherType.Drizzle     => 105.0,
            WeatherType.BlowingSnow => 95.0,
            WeatherType.Sandstorm   => 65.0,
            _ => 115.0
        };
    }
    private bool ShouldChangeWeather()
    {
        TimeSinceLastWeatherChange += deltaTime;
        if (TimeSinceLastWeatherChange < MinTimeBetweenChanges) return false;
        double changeProbability = 0.01 * deltaTime * 10;
        if (rng.NextDouble() < changeProbability)
        {
            TimeSinceLastWeatherChange = 0.0;
            return true;
        }
        return false;
    }
    private double GetHumidity(WeatherType weatherType, double tempature, double timeOfDay, double season, double avarageHumidity)
    {
        double seasonFactor   = Math.Cos(season / 2.0 * Math.PI);
        double timeFactor     = Math.Cos(timeOfDay / 24.0 * 2 * Math.PI);
        double tempatureFactor= Math.Cos(tempature / 40.0 * Math.PI);

        double baseHumidity = weatherType switch
        {
            WeatherType.Clear       => 50.0,
            WeatherType.Rain        => 60.0,
            WeatherType.Snow        => 50.0,
            WeatherType.Thunderstorm=> 70.0,
            WeatherType.Fog         => 75.0,
            WeatherType.Overcast    => 70.0,
            WeatherType.Hail        => 60.0,
            WeatherType.Sleet       => 50.0,
            WeatherType.Drizzle     => 55.0,
            WeatherType.BlowingSnow => 40.0,
            WeatherType.Sandstorm   => 40.0,
            _ => 50.0
        };
        baseHumidity += (avarageHumidity - 0.5) * 10;

        double weatherTimer = weatherType switch
        {
            WeatherType.Clear       => rng.NextDouble() * 2.0,
            WeatherType.Rain        => rng.NextDouble() * 3.5,
            WeatherType.Snow        => rng.NextDouble() * 3.0,
            WeatherType.Thunderstorm=> rng.NextDouble() * 4.0,
            WeatherType.Fog         => rng.NextDouble() * 4.5,
            WeatherType.Overcast    => rng.NextDouble() * 3.0,
            WeatherType.Hail        => rng.NextDouble() * 3.0,
            WeatherType.Sleet       => rng.NextDouble() * 3.0,
            WeatherType.Drizzle     => rng.NextDouble() * 3.0,
            WeatherType.BlowingSnow => rng.NextDouble() * 3.0,
            WeatherType.Sandstorm   => rng.NextDouble() * 3.0,
            _ => 3.0
        };

        if (_weatherFactor < baseHumidity) _weatherFactor += weatherTimer;
        else if (_weatherFactor > baseHumidity + 1) _weatherFactor -= weatherTimer;

        double humidity = _weatherFactor + seasonFactor + timeFactor + tempatureFactor;
        return Math.Clamp(humidity, 0.0, 100.0);
    }
    private double GetPressure()
    {
        double basePressure = 1013.25;
        double[] seasonAdjustments = [1.02, 0.98, 1.01, 1.03, 1.02];
        int currentSeasonIndex = (int)Math.Floor(dayNight.Season) % 4;
        int nextSeasonIndex    = (currentSeasonIndex + 1) % 4;
        double seasonFraction  = dayNight.Season - Math.Floor(dayNight.Season);

        double seasonAdjustment = seasonAdjustments[currentSeasonIndex] * (1 - seasonFraction) +
                                  seasonAdjustments[nextSeasonIndex] * seasonFraction;
        double timeAdjustment   = 1.0 + 0.005 * Math.Cos(dayNight.TimeOfDay / 24.0 * 2 * Math.PI);
        double weatherAdjustment = weather.CurrentWeather switch
        {
            WeatherType.Clear       => 1.01,
            WeatherType.Rain        => 0.99,
            WeatherType.Snow        => 0.98,
            WeatherType.Thunderstorm=> 0.95,
            WeatherType.Fog         => 1.00,
            WeatherType.Overcast    => 0.97,
            WeatherType.Hail        => 0.96,
            WeatherType.Sleet       => 0.95,
            WeatherType.Drizzle     => 0.98,
            WeatherType.BlowingSnow => 0.94,
            WeatherType.Sandstorm   => 0.93,
            _ => 1.0
        };

        double pressure = basePressure * seasonAdjustment * timeAdjustment * weatherAdjustment;
        pressure += (rng.NextDouble() - 0.5) * 0.5;
        return pressure;
    }
    // baseRegionTemp: -25 to +25°C from map's average temperature setting
    // seasonalOffset: cos((season-1.5)×π/2)×15 — peaks +15°C midsummer (1.5), -15°C midwinter (3.5)
    // dailyOffset: sin((t-8)/24×2π)×8 — peaks +8°C at 2pm, -8°C at 2am
    private static double GetTemperature(double season, double timeOfDay, WeatherType weatherType, double avarageTempature)
    {
        season %= 4.0;
        double baseRegionTemp = (avarageTempature - 0.5) * 50.0;
        double seasonalOffset = Math.Cos((season - 1.5) * Math.PI / 2.0) * 15.0;
        double dailyOffset    = Math.Sin((timeOfDay - 8.0) / 24.0 * 2.0 * Math.PI) * 8.0;
        double weatherOffset  = weatherType switch
        {
            WeatherType.Thunderstorm => -2.0,
            WeatherType.Rain         => -1.0,
            WeatherType.Snow         => -3.0,
            WeatherType.Sleet        => -1.5,
            WeatherType.Overcast     => -0.5,
            WeatherType.Clear        =>  1.0,
            WeatherType.Fog          => -0.2,
            WeatherType.Hail         => -1.5,
            WeatherType.Drizzle      => -0.5,
            WeatherType.BlowingSnow  => -2.0,
            WeatherType.Sandstorm    =>  1.5,
            _                        =>  0.0
        };
        return baseRegionTemp + seasonalOffset + dailyOffset + weatherOffset;
    }
    private void UpdateGust()
    {
        _gustTimer -= deltaTime;
        if (_gustStrength > 0.0)
            _gustStrength = Math.Max(0.0, _gustStrength - deltaTime * 2.0);
        if (_gustTimer > 0.0) return;
        double gustChance = weather.CurrentWeather switch
        {
            WeatherType.Thunderstorm => 0.40,
            WeatherType.Sandstorm    => 0.50,
            WeatherType.BlowingSnow  => 0.30,
            WeatherType.Rain         => 0.15,
            WeatherType.Snow         => 0.10,
            _                        => 0.05
        };
        if (rng.NextDouble() < gustChance)
            _gustStrength = rng.NextDouble() * 10.0 + 3.0;
        _gustTimer = rng.NextDouble() * 15.0 + 5.0;
    }
    private void UpdateWind()
    {
        WindChangeTimer += deltaTime;

        if (WindChangeTimer >= MinWindChangeInterval && !IsTurning)
        {
            double changeChance = deltaTime / (MaxWindChangeInterval - MinWindChangeInterval);
            if (rng.NextDouble() < changeChance)
            {
                IsTurning = true;
                WindChangeTimer = 0.0;
                int turn = rng.Next(-90, 91);
                WindTargetDirection = (weather.WindDirection + turn + 360) % 360;
            }
        }

        if (IsTurning)
        {
            double difference = WindTargetDirection - weather.WindDirection;
            difference = (difference + 180) % 360 - 180;
            double turnDirection = difference > 0 ? 1 : -1;
            double turnAmount    = 2.0;

            if (Math.Abs(difference) <= turnAmount)
            {
                weather.WindDirection = WindTargetDirection;
                IsTurning = false;
                WindChangeTimer = 0.0;
            }
            else
            {
                weather.WindDirection += turnDirection * turnAmount;
                weather.WindDirection  = Math.Round(weather.WindDirection);
                weather.WindDirection  = (weather.WindDirection + 360) % 360;
            }
        }

        double baseWindSpeed   = GetBaseWindSpeed();
        double timeOfDayFactor = GetTimeOfDayWindFactor();
        double seasonFactor    = GetSeasonWindFactor();
        double weatherFactor   = GetWeatherWindFactor();

        UpdateGust();
        weather.WindSpeed = Math.Clamp(
            baseWindSpeed * timeOfDayFactor * seasonFactor * weatherFactor * 3.0 + _gustStrength,
            0.0, 80.0
        );
    }
    private double GetBaseWindSpeed() => 10.0;
    // sin curve peaks midday due to thermal convection currents — varies 0.5–1.5×
    private double GetTimeOfDayWindFactor() =>
        1.0 + 0.5 * Math.Sin((dayNight.TimeOfDay / 24.0) * 2 * Math.PI);
    // cos curve: winter (3.5) → 1.2×, summer (1.5) → 0.6×, equinoxes → 0.9×
    private double GetSeasonWindFactor() =>
        0.9 + 0.3 * Math.Cos((dayNight.Season - 3.5) * Math.PI / 2.0);
    private double GetWeatherWindFactor() => weather.CurrentWeather switch
    {
        WeatherType.Thunderstorm => 1.5,
        WeatherType.Rain         => 1.2,
        WeatherType.Snow         => 1.1,
        WeatherType.Fog          => 0.7,
        WeatherType.Clear        => 1.0,
        _                        => 1.0,
    };
    public double MinWindChangeInterval {get; set;} = 30.0;
    public double MaxWindChangeInterval {get; set;} = 90.0;
    public double WindChangeTimer {get; set;}
    public double WindTargetDirection {get; set;}
    public bool IsTurning {get; set;} = false;
    #endregion
    #region dayNight cycle
    public void ComputeDayNightDarkness()
    {
        double transitionProgress = Math.Clamp(DayNightCycle.EaseInOutQuad(dayNight.GetTransitionProgress()), 0.0, 1.0);
        double maxShadowIntensity = 50.0;
        double gradientWidth      = 0.3;

        for (int x = 1; x < width - 1; x++)
        for (int y = 1; y < height - 1; y++)
        {
            double normalizedDistance = CalculateNormalizedDistance(x, y);
            double tileShadowProgress = Math.Clamp((transitionProgress - normalizedDistance + gradientWidth) / gradientWidth, 0, 1);
            darknessData[x, y]        = (int)(tileShadowProgress * maxShadowIntensity);
        }
    }
    private double CalculateNormalizedDistance(int x, int y)
    {
        double dx = 0;
        double dy = 0;
        int gradientOffset = 50;

        switch (dayNight.CurrentGradientDirection)
        {
            case GradientDirection.TL_BR:
                dx = x + gradientOffset;
                dy = y + gradientOffset;
                break;
            case GradientDirection.BR_TL:
                dx = width  - x + gradientOffset;
                dy = height - y + gradientOffset;
                break;
            case GradientDirection.BL_TR:
                dx = x + gradientOffset;
                dy = height - y + gradientOffset;
                break;
            case GradientDirection.TR_BL:
                dx = width - x + gradientOffset;
                dy = y + gradientOffset;
                break;
        }

        double distance    = Math.Sqrt(dx * dx + dy * dy);
        double maxDistance = Math.Sqrt((width + gradientOffset) * (width + gradientOffset) + (height + gradientOffset) * (height + gradientOffset));
        return distance / maxDistance;
    }
    #endregion
}
