using System;
using System.Collections.Generic;
using System.Linq;
using Internal;
using static Internal.GUI;

public partial class Map
{
    #region essentials
    public Weather weather {get; set;} = new Weather();
    public List<Cloud> clouds {get; set;} = new List<Cloud>();
    public double deltaTime {get; set;} = 0.5;
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
        weather.TimeOfDay = 12.0;
        weather.Season = rng.NextDouble() * 4.0;
        weather.Temperature = GetTemperature(weather.Season, weather.TimeOfDay, weather.CurrentWeather, avarageTempature);
        weather.Humidity = GetHumidity(weather.CurrentWeather, weather.Temperature, weather.TimeOfDay, weather.Season, avarageHumidity, seed);
        weather.Pressure = GetPressure();
        weather.WindSpeed = rng.NextDouble() * 10.0 + 5.0;
        weather.WindDirection = rng.Next(361);
        sunriseTime = 6.0;
        sunsetTime = 18.0;
    }
    public void UpdateWeather()
    {
        if (conf.DoTimeCycle)
        {
            // Update time of day and season
            weather.TimeOfDay += deltaTime * 24.0 / 720.0; 
            time = weather.TimeOfDay;
            if (weather.TimeOfDay >= 24.0) // One day is 12 minutes
            {
                weather.TimeOfDay -= 24.0;
                dayCount++;
            }

            int daysPerSeason = 10; // Customize the number of days per season
            weather.Season += deltaTime / (daysPerSeason * 720.0);
            if (weather.Season >= 4.0)
                weather.Season -= 4.0;
        }
        // Smoothly transition intensity
        weather.Intensity += (weather.IntensityTarget - weather.Intensity) * weather.IntensityChangeSpeed * deltaTime;

        // Change weather if necessary
        if (ShouldChangeWeather())
        {
            weather.CurrentWeather = weather.NextWeather;
            weather.NextWeather = GetSeasonWeatherType();
            weather.IntensityTarget = rng.NextDouble();
            InitializeMinTimeBetweenChanges();
        }

        // Update temperature based on season and time of day
        weather.Temperature = GetTemperature(weather.Season, weather.TimeOfDay, weather.CurrentWeather, avarageTempature);

        // Update humidity based on weather type and temperature
        weather.Humidity = GetHumidity(weather.CurrentWeather, weather.Temperature, weather.TimeOfDay, weather.Season, avarageHumidity, seed);

        // Update pressure based on weather conditions
        weather.Pressure = GetPressure();

        // Update wind speed and direction dynamically
        UpdateWind();
        UpdateCloudShadows();
        UpdateTime();
        UpdateSeason();
        if (conf.DoTimeCycle) UpdateSunTimes(weather.Season);
    }
    public void UpdateSunTimes(double season)
    {
        // Define the exact season values for solstices
        double summerSolstice = 1.8;
        double winterSolstice = 3.9;

        // Calculate the progression between solstices
        double progress;
        double sunriseOffset, sunsetOffset;

        if (season <= summerSolstice)
        {
            // From Spring to Summer Solstice
            progress = (season - 0.0) / (summerSolstice - 0.0);
        }
        else
        {
            // From Summer Solstice to Winter Solstice
            progress = (season - summerSolstice) / (winterSolstice - summerSolstice);
        }

        // Use a cosine function to ensure alignment at solstices
        sunriseOffset = Math.Cos(progress * Math.PI) * (winterSolsticeSunrise - equinoxSunrise);
        sunsetOffset = Math.Cos(progress * Math.PI) * (winterSolsticeSunset - equinoxSunset);

        // Update sunrise and sunset times
        sunriseTime = Math.Round(equinoxSunrise + sunriseOffset, 2);
        sunsetTime = Math.Round(equinoxSunset + sunsetOffset, 2);

        // Ensure exact times at solstices
        if (Math.Abs(season - summerSolstice) < 0.01)
        {
            sunriseTime = summerSolsticeSunrise;
            sunsetTime = summerSolsticeSunset;
        }
        else if (Math.Abs(season - winterSolstice) < 0.01)
        {
            sunriseTime = winterSolsticeSunrise;
            sunsetTime = winterSolsticeSunset;
        }
    }
    public double timeSinceLastWeatherChange {get; set;} = 0.0;

    // Average times for sunrise and sunset at equinoxes and solstices (in hours)
    public static readonly double equinoxSunrise = 6.0;
    public static readonly double equinoxSunset = 18.0;
    public static readonly double summerSolsticeSunrise = 5.0;
    public static readonly double summerSolsticeSunset = 21.0;
    public static readonly double winterSolsticeSunrise = 7.0;
    public static readonly double winterSolsticeSunset = 17.0;
    public int dayCount {get; set;}
    public double minTimeBetweenChanges {get; set;}
    private WeatherType GetSeasonWeatherType()
    {
        double temperature = weather.Temperature;
        double humidity = weather.Humidity;
        double pressure = weather.Pressure;
        _ = weather.WindSpeed;
        _ = weather.WindDirection;
        _ = weather.TimeOfDay;
        double season = weather.Season;

        WeatherType weatherType = WeatherType.Clear;

        // Initialize weather probabilities
        Dictionary<WeatherType, double> weatherProbabilities = new Dictionary<WeatherType, double>()
        {
            { WeatherType.Clear, 0.3 },
            { WeatherType.Rain, 0.1 },
            { WeatherType.Snow, 0.1 },
            { WeatherType.Thunderstorm, 0.05 },
            { WeatherType.Fog, 0.05 },
            { WeatherType.Overcast, 0.1 },
            { WeatherType.Hail, 0.05 },
            { WeatherType.Sleet, 0.05 },
            { WeatherType.Drizzle, 0.1 },
            { WeatherType.BlowingSnow, 0.05 },
            { WeatherType.Sandstorm, 0.05 }
        };

        // Adjust probabilities based on temperature
        if (temperature < 0)
        {
            weatherProbabilities[WeatherType.Snow] += 0.3;
            weatherProbabilities[WeatherType.BlowingSnow] += 0.1;
            weatherProbabilities[WeatherType.Rain] -= 0.1;
            weatherProbabilities[WeatherType.Thunderstorm] -= 0.05;
        }
        else if (temperature > 25)
        {
            weatherProbabilities[WeatherType.Thunderstorm] += 0.1;
            weatherProbabilities[WeatherType.Sandstorm] += 0.1;
        }
        else if (temperature > 15)
        {
            weatherProbabilities[WeatherType.Rain] += 0.1;
            weatherProbabilities[WeatherType.Thunderstorm] += 0.05;
        }

        // Adjust probabilities based on humidity
        if (humidity > 80)
        {
            weatherProbabilities[WeatherType.Rain] += 0.2;
            weatherProbabilities[WeatherType.Drizzle] += 0.1;
            weatherProbabilities[WeatherType.Fog] += 0.1;
            weatherProbabilities[WeatherType.Overcast] += 0.1;
        }
        else if (humidity < 30)
        {
            weatherProbabilities[WeatherType.Clear] += 0.1;
            weatherProbabilities[WeatherType.Sandstorm] += 0.1;
        }

        // Adjust probabilities based on pressure
        if (pressure < 1000)
        {
            weatherProbabilities[WeatherType.Rain] += 0.1;
            weatherProbabilities[WeatherType.Thunderstorm] += 0.1;
            weatherProbabilities[WeatherType.Hail] += 0.05;
            weatherProbabilities[WeatherType.Overcast] += 0.1;
        }
        else if (pressure > 1020)
        {
            weatherProbabilities[WeatherType.Clear] += 0.2;
        }

        // Adjust probabilities based on season
        if (season >= 0.0 && season < 1.0) // Spring
        {
            weatherProbabilities[WeatherType.Rain] += 0.1;
            weatherProbabilities[WeatherType.Drizzle] += 0.1;
        }
        else if (season >= 1.0 && season < 2.0) // Summer
        {
            weatherProbabilities[WeatherType.Thunderstorm] += 0.1;
            weatherProbabilities[WeatherType.Clear] += 0.1;
        }
        else if (season >= 2.0 && season < 3.0) // Autumn
        {
            weatherProbabilities[WeatherType.Overcast] += 0.1;
            weatherProbabilities[WeatherType.Fog] += 0.1;
        }
        else // Winter
        {
            weatherProbabilities[WeatherType.Snow] += 0.2;
            weatherProbabilities[WeatherType.BlowingSnow] += 0.1;
            weatherProbabilities[WeatherType.Sleet] += 0.1;
        }

        // Adjust probabilities based on current weather
        switch (weather.CurrentWeather)
        {
            case WeatherType.Rain:
                weatherProbabilities[WeatherType.Thunderstorm] += 0.2;
                weatherProbabilities[WeatherType.Rain] += 0.1;
                weatherProbabilities[WeatherType.Drizzle] += 0.05;
                break;
            case WeatherType.Thunderstorm:
                weatherProbabilities[WeatherType.Rain] += 0.1;
                weatherProbabilities[WeatherType.Clear] += 0.05;
                break;
            case WeatherType.Clear:
                weatherProbabilities[WeatherType.Clear] += 0.1;
                weatherProbabilities[WeatherType.Rain] += 0.05;
                break;
            case WeatherType.Overcast:
                weatherProbabilities[WeatherType.Rain] += 0.1;
                weatherProbabilities[WeatherType.Clear] += 0.05;
                break;
            case WeatherType.Snow:
                weatherProbabilities[WeatherType.Snow] += 0.2;
                weatherProbabilities[WeatherType.BlowingSnow] += 0.1;
                break;
            case WeatherType.BlowingSnow:
                weatherProbabilities[WeatherType.Snow] += 0.1;
                weatherProbabilities[WeatherType.BlowingSnow] += 0.1;
                break;
            case WeatherType.Fog:
                weatherProbabilities[WeatherType.Fog] += 0.1;
                weatherProbabilities[WeatherType.Clear] += 0.05;
                break;
            // Add more cases as needed
        }

        // Ensure probabilities are not negative
        foreach (WeatherType key in weatherProbabilities.Keys.ToArray())
        {
            if (weatherProbabilities[key] < 0)
                weatherProbabilities[key] = 0;
        }

        // Normalize probabilities
        double totalProbability = weatherProbabilities.Values.Sum();
        if (totalProbability == 0)
        {
            // If totalProbability is zero after adjustments, assign default probabilities
            weatherProbabilities = new Dictionary<WeatherType, double>()
            {
                { WeatherType.Clear, 0.3 },
                { WeatherType.Rain, 0.1 },
                { WeatherType.Snow, 0.1 },
                { WeatherType.Thunderstorm, 0.05 },
                { WeatherType.Fog, 0.05 },
                { WeatherType.Overcast, 0.1 },
                { WeatherType.Hail, 0.05 },
                { WeatherType.Sleet, 0.05 },
                { WeatherType.Drizzle, 0.1 },
                { WeatherType.BlowingSnow, 0.05 },
                { WeatherType.Sandstorm, 0.05 }
            };
            totalProbability = weatherProbabilities.Values.Sum();
        }

        // Generate random number to select weather type
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
        minTimeBetweenChanges = weather.CurrentWeather switch
        {
            WeatherType.Clear => 95.0,
            WeatherType.Rain => 125.0,
            WeatherType.Snow => 255.0,
            WeatherType.Thunderstorm => 260.0,
            WeatherType.Fog => 140.0,
            WeatherType.Overcast => 90.0,
            WeatherType.Hail => 90.0,
            WeatherType.Sleet => 120.0,
            WeatherType.Drizzle => 105.0,
            WeatherType.BlowingSnow => 95.0,
            WeatherType.Sandstorm => 65.0,
            _ => 115.0
        };
    }
    private bool ShouldChangeWeather()
    {
        // Increment the timer
        timeSinceLastWeatherChange += deltaTime;

        if (timeSinceLastWeatherChange < minTimeBetweenChanges)
        {
            return false;
        }

        // Probability-based weather change
        double changeProbability = 0.01 * deltaTime * 10;
        if (rng.NextDouble() < changeProbability)
        {
            timeSinceLastWeatherChange = 0.0;
            return true;
        }

        return false;
    }
    private WeatherType GetRandomWeatherType()
    {
        Array values = Enum.GetValues(typeof(WeatherType));
        object? value = values.GetValue(rng.Next(values.Length));
        return value != null ? (WeatherType)value : WeatherType.Clear;
    }
    public static double weatherFactor {get; set;} = 40;
    private static double GetHumidity(WeatherType weatherType, double tempature, double timeOfDay, double season, double avarageHumidity, int seed)
    {
        double seasonFactor = Math.Cos(season / 2.0 * Math.PI);
        double timeFactor = Math.Cos(timeOfDay / 24.0 * 2 * Math.PI);
        double tempatureFactor = Math.Cos(tempature / 40.0 * Math.PI);
        Random rng = new Random(seed);
        double humidity;
        double weather = weatherType switch
        {
            WeatherType.Clear => 50.0,
            WeatherType.Rain => 60.0,
            WeatherType.Snow => 50.0,
            WeatherType.Thunderstorm => 70.0,
            WeatherType.Fog => 75.0,
            WeatherType.Overcast => 70.0,
            WeatherType.Hail => 60.0,
            WeatherType.Sleet => 50.0,
            WeatherType.Drizzle => 55.0,
            WeatherType.BlowingSnow => 40.0,
            WeatherType.Sandstorm => 40.0,
            _ => 50.0
        };
        weather += (avarageHumidity - 0.5) * 10;
        double weatherTimer = weatherType switch
        {
            WeatherType.Clear => rng.NextDouble() * 2.0,
            WeatherType.Rain => rng.NextDouble() * 3.5,
            WeatherType.Snow => rng.NextDouble() * 3.0,
            WeatherType.Thunderstorm => rng.NextDouble() * 4.0,
            WeatherType.Fog => rng.NextDouble() * 4.5,
            WeatherType.Overcast => rng.NextDouble() * 3.0,
            WeatherType.Hail => rng.NextDouble() * 3.0,
            WeatherType.Sleet => rng.NextDouble() * 3.0,
            WeatherType.Drizzle => rng.NextDouble() * 3.0,
            WeatherType.BlowingSnow => rng.NextDouble() * 3.0,
            WeatherType.Sandstorm => rng.NextDouble() * 3.0,
            _ => 3.0
        };
        if (weatherFactor < weather)
        {
            weatherFactor += weatherTimer;
        }
        else if (weatherFactor > weather + 1)
        {
            weatherFactor -= weatherTimer;
        }

        humidity = weatherFactor + seasonFactor + timeFactor + tempatureFactor;
    
        // Ensure humidity stays within realistic bounds (0% - 100%)
        humidity = Math.Clamp(humidity, 0.0, 100.0);
    
        return humidity;
    }
    private double GetPressure()
    {
        // Base atmospheric pressure in hPa
        double basePressure = 1013.25;
        double pressure;

        // Seasonal adjustments with smooth transitions
        double[] seasonAdjustments = new double[] { 1.02, 0.98, 1.01, 1.03, 1.02 }; // Spring, Summer, Autumn, Winter, Spring
        int currentSeasonIndex = (int)Math.Floor(weather.Season) % 4;
        int nextSeasonIndex = (currentSeasonIndex + 1) % 4;
        double seasonFraction = weather.Season - Math.Floor(weather.Season);

        double seasonAdjustment = seasonAdjustments[currentSeasonIndex] * (1 - seasonFraction) +
                                seasonAdjustments[nextSeasonIndex] * seasonFraction;

        // Time of day adjustments (higher pressure at night)
        double timeAdjustment = 1.0 + 0.005 * Math.Cos(weather.TimeOfDay / 24.0 * 2 * Math.PI);

        // Weather type adjustments
        double weatherAdjustment = weather.CurrentWeather switch
        {
            WeatherType.Clear => 1.01,
            WeatherType.Rain => 0.99,
            WeatherType.Snow => 0.98,
            WeatherType.Thunderstorm => 0.95,
            WeatherType.Fog => 1.00,
            WeatherType.Overcast => 0.97,
            WeatherType.Hail => 0.96,
            WeatherType.Sleet => 0.95,
            WeatherType.Drizzle => 0.98,
            WeatherType.BlowingSnow => 0.94,
            WeatherType.Sandstorm => 0.93,
            _ => 1.0
        };

        // Calculate dynamic pressure
        pressure = basePressure * seasonAdjustment * timeAdjustment * weatherAdjustment;

        // Introduce minor random fluctuations for realism
        pressure += (rng.NextDouble() - 0.5) * 0.5; // ±0.25 hPa
        return pressure;
    }
    private static double GetTemperature(double season, double timeOfDay, WeatherType weatherType, double avarageTempature)
    {
        // Normalize the season value between 0 and 4
        season %= 4.0;

        // Define temperatures at key points for each season (in degrees Celsius)
        // Index 0: Start of Spring, 1: Start of Summer, 2: Start of Autumn, 3: Start of Winter, 4: Wrap back to Spring
        // Determine temperature zones
        int tempZone = avarageTempature switch
        {
            < 0.0 => throw new ArgumentOutOfRangeException(nameof(avarageTempature), "Temperature cannot be negative."),
            < 0.1 => 1, // Very Cold
            < 0.3 => 2, // Cold
            < 0.5 => 3, // Cool
            < 0.7 => 4, // Temperate
            _ => 5,      // Warm
        };

        // Define temperatures at key points for each season (in degrees Celsius)
        double[] seasonTemperatures = tempZone switch
        {
            1 => new double[] { -10.0, 0.0, -5.0, -20.0, -10.0 }, // Very Cold
            2 => new double[] { 0.0, 10.0, 5.0, -5.0, 0.0 }, // Cold
            3 => new double[] { 10.0, 20.0, 15.0, 5.0, 10.0 }, // Cool
            4 => new double[] { 15.0, 25.0, 20.0, 10.0, 15.0 }, // Temperate
            5 => new double[] { 20.0, 30.0, 25.0, 15.0, 20.0 }, // Warm
            _ => new double[] { 10.0, 25.0, 15.0, 0.0, 10.0 } // Default
        };

        // Get the current season index and the fraction within that season
        int seasonIndex = (int)Math.Floor(season);
        double seasonProgress = season - seasonIndex;

        // Get temperatures at the start and end of the current season
        double tempStart = seasonTemperatures[seasonIndex];
        double tempEnd = seasonTemperatures[seasonIndex + 1];

        // Smoothly interpolate the base temperature between seasons
        double baseTemp = tempStart + (tempEnd - tempStart) * seasonProgress;

        // Adjust temperature based on time of day (warmer during the day, cooler at night)
        // Shift the time to peak at 14 hours
        double dayTemperatureVariation = Math.Sin((timeOfDay - 8.0) / 24.0 * 2 * Math.PI) * 8.0;
        baseTemp += dayTemperatureVariation;

        // Adjust temperature based on current weather conditions
        double weatherAdjustment = weatherType switch
        {
            WeatherType.Thunderstorm => -2.0,
            WeatherType.Rain => -1.0,
            WeatherType.Snow => -3.0,
            WeatherType.Sleet => -1.5,
            WeatherType.Overcast => -0.5,
            WeatherType.Clear => 1.0,
            WeatherType.Fog => -0.2,
            WeatherType.Hail => -1.5,
            WeatherType.Drizzle => -0.5,
            WeatherType.BlowingSnow => -2.0,
            WeatherType.Sandstorm => 1.5,
            _ => 0.0
        };
        baseTemp += weatherAdjustment;
        
        // Integrate the average temperature to make a realistic temperature based around it
        int avarageTempatureFactor = avarageTempature switch
        {
            < 0.0 => throw new ArgumentOutOfRangeException(nameof(avarageTempature), "Temperature cannot be negative."),
            < 0.1 => 45, // Very Cold
            < 0.3 => 35, // Cold
            < 0.5 => 30, // Cool
            < 0.7 => 35, // Temperate
            _ => 35,      // Warm
        };
        baseTemp += (avarageTempature - 0.5) * avarageTempatureFactor; // Adjust the factor as needed to influence the temperature
        
        return baseTemp;
    }
    private void UpdateWind()
    {
        windChangeTimer += deltaTime;

        if (windChangeTimer >= minWindChangeInterval && !isTurning)
        {
            // Decide whether to change wind direction
            double changeChance = deltaTime / (maxWindChangeInterval - minWindChangeInterval);
            if (rng.NextDouble() < changeChance)
            {
                // Start turning
                isTurning = true;
                windChangeTimer = 0.0;
                // Choose a random angle to turn, limited to -20 to 20 degrees
                int turn = rng.Next(-20, 21); // Random integer between -20 and 20 inclusive
                windTargetDirection = (weather.WindDirection + turn + 360) % 360;
            }
        }

        if (isTurning)
        {
            // Calculate the smallest difference
            double difference = windTargetDirection - weather.WindDirection;
            difference = (difference + 180) % 360 - 180;

            // Determine the direction to turn
            double turnDirection = difference > 0 ? 1 : -1;

            // Apply a smooth turn by 1 degree per update
            double turnAmount = 1.0;

            if (Math.Abs(difference) <= turnAmount)
            {
                weather.WindDirection = windTargetDirection;
                isTurning = false;
                windChangeTimer = 0.0;
            }
            else
            {
                weather.WindDirection += turnDirection * turnAmount;
                // Ensure degrees are integers
                weather.WindDirection = Math.Round(weather.WindDirection);
                weather.WindDirection = (weather.WindDirection + 360) % 360;
            }
        }

        // Calculate base wind speed
        double baseWindSpeed = GetBaseWindSpeed();

        // Adjust wind speed based on time of day, season, and weather
        double timeOfDayFactor = GetTimeOfDayWindFactor();
        double seasonFactor = GetSeasonWindFactor();
        double weatherFactor = GetWeatherWindFactor();

        weather.WindSpeed = baseWindSpeed * timeOfDayFactor * seasonFactor * weatherFactor;

        // Calculate pressure gradient influence
        double pressureGradient = GetPressureGradient();
        double gradientFactor = 0.05; // Adjust for realism

        // Calculate temperature influence on wind
        double temperatureGradient = GetTemperatureGradient();
        double temperatureFactor = 0.03; // Adjust for realism

        // Update wind speed based on pressure and temperature gradients
        double windSpeedChange = (pressureGradient * gradientFactor) + (temperatureGradient * temperatureFactor);
        weather.WindSpeed += windSpeedChange * deltaTime;

        // Clamp wind speed to realistic bounds
        weather.WindSpeed = Math.Clamp(weather.WindSpeed, 0.0, 40.0);
    }
    private double GetBaseWindSpeed()
    {
        return 10.0; // Base wind speed
    }
    private double GetBaseWindDirection()
    {
        return Math.PI / 2; // Base wind direction (East)
    }
    private double GetTimeOfDayWindFactor()
    {
        // Assume stronger winds during midday due to thermal currents
        double time = weather.TimeOfDay;
        double factor = 1.0 + 0.5 * Math.Sin((time / 24.0) * 2 * Math.PI);
        return factor; // Varies between 0.5 and 1.5
    }
    private double GetSeasonWindFactor()
    {
        double season = weather.Season;
        // Seasons are represented as 0.0 to 4.0 (0 to less than 1 is Spring, etc.)
        if (season >= 0.0 && season < 1.0) // Spring
            return 1.0;
        else if (season >= 1.0 && season < 2.0) // Summer
            return 1.2;
        else if (season >= 2.0 && season < 3.0) // Autumn
            return 0.9;
        else // Winter
            return 0.8;
    }
    private double GetWeatherWindFactor()
    {
        switch (weather.CurrentWeather)
        {
            case WeatherType.Thunderstorm:
                return 1.5;
            case WeatherType.Rain:
                return 1.2;
            case WeatherType.Snow:
                return 1.1;
            case WeatherType.Fog:
                return 0.7;
            case WeatherType.Clear:
                return 1.0;
            default:
                return 1.0;
        }
    }
    private double GetWeatherWindDirectionChange()
    {
        switch (weather.CurrentWeather)
        {
            case WeatherType.Thunderstorm:
                return (rng.NextDouble() - 0.5) * (Math.PI / 4); // Random change up to ±22.5 degrees
            case WeatherType.Sandstorm:
                return Math.PI; // Winds blow from the opposite direction during sandstorms
            default:
                return 0.0;
        }
    }
    public double minWindChangeInterval {get; set;} = 30.0; // minimum interval in seconds
    public double maxWindChangeInterval {get; set;} = 90.0; // maximum interval in seconds
    public double windChangeTimer {get; set;}
    public double windTargetDirection {get; set;}
    public bool isTurning {get; set;} = false;

    // Helper method to calculate pressure gradient
    private double GetPressureGradient()
    {
        // Example: Simple gradient based on neighboring pressure
        double gradient = 0.0;
        // Implement actual pressure gradient calculation based on map data
        return gradient;
    }
    private double GetTemperatureGradient()
    {
        // Example: Simple gradient based on temperature differences
        double gradient = 0.0;
        // Implement actual temperature gradient calculation based on map data
        return gradient;
    }
    private double CalculateWindDirectionChange(double pressureGradient, double temperatureGradient)
    {
        // Example: Change direction based on pressure and temperature gradients
        double directionChange = 0.0;
        // Implement actual logic to adjust wind direction
        return directionChange;
    }
    private double GetAltitudeWindFactor(double altitude)
    {
        return 1.0 + (altitude / 10000.0) * 0.5;
    }
    #endregion
    #region dayNight cycle
    public HashSet<(int x, int y)> darkenedPositions {get; set;} = new HashSet<(int x, int y)>();
    public Dictionary<(int x, int y), int> darkenedPositionsIntensities {get; set;} = new Dictionary<(int x, int y), int>();
    public enum GradientDirection
    {
        TL_BR, // Top Left to Bottom Right
        BR_TL, // Bottom Right to Top Left
        BL_TR, // Bottom Left to Top Right
        TR_BL  // Top Right to Bottom Left
    }
    public GradientDirection CurrentGradientDirection { get; set; } = GradientDirection.TL_BR;
    public void UpdateGradientDirection(double timeOfDay)
    {
        if (timeOfDay > 0.0 && timeOfDay < 6.0)
        {
            CurrentGradientDirection = GradientDirection.BR_TL;
        }
        else if (timeOfDay > 12.0 && timeOfDay < 18.0)
        {
            CurrentGradientDirection = GradientDirection.TL_BR;
        }
    }
    public void DisplayDayNightTransition(bool displayGUI = true)
    {
        // Determine the current time and calculate transition progress
        double transitionProgress = GetTransitionProgress();

        // Clamp transitionProgress to stay within [0,1]
        transitionProgress = Math.Clamp(transitionProgress, 0.0, 1.0);

        // Use an easing function to simulate smooth transition
        double easedProgress = EaseInOutQuad(transitionProgress);

        // Increase maximum shadow intensity to make the effect noticeable
        double maxShadowIntensity = 50.0;

        // Define the width of the gradient transition (adjusted for complete coverage)
        double gradientWidth = 0.3;

        // Temporary list to track tiles to remove from darkened positions
        List<(int x, int y)> tilesToRemove = new List<(int x, int y)>();

        // Update only the tiles that need to be updated, excluding edges
        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                // Calculate normalized distance based on selected gradient direction
                double normalizedDistance = CalculateNormalizedDistance(x, y);

                // Calculate shadow progress with a smooth gradient
                double tileShadowProgress = Math.Clamp((easedProgress - normalizedDistance + gradientWidth) / gradientWidth, 0, 1);

                // Calculate current shadow intensity for this tile
                int tileShadowIntensity = (int)(tileShadowProgress * maxShadowIntensity);

                if (tileShadowIntensity > 0)
                {
                    // If the tile is already darkened, update its intensity if it has changed
                    if (darkenedPositionsIntensities.TryGetValue((x, y), out int currentIntensity))
                    {
                        if (currentIntensity != tileShadowIntensity)
                        {
                            darkenedPositionsIntensities[(x, y)] = tileShadowIntensity;
                            darkenedPositions.Add((x, y));

                            // Update tile with new shadow intensity
                            if ((isCloudsRendering && !IsTileUnderCloud(x, y)) || !isCloudsRendering) UpdateTileShadow(x, y, tileShadowIntensity, displayGUI);
                        }
                    }
                    else
                    {
                        // Add new darkened tile
                        darkenedPositionsIntensities[(x, y)] = tileShadowIntensity;
                        darkenedPositions.Add((x, y));

                        // Update tile with shadow
                        if ((isCloudsRendering && !IsTileUnderCloud(x, y)) || !isCloudsRendering) UpdateTileShadow(x, y, tileShadowIntensity, displayGUI);
                    }
                }
                else
                {
                    // Remove tiles that no longer have shadow
                    if (darkenedPositionsIntensities.ContainsKey((x, y)))
                    {
                        tilesToRemove.Add((x, y));
                    }
                }
            }
        }

        // Remove tiles that no longer have shadow
        foreach ((int x, int y) tile in tilesToRemove)
        {
            darkenedPositionsIntensities.Remove(tile);
            darkenedPositions.Remove(tile);

            // Reset tile color
            ResetTileColor(tile.x, tile.y, displayGUI);
        }
    }
    private void UpdateTileShadow(int x, int y, int shadowIntensity, bool displayGUI = true)
    {
        int effectiveLeftPadding = displayGUI ? leftPadding : 0;
        int effectiveTopPadding = displayGUI ? topPadding : 2;
        // Get the base color of the tile
        (int r, int g, int b) baseColor = GetTileBaseColor(x, y);

        // Apply the shadow intensity
        int r = Math.Max(0, baseColor.r - shadowIntensity);
        int g = Math.Max(0, baseColor.g - shadowIntensity);
        int b = Math.Max(0, baseColor.b - shadowIntensity);

        // Update tile with new color
        GUI.SetCursorPosition(effectiveLeftPadding + x * 2, y + effectiveTopPadding);

        if (IsThereAnOverlayTile(x, y))
        {
            (int r, int g, int b) overlayColor = GetOverlayColor(overlayData[x, y]);

            // Apply shadow intensity to overlay color as well
            int or = Math.Max(0, overlayColor.r - shadowIntensity);
            int og = Math.Max(0, overlayColor.g - shadowIntensity);
            int ob = Math.Max(0, overlayColor.b - shadowIntensity);

            string background = GUI.SetBackgroundColor(r, g, b);
            string foreground = GUI.SetForegroundColor(or, og, ob);
            GUI.Write(background + foreground + GetSpeciesIcon(overlayData[x, y]) + GUI.ResetColor());
        }
        else
        {
            string background = GUI.SetBackgroundColor(r, g, b);
            GUI.Write(background + "  " + GUI.ResetColor());
        }
    }
    private void ResetTileColor(int x, int y, bool displayGUI = true)
    {
        int effectiveLeftPadding = displayGUI ? leftPadding : 0;
        int effectiveTopPadding = displayGUI ? topPadding : 2;
        // Get the base color of the tile
        (int r, int g, int b) baseColor = GetTileBaseColor(x, y);

        // Update tile with base color
        GUI.SetCursorPosition(effectiveLeftPadding + x * 2, y + effectiveTopPadding);
        string background = GUI.SetBackgroundColor(baseColor.r, baseColor.g, baseColor.b);
        GUI.Write(background + "  " + GUI.ResetColor());

        if (IsThereAnOverlayTile(x, y))
        {
            (int r, int g, int b) overlayColor = GetOverlayColor(overlayData[x, y]);
            string foreground = GUI.SetForegroundColor(overlayColor.r, overlayColor.g, overlayColor.b);
            GUI.SetCursorPosition(effectiveLeftPadding + x * 2, y + effectiveTopPadding);
            GUI.Write(background + foreground + $"{overlayData[x, y]}" + GUI.ResetColor());
        }
    }
    private double CalculateNormalizedDistance(int x, int y)
    {
        double dx = 0;
        double dy = 0;

        // Offset to move the gradient origin outside the display
        int gradientOffset = 50; // Adjust this value as needed

        switch (CurrentGradientDirection)
        {
            case GradientDirection.TL_BR:
                dx = x + gradientOffset;
                dy = y + gradientOffset;
                break;
            case GradientDirection.BR_TL:
                dx = (width - x) + gradientOffset;
                dy = (height - y) + gradientOffset;
                break;
            case GradientDirection.BL_TR:
                dx = x + gradientOffset;
                dy = (height - y) + gradientOffset;
                break;
            case GradientDirection.TR_BL:
                dx = (width - x) + gradientOffset;
                dy = y + gradientOffset;
                break;
        }

        double distance = Math.Sqrt(dx * dx + dy * dy);
        double maxDistance = Math.Sqrt((width + gradientOffset) * (width + gradientOffset) + (height + gradientOffset) * (height + gradientOffset));
        return distance / maxDistance;
    }
    private double GetTransitionProgress()
    {
        double timeOfDay = weather.TimeOfDay; // Current time in 24h format

        // Duration of the transition in hours (adjustable)
        double transitionDuration = 1.0;

        // Normalize timeOfDay to [0,24)
        timeOfDay %= 24.0;

        double transitionProgress = 0.0;

        // Setup transition periods
        double sunsetStart = sunsetTime - transitionDuration;
        if (sunsetStart < 0) sunsetStart += 24.0;

        double sunsetEnd = sunsetTime;

        double sunriseStart = sunriseTime;
        double sunriseEnd = sunriseTime + transitionDuration;
        if (sunriseEnd >= 24.0) sunriseEnd -= 24.0;

        if (IsTimeBetween(timeOfDay, sunsetStart, sunsetEnd))
        {
            // Sunset transition (progress from 0 to 1)
            double totalDuration = (sunsetEnd - sunsetStart + 24.0) % 24.0;
            transitionProgress = ((timeOfDay - sunsetStart + 24.0) % 24.0) / totalDuration;
        }
        else if (IsTimeBetween(timeOfDay, sunriseStart, sunriseEnd))
        {
            // Sunrise transition (progress from 1 to 0)
            double totalDuration = (sunriseEnd - sunriseStart + 24.0) % 24.0;
            transitionProgress = 1.0 - ((timeOfDay - sunriseStart + 24.0) % 24.0) / totalDuration;
        }
        else if (IsNightTime(timeOfDay, sunsetEnd, sunriseStart))
        {
            // Night time
            transitionProgress = 1.0;
        }
        else
        {
            // Day time
            transitionProgress = 0.0;
        }

        // Clamp transitionProgress to ensure it stays within bounds
        transitionProgress = Math.Clamp(transitionProgress, 0.0, 1.0);

        return transitionProgress;
    }
    private bool IsTimeBetween(double time, double start, double end)
    {
        if (start <= end)
        {
            return time >= start && time <= end;
        }
        else
        {
            return time >= start || time <= end;
        }
    }
    private bool IsNightTime(double time, double sunsetEnd, double sunriseStart)
    {
        return IsTimeBetween(time, sunsetEnd, sunriseStart);
    }
    private static double EaseInOutQuad(double t)
    {
        // Simulate smooth transition
        if (t < 0.5)
            return 2 * t * t;
        else
            return -1 + (4 - 2 * t) * t;
    }
    private (int r, int g, int b) GetTileBaseColor(int x, int y)
    {
        if (IsThereAWaveTile(x, y))
        {
            return GetWaveColor(x, y);
        }
        else
        {
            return GetColor(mapData[x, y], x, y);
        }
    }
    private bool IsTileDarkened(int x, int y)
    {
        return darkenedPositions.Contains((x, y));
    }
    private (int r, int g, int b) GetDarkenedColor(int x, int y)
    {
        if (darkenedPositionsIntensities.TryGetValue((x, y), out int intensity))
        {
            (int r, int g, int b) baseColor = GetTileBaseColor(x, y);
            int r = Math.Max(0, baseColor.r - intensity);
            int g = Math.Max(0, baseColor.g - intensity);
            int b = Math.Max(0, baseColor.b - intensity);
            return (r, g, b);
        }
        return GetTileBaseColor(x, y);
    }
    public void DisplayDarkenedTiles(bool displayGUI = true)
    {
        int effectiveLeftPadding = displayGUI ? leftPadding : 0;
        int effectiveTopPadding = displayGUI ? topPadding : 2;
        foreach ((int x, int y) in darkenedPositions)
        {
            GUI.SetCursorPosition(effectiveLeftPadding + x * 2, y + effectiveTopPadding);
            (int r, int g, int b) color = GetDarkenedColor(x, y);
            GUI.Write(GUI.SetBackgroundColor(color.r, color.g, color.b) + "  " + GUI.ResetColor());
        }
    }
    public void DisplayDarkenedWaveTiles(bool displayGUI = true)
    {
        int effectiveLeftPadding = displayGUI ? leftPadding : 0;
        int effectiveTopPadding = displayGUI ? topPadding : 2;
        foreach ((int x, int y) in wavePositions)
        {
            GUI.SetCursorPosition(effectiveLeftPadding + x * 2, y + effectiveTopPadding);
            (int r, int g, int b) color = GetDarkenedColor(x, y);
            GUI.Write(GUI.SetBackgroundColor(color.r, color.g, color.b) + "  " + GUI.ResetColor());
        }
    }
    private (int r, int g, int b) GetDarkenedTileColor(TileId tile, int x, int y)
    {
        var baseColor = GetColor(tile, x, y);
        return (
            Math.Max(0, baseColor.r - 50),
            Math.Max(0, baseColor.g - 50),
            Math.Max(0, baseColor.b - 50)
        );
    }
    private double GetDarkenedTileIntensity(int x, int y)
    {
        if (darkenedPositionsIntensities.TryGetValue((x, y), out int intensity))
        {
            return intensity;
        }
        return 0;
    }
    private void DisplayDarkenedOverlayTiles()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (overlayData[x, y] != EntityId.None)
                {
                    
                }
            }
        }
    }
    #endregion
}
