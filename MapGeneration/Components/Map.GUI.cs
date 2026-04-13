using System;
using System.Collections.Generic;
using System.Linq;
using Internal;
using static Internal.GUI;

public partial class Map
{
    #region GUI functions
    public void DisplayGUI()
    {
        double time = Math.Round(weather.TimeOfDay, 2);
        double season = Math.Round(weather.Season, 2);
        WeatherType currentWeather = weather.CurrentWeather;
        WeatherType nextWeather = weather.NextWeather;
        double temperature = Math.Round(weather.Temperature, 2);
        double humidity = Math.Round(weather.Humidity, 2);
        double pressure = Math.Round(weather.Pressure, 2);
        double windSpeed = Math.Round(weather.WindSpeed, 2);
        double windDirection = Math.Round(weather.WindDirection, 2);
        // Get console dimensions
        int consoleWidth = Console.WindowWidth;
        int consoleHeight = Console.WindowHeight;

        // Initialize GUI Configuration
        GUIConfig config = new GUIConfig(consoleWidth, consoleHeight);

        // Check console size
        if (consoleWidth < config.MinConsoleWidth || consoleHeight < config.MinConsoleHeight)
        {
            GUI.Clear();
            GUI.SetCursorPosition(0, 0);
            GUI.Write("Please resize the console window to a larger size.");
            shouldSimulationContinue = false;
            return;
        }

        // Define margins
        int leftMargin = leftPadding;
        int topMargin = topPadding;
        int rightMargin = rightPadding;
        int bottomMargin = bottomPadding;

        // Calculate content area dimensions
        _ = consoleWidth - leftMargin - rightMargin;
        _ = consoleHeight - topMargin - bottomMargin;

        if (consoleWidth >= config.MinConsoleWidth && consoleHeight >= config.MinConsoleHeight)
        {
            // Weather Radar
            DrawBox(0, 0, config.RadarWidth, config.RadarHeight, "Weather Radar");
            GUI.SetCursorPosition(2, 1);
            GUI.Write($"Not Implemented Yet");
            // Time Info
            DisplayTimeInfo(time, season, config.RadarWidth, config.TimeWidth, config.TimeHeight);
            // Weather Stats
            DisplayWeatherStats(
                currentWeather, nextWeather, temperature, humidity, pressure, windSpeed, windDirection,
                config.RadarWidth, config.StatsWidth, config.StatsHeight
            );
            // Thanks Info
            if (Console.WindowWidth > 200 && rightMargin >= 20)
            {
                DisplayThanksMessage(config.ThanksWidth, config.ThanksHeight);
            }
            // Title
            DisplayTitleAndSignature(config.TitleWidth, config.TitleHeight, "Chambers");
            // Output Log
            DisplayOutputLog(config.OutputWidth, config.OutputHeight, config.TitleWidth);
        }
        else
        {
            // Display message if the console is too small
            GUI.Clear();
            GUI.SetCursorPosition(0, 0);
            GUI.Write("Please resize the console window to a larger size.");
            shouldSimulationContinue = false;
        }
        if (rightMargin >= 20)
        {
            // Help Info
            DisplayHelpInfo(config.HelpWidth, config.HelpHeight);
            // Tile Info
            DisplayTileInfo(config.TileWidth, config.TileHeight);
        }

        GUI.SetCursorPosition(0, height + topMargin - 1);
    }
    public void UpdateGUIValues()
    {
        double time = Math.Round(weather.TimeOfDay, 2);
        double season = Math.Round(weather.Season, 2);
        WeatherType currentWeather = weather.CurrentWeather;
        WeatherType nextWeather = weather.NextWeather;
        double temperature = Math.Round(weather.Temperature, 2);
        double humidity = Math.Round(weather.Humidity, 2);
        double pressure = Math.Round(weather.Pressure, 2);
        double windSpeed = Math.Round(weather.WindSpeed, 2);
        double windDirection = Math.Round(weather.WindDirection, 2);

        // Get console dimensions
        int consoleWidth = Console.WindowWidth;
        int consoleHeight = Console.WindowHeight;

        // Initialize GUI Configuration
        GUIConfig config = new GUIConfig(consoleWidth, consoleHeight);

        // Time Info
        UpdateTimeInfo(time, season, config.RadarWidth, config.TimeWidth, config.TimeHeight);

        // Weather Stats
        UpdateWeatherStats(
            currentWeather, nextWeather, temperature, humidity, pressure, windSpeed, windDirection,
            config.RadarWidth, config.StatsWidth, config.StatsHeight
        );
        
        // Output Log
        UpdateOutputLog(config.OutputWidth, config.OutputHeight, config.TitleWidth);
    }
    private void DisplayWeatherRadar(int x, int y, int width, int height)
    {
    }
    private void DisplayWeatherStats(WeatherType currentWeather, WeatherType nextWeather, double temperature, double humidity,
    double pressure, double windSpeed, double windDirection, int radarWidth, int statsWidth, int statsHeight)
    {
        string currentWeatherDisplay = statsWidth < 43 ? GetShortWeatherName(currentWeather) : currentWeather.ToString();
        string nextWeatherDisplay = statsWidth < 43 ? GetShortWeatherName(nextWeather) : nextWeather.ToString();

        DrawBox(radarWidth - 1, 0, statsWidth + 2, 3, "Weather Stats");
        GUI.SetCursorPosition(radarWidth + 1, 1);
        GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}Current: {currentWeatherDisplay}, Next: {nextWeatherDisplay}{GUI.ResetColor()}");
        DrawBox(radarWidth - 1, 2, statsWidth + 2, statsHeight, " ");
        GUI.SetCursorPosition(radarWidth + 1, 3);
        GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.CYAN.r, ColorSpectrum.CYAN.g, ColorSpectrum.CYAN.b)}Cloud Formations: {GetCloudFormations()}{GUI.ResetColor()}");
        GUI.SetCursorPosition(radarWidth + 1, 4);
        GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.CYAN.r, ColorSpectrum.CYAN.g, ColorSpectrum.CYAN.b)}Cloud Tiles: {GetCloudTilesCount()}{GUI.ResetColor()}");
        GUI.SetCursorPosition(radarWidth + 1, 5);
        GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.GREEN.r, ColorSpectrum.GREEN.g, ColorSpectrum.GREEN.b)}Temperature: {temperature}°C{GUI.ResetColor()}");
        GUI.SetCursorPosition(radarWidth + 1, 6);
        GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.BLUE.r, ColorSpectrum.BLUE.g, ColorSpectrum.BLUE.b)}Humidity: {humidity}%{GUI.ResetColor()}");
        GUI.SetCursorPosition(radarWidth + 1, 7);
        GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.MAGENTA.r, ColorSpectrum.MAGENTA.g, ColorSpectrum.MAGENTA.b)}Pressure: {pressure}hPa{GUI.ResetColor()}");
        GUI.SetCursorPosition(radarWidth + 1, 8);
        GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.ORANGE.r, ColorSpectrum.ORANGE.g, ColorSpectrum.ORANGE.b)}Wind Speed: {windSpeed}m/s{GUI.ResetColor()}");
        GUI.SetCursorPosition(radarWidth + 1, 9);
        GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.PURPLE.r, ColorSpectrum.PURPLE.g, ColorSpectrum.PURPLE.b)}Wind Direction: {windDirection}°{GUI.ResetColor()}");
    }
    private void UpdateWeatherStats(WeatherType currentWeather, WeatherType nextWeather, double temperature, double humidity,
    double pressure, double windSpeed, double windDirection, int radarWidth, int statsWidth, int statsHeight)
    {
        string currentWeatherDisplay = statsWidth < 43 ? GetShortWeatherName(currentWeather) : currentWeather.ToString();
        string nextWeatherDisplay = statsWidth < 43 ? GetShortWeatherName(nextWeather) : nextWeather.ToString();

        GUI.SetCursorPosition(radarWidth + 1, 1);
        GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}Current: {currentWeatherDisplay}, Next: {nextWeatherDisplay}{GUI.ResetColor()}    ");
        GUI.SetCursorPosition(radarWidth + 1, 5);
        GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.GREEN.r, ColorSpectrum.GREEN.g, ColorSpectrum.GREEN.b)}Temperature: {temperature}°C{GUI.ResetColor()}   ");
        GUI.SetCursorPosition(radarWidth + 1, 6);
        GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.BLUE.r, ColorSpectrum.BLUE.g, ColorSpectrum.BLUE.b)}Humidity: {humidity}%{GUI.ResetColor()}   ");
        GUI.SetCursorPosition(radarWidth + 1, 7);
        GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.MAGENTA.r, ColorSpectrum.MAGENTA.g, ColorSpectrum.MAGENTA.b)}Pressure: {pressure}hPa{GUI.ResetColor()}   ");
        GUI.SetCursorPosition(radarWidth + 1, 8);
        GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.ORANGE.r, ColorSpectrum.ORANGE.g, ColorSpectrum.ORANGE.b)}Wind Speed: {windSpeed}m/s{GUI.ResetColor()}   ");
        GUI.SetCursorPosition(radarWidth + 1, 9);
        GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.PURPLE.r, ColorSpectrum.PURPLE.g, ColorSpectrum.PURPLE.b)}Wind Direction: {windDirection}°{GUI.ResetColor()}   ");
    }
    private string GetShortWeatherName(WeatherType weather)
    {
        return weather switch
        {
            WeatherType.Clear => "Clear",
            WeatherType.Drizzle => "Drizz",
            WeatherType.Rain => "Rain",
            WeatherType.Snow => "Snow",
            WeatherType.Thunderstorm => "Thund",
            WeatherType.Fog => "Foggy",
            WeatherType.Overcast => "Overc",
            WeatherType.Sleet => "Sleet",
            WeatherType.Hail => "Hail",
            WeatherType.BlowingSnow => "BlowS",
            WeatherType.Sandstorm => "Sand",
            _ => weather.ToString()
        };
    }
    private void DisplayTimeInfo(double time, double season, int radarWidth, int statsWidth, int statsHeight)
    {
        DrawBox(radarWidth + statsWidth - 2, 0, statsWidth, statsHeight + 2, "Time Info");
        GUI.SetCursorPosition(radarWidth + statsWidth, 1);
        GUI.Write($" {GUI.SetForegroundColor(ColorSpectrum.LIGHT_BLUE.r, ColorSpectrum.LIGHT_BLUE.g, ColorSpectrum.LIGHT_BLUE.b)}Time: {time:F2}h{GUI.ResetColor()}   ");
        GUI.SetCursorPosition(radarWidth + statsWidth, 2);
        GUI.Write($" {GUI.SetForegroundColor(ColorSpectrum.LIGHT_GREEN.r, ColorSpectrum.LIGHT_GREEN.g, ColorSpectrum.LIGHT_GREEN.b)}Season: {season}{GUI.ResetColor()}   ");
        GUI.SetCursorPosition(radarWidth + statsWidth, 3);
        GUI.Write($" {GUI.SetForegroundColor(ColorSpectrum.ORANGE.r, ColorSpectrum.ORANGE.g, ColorSpectrum.ORANGE.b)}Sunrise: {sunriseTime}h{GUI.ResetColor()}   ");
        GUI.SetCursorPosition(radarWidth + statsWidth, 4);
        GUI.Write($" {GUI.SetForegroundColor(ColorSpectrum.ORANGE.r, ColorSpectrum.ORANGE.g, ColorSpectrum.ORANGE.b)}Sunset: {sunsetTime}h{GUI.ResetColor()}  ");
        GUI.SetCursorPosition(radarWidth + statsWidth, 5);
        if (time < sunriseTime)
        {
            GUI.Write($" {GUI.SetForegroundColor(ColorSpectrum.GREEN.r, ColorSpectrum.GREEN.g, ColorSpectrum.GREEN.b)}Time Until Sunrise: {sunriseTime - time:F2}h{GUI.ResetColor()}");
        }
        else if (time >= sunriseTime && time < sunsetTime)
        {
            GUI.Write($" {GUI.SetForegroundColor(ColorSpectrum.RED.r, ColorSpectrum.RED.g, ColorSpectrum.RED.b)}Time Until Sunset: {sunsetTime - time:F2}h{GUI.ResetColor()}");
        }
        else
        {
            double timeUntilMidnight = 24.0 - time;
            double timeUntilSunrise = timeUntilMidnight + sunriseTime;
            GUI.Write($" {GUI.SetForegroundColor(ColorSpectrum.GREEN.r, ColorSpectrum.GREEN.g, ColorSpectrum.GREEN.b)}Time Until Sunrise: {timeUntilSunrise:F2}h{GUI.ResetColor()}");
        }
        GUI.SetCursorPosition(radarWidth + statsWidth, 6);
        GUI.Write($" {GUI.SetForegroundColor(ColorSpectrum.PINK.r, ColorSpectrum.PINK.g, ColorSpectrum.PINK.b)}Day: {dayCount}{GUI.ResetColor()}   ");
        
    }
    private void UpdateTimeInfo(double time, double season, int radarWidth, int statsWidth, int statsHeight)
    {
        GUI.SetCursorPosition(radarWidth + statsWidth, 1);
        GUI.Write($" {GUI.SetForegroundColor(ColorSpectrum.LIGHT_BLUE.r, ColorSpectrum.LIGHT_BLUE.g, ColorSpectrum.LIGHT_BLUE.b)}Time: {time:F2}h{GUI.ResetColor()}   ");
        GUI.SetCursorPosition(radarWidth + statsWidth, 2);
        GUI.Write($" {GUI.SetForegroundColor(ColorSpectrum.LIGHT_GREEN.r, ColorSpectrum.LIGHT_GREEN.g, ColorSpectrum.LIGHT_GREEN.b)}Season: {season}{GUI.ResetColor()}   ");
        GUI.SetCursorPosition(radarWidth + statsWidth, 3);
        GUI.Write($" {GUI.SetForegroundColor(ColorSpectrum.ORANGE.r, ColorSpectrum.ORANGE.g, ColorSpectrum.ORANGE.b)}Sunrise: {sunriseTime}h{GUI.ResetColor()}   ");
        GUI.SetCursorPosition(radarWidth + statsWidth, 4);
        GUI.Write($" {GUI.SetForegroundColor(ColorSpectrum.ORANGE.r, ColorSpectrum.ORANGE.g, ColorSpectrum.ORANGE.b)}Sunset: {sunsetTime}h{GUI.ResetColor()}  ");
        GUI.SetCursorPosition(radarWidth + statsWidth, 5);
        if (time < sunriseTime)
        {
            GUI.Write($" {GUI.SetForegroundColor(ColorSpectrum.GREEN.r, ColorSpectrum.GREEN.g, ColorSpectrum.GREEN.b)}Time Until Sunrise: {sunriseTime - time:F2}h{GUI.ResetColor()}   ");
        }
        else if (time >= sunriseTime && time < sunsetTime)
        {
            GUI.Write($" {GUI.SetForegroundColor(ColorSpectrum.RED.r, ColorSpectrum.RED.g, ColorSpectrum.RED.b)}Time Until Sunset: {sunsetTime - time:F2}h{GUI.ResetColor()}   ");
        }
        else
        {
            double timeUntilMidnight = 24.0 - time;
            double timeUntilSunrise = timeUntilMidnight + sunriseTime;
            GUI.Write($" {GUI.SetForegroundColor(ColorSpectrum.GREEN.r, ColorSpectrum.GREEN.g, ColorSpectrum.GREEN.b)}Time Until Sunrise: {timeUntilSunrise:F2}h{GUI.ResetColor()}   ");
        }
        GUI.SetCursorPosition(radarWidth + statsWidth, 6);
        GUI.Write($" {GUI.SetForegroundColor(ColorSpectrum.PINK.r, ColorSpectrum.PINK.g, ColorSpectrum.PINK.b)}Day: {dayCount}{GUI.ResetColor()}   ");
    }
    private void DisplayHelpInfo(int helpWidth, int helpHeight)
    {
        string line = new string('-', helpWidth - 3);
        if (helpHeight < 30)
        {
            DrawBox(Console.WindowWidth - rightPadding * 2, Console.WindowHeight - bottomPadding - helpHeight, helpWidth, helpHeight, "Help Menu");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 1);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}P/Space:{GUI.ResetColor()}");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 2);
            GUI.Write("Toggle updating");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 3);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}PgUp/PgDn:{GUI.ResetColor()}");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 4);
            GUI.Write("Increase/Decrease updating speed");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 5);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}Q:{GUI.ResetColor()}");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 6);
            GUI.Write("Toggle cloud rendering");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 7);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}Up/Down:{GUI.ResetColor()}");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 8);
            GUI.Write("Go to last/first chamber");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 9);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}Left/Right:{GUI.ResetColor()}");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 10);
            GUI.Write("Previous/Next chamber");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 11);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}1 - 9:{GUI.ResetColor()}");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 12);
            GUI.Write("Go to chamber 1 - 9");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 13);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}C:{GUI.ResetColor()}");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 14);
            GUI.Write("Open console");
        }
        else
        {
            DrawBox(Console.WindowWidth - rightPadding * 2, Console.WindowHeight - bottomPadding - helpHeight, helpWidth, helpHeight, "Help Menu");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 1);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}P/Space:{GUI.ResetColor()}");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 2);
            GUI.Write("Toggle updating");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 3);
            GUI.Write(line);
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 4);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}PgUp/PgDn:{GUI.ResetColor()}");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 5);
            GUI.Write("Increase/Decrease updating speed");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 6);
            GUI.Write(line);
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 7);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}Q:{GUI.ResetColor()}");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 8);
            GUI.Write("Toggle cloud rendering");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 9);
            GUI.Write(line);
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 10);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}Up/Down:{GUI.ResetColor()}");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 11);
            GUI.Write("Go to last/first chamber");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 12);
            GUI.Write(line);
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 13);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}Left/Right:{GUI.ResetColor()}");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 14);
            GUI.Write("Previous/Next chamber");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 15);
            GUI.Write(line);
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 16);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}1 - 9:{GUI.ResetColor()}");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 17);
            GUI.Write("Go to chamber 1 - 9");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 18);
            GUI.Write(line);
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 19);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}C:{GUI.ResetColor()}");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 20);
            GUI.Write("Open console");
        }
    }
    private void DisplayTileInfo(int tileWidth, int tileHeight)
    {
        DrawBox(Console.WindowWidth - tileWidth, 0 + topPadding + 1, tileWidth, tileHeight, "Tile Info");
        if (rightPadding >= 20)
        {
            GUI.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 1 + topPadding + 1);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.DARK_GREEN.r, ColorSpectrum.DARK_GREEN.g, ColorSpectrum.DARK_GREEN.b)}Dark Green:{GUI.ResetColor()} Forest");
            GUI.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 2 + topPadding + 1);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.GREEN.r, ColorSpectrum.GREEN.g, ColorSpectrum.GREEN.b)}Green:{GUI.ResetColor()} Plains");
            GUI.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 3 + topPadding + 1);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.GREY.r, ColorSpectrum.GREY.g, ColorSpectrum.GREY.b)}Grey / {GUI.SetForegroundColor(ColorSpectrum.DARK_GREY.r, ColorSpectrum.DARK_GREY.g, ColorSpectrum.DARK_GREY.b)}Dark Gray:{GUI.ResetColor()} Mountain");
            GUI.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 4 + topPadding + 1);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.WHITE.r, ColorSpectrum.WHITE.g, ColorSpectrum.WHITE.b)}White:{GUI.ResetColor()} Snow Peak");
            GUI.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 5 + topPadding + 1);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.BLUE.r, ColorSpectrum.BLUE.g, ColorSpectrum.BLUE.b)}Blue:{GUI.ResetColor()} Water");
            GUI.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 6 + topPadding + 1);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}Yellow:{GUI.ResetColor()} Beach");
        }
        else
        {
            GUI.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 1 + topPadding + 1);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.DARK_GREEN.r, ColorSpectrum.DARK_GREEN.g, ColorSpectrum.DARK_GREEN.b)}Dark Green:{GUI.ResetColor()}");
            GUI.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 2 + topPadding + 1);
            GUI.Write("Forest");
            GUI.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 3 + topPadding + 1);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.GREEN.r, ColorSpectrum.GREEN.g, ColorSpectrum.GREEN.b)}Green:{GUI.ResetColor()}");
            GUI.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 4 + topPadding + 1);
            GUI.Write("Plains");
            GUI.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 5 + topPadding + 1);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.GREY.r, ColorSpectrum.GREY.g, ColorSpectrum.GREY.b)}Grey / {GUI.SetForegroundColor(ColorSpectrum.DARK_GREY.r, ColorSpectrum.DARK_GREY.g, ColorSpectrum.DARK_GREY.b)}Dark Gray:{GUI.ResetColor()}");
            GUI.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 6 + topPadding + 1);
            GUI.Write("Mountain");
            GUI.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 7 + topPadding + 1);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.WHITE.r, ColorSpectrum.WHITE.g, ColorSpectrum.WHITE.b)}White:{GUI.ResetColor()}");
            GUI.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 8 + topPadding + 1);
            GUI.Write("Snow Peak");
            GUI.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 9 + topPadding + 1);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.BLUE.r, ColorSpectrum.BLUE.g, ColorSpectrum.BLUE.b)}Blue:{GUI.ResetColor()}");
            GUI.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 10 + topPadding + 1);
            GUI.Write("Water");
            GUI.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 11 + topPadding + 1);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}Yellow:{GUI.ResetColor()}");
            GUI.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 12 + topPadding + 1);
            GUI.Write("Beach");
        }
    }
    private void DisplayThanksMessage(int thanksWidth, int thanksHeight)
    {
        string thanks = "Thanks";
        string line = new string('-', thanksWidth - 3);
        string halfLine = new string('-', thanksWidth / 2 - 2 - thanks.Length / 2 - 1);
        DrawBox(Console.WindowWidth - thanksWidth, 0, thanksWidth, thanksHeight, "Other");
        GUI.SetCursorPosition(Console.WindowWidth - thanksWidth + 2, 1);
        GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.PURPLE.r, ColorSpectrum.PURPLE.g, ColorSpectrum.PURPLE.b)}Thanks for playing!{GUI.ResetColor()}");
        GUI.SetCursorPosition(Console.WindowWidth - thanksWidth + 2, 2);
        GUI.Write(line);
        GUI.SetCursorPosition(Console.WindowWidth - thanksWidth + 2, 3);
        GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.CYAN.r, ColorSpectrum.CYAN.g, ColorSpectrum.CYAN.b)}This project was created as{GUI.ResetColor()}");
        GUI.SetCursorPosition(Console.WindowWidth - thanksWidth + 2, 4);
        GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.CYAN.r, ColorSpectrum.CYAN.g, ColorSpectrum.CYAN.b)}a starting project for learning C#. {GUI.ResetColor()}");
        GUI.SetCursorPosition(Console.WindowWidth - thanksWidth + 2, 5);
        GUI.Write(line);
        GUI.SetCursorPosition(Console.WindowWidth - thanksWidth + 2, thanksHeight - 4);
        GUI.Write($"{halfLine}{GUI.SetForegroundColor(ColorSpectrum.GREEN.r, ColorSpectrum.GREEN.g, ColorSpectrum.GREEN.b)} {thanks} {GUI.ResetColor()}{halfLine}");
    }
    private void DisplayTitleAndSignature(int titleWidth, int titleHeight, string title)
    {
        // Draw the box
        DrawBox(Console.WindowWidth / 2 - titleWidth / 2, 0, titleWidth, 3, " ");

        int x = Console.WindowWidth / 2 - titleWidth / 2;
        int y = 0;

        // Define the side patterns
        string leftSide = "~~//";
        string rightSide = "//~~";

        // Construct the title with side patterns
        string name = leftSide + title + rightSide;

        // Ensure the name fits within titleWidth
        int maxNameLength = titleWidth - 2; // Subtract borders
        if (name.Length > maxNameLength)
        {
            // Truncate the title to fit
            int maxTitleLength = maxNameLength - leftSide.Length - rightSide.Length;
            title = title.Substring(0, Math.Max(maxTitleLength, 0));
            name = leftSide + title + rightSide;
        }

        // Calculate positions
        int nameStartX = x + (titleWidth - name.Length) / 2;
        int titleY = y + 1;

        // Write the name
        GUI.SetCursorPosition(nameStartX, titleY);
        GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.CYAN.r, ColorSpectrum.CYAN.g, ColorSpectrum.CYAN.b)}{name}{GUI.ResetColor()}");

        DrawBox(Console.WindowWidth / 2 - titleWidth / 2, 2, titleWidth, titleHeight, " ");
        // Centered and fancy signature
        int centerX = Console.WindowWidth / 2;
        int centerY = topPadding / 2;

        string signature1 = "** Made by: @cybutr **";
        string signature2 = "* On GitHub *";

        GUI.SetCursorPosition(centerX - signature1.Length / 2, centerY);
        GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.MAGENTA.r, ColorSpectrum.MAGENTA.g, ColorSpectrum.MAGENTA.b)}{signature1}{GUI.ResetColor()}");

        GUI.SetCursorPosition(centerX - signature2.Length / 2, centerY + 1);
        GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.CYAN.r, ColorSpectrum.CYAN.g, ColorSpectrum.CYAN.b)}{signature2}{GUI.ResetColor()}");
    }
    public void DisplayOutputLog(int outputWidth, int outputHeight, int titleWidth)
    {
        int startX = Console.WindowWidth / 2 + titleWidth / 2 - 1;
        int startY = 2;

        if (startX < 0) startX = 0;
        if (startY < 0) startY = 0;

        DrawBox(startX, 0, outputWidth, 3, "Current Event");
        string text = eventBuffer.LastOrDefault() ?? "";
        int textLength = text.Length;
        int xPosition = startX + (outputWidth - textLength) / 2;
        GUI.SetCursorPosition(xPosition, 1);
        GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.CYAN.r, ColorSpectrum.CYAN.g, ColorSpectrum.CYAN.b)}{text}{GUI.ResetColor()}");
        DrawBox(startX, startY, outputWidth, outputHeight - 3, " ");
        int cursorX = startX + 2;
        int cursorY = startY + 1;

        int maxLines = outputHeight - 5;
        _ = Math.Min(actualOutputBuffer.Count, maxLines);

        // Define keyword-color mapping using ColorSpectrum
        Dictionary<string, (int r, int g, int b)> keywordColors = new Dictionary<string, (int r, int g, int b)>(StringComparer.OrdinalIgnoreCase)
        {
            { "Added", ColorSpectrum.GREEN },
            { "Removed", ColorSpectrum.RED },
            { "Updated", ColorSpectrum.YELLOW },
            { "Regenerated", ColorSpectrum.CYAN },
            { "Chamber", ColorSpectrum.DARKER_BLUE },
            { "Error", ColorSpectrum.RED },
            { "Warning", ColorSpectrum.YELLOW },
            { "Info", ColorSpectrum.CYAN },
            { "Debug", ColorSpectrum.DARK_GREY },
            { "Crab", ColorSpectrum.RED },
            { "Turtle", ColorSpectrum.GREEN },
            { "Sheep", ColorSpectrum.SILVER },
            { "Cow", ColorSpectrum.SILVER },
            { "Pig", ColorSpectrum.PINK },
            { "Chicken", ColorSpectrum.YELLOW },
            { "Fox", ColorSpectrum.ORANGE },
            { "Rabbit", ColorSpectrum.GREY },
            { "Wolf", ColorSpectrum.GREY },
            { "Bear", ColorSpectrum.BROWN },
            { "Goat", ColorSpectrum.DARK_GREY},
            { "Fish", ColorSpectrum.BLUE_VIOLET},
            { "Bird", ColorSpectrum.DARK_GREY},
            { "Weather", ColorSpectrum.CYAN },
            { "Time", ColorSpectrum.LIGHT_BLUE },
            { "Season", ColorSpectrum.LIGHT_GREEN },
            { "Temperature", ColorSpectrum.GREEN },
            { "Humidity", ColorSpectrum.BLUE },
            { "Pressure", ColorSpectrum.MAGENTA },
            { "Wind", ColorSpectrum.ORANGE },
            { "Sunrise", ColorSpectrum.ORANGE },
            { "Sunset", ColorSpectrum.ORANGE },
            { "Day", ColorSpectrum.PINK },
            { "Cloud", ColorSpectrum.SILVER},
            { "Plains", ColorSpectrum.GREEN},
            { "Forest", ColorSpectrum.DARK_GREEN},
            { "Mountain", ColorSpectrum.GREY},
            { "Snow", ColorSpectrum.WHITE},
            { "Water", ColorSpectrum.BLUE},
            { "Beach", ColorSpectrum.YELLOW},
        };

        string Reset = "\u001b[0m";

        // Get the latest lines and reverse them to display newest at the top
        List<string> lines = actualOutputBuffer.Skip(Math.Max(0, actualOutputBuffer.Count - maxLines))
                                .Take(maxLines)
                                .Reverse()
                                .ToList();

        for (int i = 0; i < lines.Count; i++)
        {
            string line = lines[i];
            string[] words = line.Split(' ');
            GUI.SetCursorPosition(cursorX, cursorY + i);
            foreach (string word in words)
            {
                string trimmedWord = word.Trim(',', '.', '!', '?'); // Trim punctuation
                if (keywordColors.ContainsKey(trimmedWord))
                {
                    (int r, int g, int b) color = keywordColors[trimmedWord];
                    GUI.Write($"{GUI.SetForegroundColor(color.r, color.g, color.b)}{word}{Reset} ");
                }
                else
                {
                    GUI.Write($"{word} ");
                }
            }
        }
    }
    public void UpdateOutputLog(int outputWidth, int outputHeight, int titleWidth)
    {
        int startX = Console.WindowWidth / 2 + titleWidth / 2 - 1;
        int startY = 2;

        if (startX < 0) startX = 0;
        if (startY < 0) startY = 0;

        string text = eventBuffer.LastOrDefault() ?? "";
        int textLength = text.Length;
        int xPosition = startX + (outputWidth - textLength) / 2;
        GUI.SetCursorPosition(xPosition, 1);
        GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.CYAN.r, ColorSpectrum.CYAN.g, ColorSpectrum.CYAN.b)}{text}{GUI.ResetColor()}");
        int cursorX = startX + 2;
        int cursorY = startY + 1;

        int maxLines = outputHeight - 5;
        _ = Math.Min(actualOutputBuffer.Count, maxLines);

        // Define keyword-color mapping using ColorSpectrum
        Dictionary<string, (int r, int g, int b)> keywordColors = new Dictionary<string, (int r, int g, int b)>(StringComparer.OrdinalIgnoreCase)
        {
            { "Added", ColorSpectrum.GREEN },
            { "Removed", ColorSpectrum.RED },
            { "Updated", ColorSpectrum.YELLOW },
            { "Regenerated", ColorSpectrum.CYAN },
            { "Chamber", ColorSpectrum.DARKER_BLUE },
            { "Error", ColorSpectrum.RED },
            { "Warning", ColorSpectrum.YELLOW },
            { "Info", ColorSpectrum.CYAN },
            { "Debug", ColorSpectrum.DARK_GREY },
            { "Crab", ColorSpectrum.RED },
            { "Turtle", ColorSpectrum.GREEN },
            { "Sheep", ColorSpectrum.SILVER },
            { "Cow", ColorSpectrum.SILVER },
            { "Pig", ColorSpectrum.PINK },
            { "Chicken", ColorSpectrum.YELLOW },
            { "Fox", ColorSpectrum.ORANGE },
            { "Rabbit", ColorSpectrum.GREY },
            { "Wolf", ColorSpectrum.GREY },
            { "Bear", ColorSpectrum.BROWN },
            { "Goat", ColorSpectrum.DARK_GREY},
            { "Fish", ColorSpectrum.BLUE_VIOLET},
            { "Bird", ColorSpectrum.DARK_GREY},
            { "Weather", ColorSpectrum.CYAN },
            { "Time", ColorSpectrum.LIGHT_BLUE },
            { "Season", ColorSpectrum.LIGHT_GREEN },
            { "Temperature", ColorSpectrum.GREEN },
            { "Humidity", ColorSpectrum.BLUE },
            { "Pressure", ColorSpectrum.MAGENTA },
            { "Wind", ColorSpectrum.ORANGE },
            { "Sunrise", ColorSpectrum.ORANGE },
            { "Sunset", ColorSpectrum.ORANGE },
            { "Day", ColorSpectrum.PINK },
            { "Cloud", ColorSpectrum.SILVER},
            { "Plains", ColorSpectrum.GREEN},
            { "Forest", ColorSpectrum.DARK_GREEN},
            { "Mountain", ColorSpectrum.GREY},
            { "Snow", ColorSpectrum.WHITE},
            { "Water", ColorSpectrum.BLUE},
            { "Beach", ColorSpectrum.YELLOW},
        };
        string Reset = "\u001b[0m";

        // Get the latest lines and reverse them to display newest at the top
        List<string> lines = actualOutputBuffer.Skip(Math.Max(0, actualOutputBuffer.Count - maxLines))
                                .Take(maxLines)
                                .Reverse()
                                .ToList();

        foreach (string? line in lines)
        {
            string[] words = line.Split(' ');
            int currentX = cursorX;
            int currentY = cursorY;
            foreach (string word in words)
            {
                string trimmedWord = word.Trim(',', '.', '!', '?');
                string displayWord = word + " ";
                int wordLength = displayWord.Length;

                // Split word if it's longer than outputWidth - 3
                if (wordLength > outputWidth - 3)
                {
                    int splitIndex = outputWidth - 3;
                    string firstPart = displayWord.Substring(0, splitIndex);
                    string remainingPart = displayWord.Substring(splitIndex);

                    // Write the first part
                    if (currentX + firstPart.Length > startX + outputWidth - 2)
                    {
                        currentX = startX + 2;
                        currentY += 1;
                        if (currentY >= startY + maxLines)
                            break;
                    }

                    if (keywordColors.ContainsKey(trimmedWord))
                    {
                        (int r, int g, int b) color = keywordColors[trimmedWord];
                        GUI.SetCursorPosition(currentX, currentY);
                        GUI.Write($"{GUI.SetForegroundColor(color.r, color.g, color.b)}{firstPart}{Reset}");
                    }
                    else
                    {
                        GUI.SetCursorPosition(currentX, currentY);
                        GUI.Write($"{firstPart}");
                    }
                    currentX += firstPart.Length;

                    // Prepare the remaining part
                    if (!string.IsNullOrWhiteSpace(remainingPart))
                    {
                        currentX = startX + 2;
                        currentY += 1;
                        if (currentY >= startY + maxLines)
                            break;
                        displayWord = remainingPart;
                        wordLength = displayWord.Length;
                    }
                    else
                    {
                        continue;
                    }
                }

                // Check if the word fits in the current line
                if (currentX + wordLength > startX + outputWidth - 2)
                {
                    // Move to next line
                    currentX = startX + 2;
                    currentY += 1;
                    if (currentY >= startY + maxLines)
                        break;
                }

                // Write word with color
                if (keywordColors.ContainsKey(trimmedWord))
                {
                    (int r, int g, int b) color = keywordColors[trimmedWord];
                    GUI.SetCursorPosition(currentX, currentY);
                    GUI.Write($"{GUI.SetForegroundColor(color.r, color.g, color.b)}{word}{Reset} ");
                }
                else
                {
                    GUI.SetCursorPosition(currentX, currentY);
                    GUI.Write($"{word} ");
                }

                currentX += wordLength;
            }
        }
    }

    #endregion
    #region map config GUI
    public bool GetConfig()
    {
        DisplayMapConfig();
        GUI.SetCursorPosition(terminalCentre.x - saveWidth / 2 + saveWidth / 2 - 2 , terminalCentre.y + gamerulesHeight + structuresHeight - mapConfigHeight + heightOffset + bottomHeight + 1);
        GUI.Write(
            GUI.SetBackgroundColor(selectColor.r, selectColor.g, selectColor.b) +
            GUI.SetForegroundColor(ColorSpectrum.BLACK.r, ColorSpectrum.BLACK.g, ColorSpectrum.BLACK.b) +
            $"SAVE{GUI.ResetColor()}"
        );
        ManageParamNavigation();
        numberOfWaves = conf.NumberOfWaves;
        GUI.Clear();
        return false;
    }
    #region params
    public enum SettingType
    {
        MapConfig,
        Gamerules,
        Structures,
        Economy,
        Animals,
        Disasters,
        Visuals,
        Events
    } 
    public enum ParamType
    {
        Int,
        Double,
        Bool,
        String
    }
    public class ParamCoordinate
    {
        public string PropertyName { get; set; }
        public SettingType SettingType { get; set; }
        public ParamType ParamType { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        public ParamCoordinate(string propertyName, SettingType settingType, ParamType type, int x, int y)
        {
            PropertyName = propertyName;
            SettingType = settingType;
            ParamType = type;
            X = x;
            Y = y;
        }
    }
    public static List<ParamCoordinate> mapConfigParams {get; set;} = new List<ParamCoordinate>
    {
        new ParamCoordinate("Seed", SettingType.MapConfig, ParamType.String, 0, 0),
        new ParamCoordinate("NoiseScale", SettingType.MapConfig, ParamType.Double, 0, 0),
        new ParamCoordinate("ErosionFactor", SettingType.MapConfig, ParamType.Double, 0, 0),
        new ParamCoordinate("MinBiomeSize", SettingType.MapConfig, ParamType.Int, 0, 0),
        new ParamCoordinate("MinLakeSize", SettingType.MapConfig, ParamType.Int, 0, 0),
        new ParamCoordinate("MinRiverWidth", SettingType.MapConfig, ParamType.Int, 0, 0),
        new ParamCoordinate("MaxRiverWidth", SettingType.MapConfig, ParamType.Int, 0, 0),
        new ParamCoordinate("MinMountainWidth", SettingType.MapConfig, ParamType.Int, 0, 0),
        new ParamCoordinate("MaxMountainWidth", SettingType.MapConfig, ParamType.Int, 0, 0),
        new ParamCoordinate("RiverFlowChance", SettingType.MapConfig, ParamType.Double, 0, 0),
        new ParamCoordinate("PlainsHeightThreshold", SettingType.MapConfig, ParamType.Double, 0, 0),
        new ParamCoordinate("ForestHeightThreshold", SettingType.MapConfig, ParamType.Double, 0, 0),
        new ParamCoordinate("MountainHeightThreshold", SettingType.MapConfig, ParamType.Double, 0, 0),
        new ParamCoordinate("EnableRivers", SettingType.MapConfig, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableLakes", SettingType.MapConfig, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableMountainRanges", SettingType.MapConfig, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableTempatureBiomeChanges", SettingType.MapConfig, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableHumidityBiomeChanges", SettingType.MapConfig, ParamType.Bool, 0, 0),
        new ParamCoordinate("BiomeBlend", SettingType.MapConfig, ParamType.Int, 0, 0)
    };
    public static List<ParamCoordinate> gameruleParams {get; set;} = new List<ParamCoordinate>
    {
        new ParamCoordinate("EnableWildfires", SettingType.Gamerules, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableSecrets", SettingType.Gamerules, ParamType.Bool, 0, 0),
        new ParamCoordinate("DoTimeCycle", SettingType.Gamerules, ParamType.Bool, 0, 0),
        new ParamCoordinate("DoWeatherCycle", SettingType.Gamerules, ParamType.Bool, 0, 0)
    };
    public static List<ParamCoordinate> structureParams {get; set;} = new List<ParamCoordinate>
    {
        new ParamCoordinate("GenerateStructrs", SettingType.Structures, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableVillages", SettingType.Structures, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableCities", SettingType.Structures, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableDungeons", SettingType.Structures, ParamType.Bool, 0, 0)
    };
    public static List<ParamCoordinate> economyParams {get; set;} = new List<ParamCoordinate>
    {
        new ParamCoordinate("EnableTrades", SettingType.Economy, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableTrades", SettingType.Economy, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableCurrency", SettingType.Economy, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableTaxes", SettingType.Economy, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableBanks", SettingType.Economy, ParamType.Bool, 0, 0)
    };
    public static List<ParamCoordinate> animalParams {get; set;} = new List<ParamCoordinate>
    {
        new ParamCoordinate("GenerateAnimals", SettingType.Animals, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnablePredators", SettingType.Animals, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableAnimalMovement", SettingType.Animals, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableAnimalBreeding", SettingType.Animals, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableAnimalDeath", SettingType.Animals, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableAnimalExtinction", SettingType.Animals, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableAnimalMigration", SettingType.Animals, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableAnimalHunting", SettingType.Animals, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableAnimalDomestication", SettingType.Animals, ParamType.Bool, 0, 0)
    };
    public static List<ParamCoordinate> disasterParams {get; set;} = new List<ParamCoordinate>
    {
        new ParamCoordinate("EnableTornadoes", SettingType.Disasters, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableEarthquakes", SettingType.Disasters, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableVolcanoes", SettingType.Disasters, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableFloods", SettingType.Disasters, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableMeteors", SettingType.Disasters, ParamType.Bool, 0, 0)
    };
    public static List<ParamCoordinate> eventParams {get; set;} = new List<ParamCoordinate>
    {
        new ParamCoordinate("EnableRobberies", SettingType.Events, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableMurders", SettingType.Events, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableRiots", SettingType.Events, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnablePlagues", SettingType.Events, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableWars", SettingType.Events, ParamType.Bool, 0, 0)
    };
    public static List<ParamCoordinate> visualParams {get; set;} = new List<ParamCoordinate>
    {
        new ParamCoordinate("DisplayShadows", SettingType.Visuals, ParamType.Bool, 0, 0),
        new ParamCoordinate("DisplayWaves", SettingType.Visuals, ParamType.Bool, 0, 0),
        new ParamCoordinate("NumberOfWaves", SettingType.Visuals, ParamType.Int, 0, 0)
    };
    public static List<ParamCoordinate> nullParams {get; set;} = new List<ParamCoordinate>();
    public static List<ParamCoordinate>[,] allParams {get; set;} = new List<ParamCoordinate>[4, 3]
    {
        { gameruleParams, mapConfigParams, nullParams },
        { structureParams, eventParams, nullParams },
        { economyParams, animalParams, disasterParams },
        { nullParams, nullParams, visualParams }
    };
    #endregion
    public static string title {get; set;} = @"
   __  _ __  _   _   __ ___   ___  ___    ___
 ,'_/ /// /.' \ / \,' // o.) / _/ / o | ,' _/
/ /_ / ` // o // \,' // o \ / _/ /  ,' _\ `. 
|__//_n_//_n_//_/ /_//___,'/___//_/`_\/___,' 
";
    private int GetConfWindowWidth(List<ParamCoordinate> paramList)
    {
        switch (paramList)
        {
            case List<ParamCoordinate> _ when paramList == mapConfigParams:
                return configWidth - mapConfigOffset;
            case List<ParamCoordinate> _ when paramList == gameruleParams:
                return mapConfigOffset - 1;
            case List<ParamCoordinate> _ when paramList == structureParams:
                return mapConfigOffset - 1;
            case List<ParamCoordinate> _ when paramList == economyParams:
                return mapConfigOffset - 1;
            case List<ParamCoordinate> _ when paramList == animalParams:
                return (configWidth - mapConfigOffset) / 2 - 1;
            case List<ParamCoordinate> _ when paramList == disasterParams:
                return (configWidth - mapConfigOffset) / 2;
            case List<ParamCoordinate> _ when paramList == visualParams:
                return (configWidth - mapConfigOffset) / 2;
            case List<ParamCoordinate> _ when paramList == eventParams:
                return configWidth - mapConfigOffset;
            default:
                return 20;
        }
    }
    public static int currentParamX {get; set;}
    public static int currentParamY {get; set;}
    public static int configWidth {get; set;} = 95;
    public static int configHeight {get; set;} = 62;
    public static int mapConfigOffset {get; set;} = 25;
    public static int mapConfigHeight {get; set;} = configHeight / 2 - 10;
    public static int gamerulesHeight {get; set;} = configHeight / 2 - 20;
    public static int structuresHeight {get; set;} = 15;
    public static int bottomHeight {get; set;} = configHeight / 2 - 16;
    public static int saveWidth {get; set;} = 10;
    public static int heightOffset {get; set;} = Math.Max(0, (Console.WindowHeight - (10 + gamerulesHeight + structuresHeight + bottomHeight)) / 6);
    public (int r, int g, int b) selectColor {get; set;} = ColorSpectrum.SILVER;
    public static (int x, int y) terminalCentre {get; set;} = (Console.WindowWidth / 2, Console.WindowHeight / 2);
    public void CalculateParamCoordinates()
    {
        int centerX = Console.WindowWidth / 2;
        int startY =  terminalCentre.y - configHeight / 2 + heightOffset;
    
        // Calculate the starting X position based on configWidth to center the window
        int startX = centerX - (configWidth / 2);
    
        startY += 11;
        // Assign coordinates for Gamerules
        foreach (var param in gameruleParams)
        {
            param.X = startX + 2;
            param.Y = startY++;
        }
    
        startY += gamerulesHeight - gameruleParams.Count(); // Add spacing between sections
    
        // Assign coordinates for Structures
        foreach (var param in structureParams)
        {
            param.X = startX + 2;
            param.Y = startY++;
        }
    
        startY -= structureParams.Count() + 11; // Add spacing between sections
    
        // Assign coordinates for Map Config
        foreach (var param in mapConfigParams)
        {
            param.X = startX + mapConfigOffset + 2;
            param.Y = startY++;
        }
    
        startY += structuresHeight - structureParams.Count() * 2; // Add spacing between sections
    
        // Assign coordinates for Economy
        foreach (var param in economyParams)
        {
            param.X = startX + 2;
            param.Y = startY++;
        }
    
        startY -= economyParams.Count(); // Add spacing between sections
    
        // Assign coordinates for Animals
        foreach (var param in animalParams)
        {
            param.X = startX + mapConfigOffset + 2;
            param.Y = startY++;
        }
    
        startY -= animalParams.Count(); // Add spacing between sections
    
        // Assign coordinates for Disasters
        foreach (var param in disasterParams)
        {
            param.X = startX + mapConfigOffset + ((configWidth - mapConfigOffset) / 2) + 2;
            param.Y = startY++;
        }
    
        startY += 2; // Add spacing between sections
    
        // Assign coordinates for Visuals
        foreach (var param in visualParams)
        {
            param.X = startX + mapConfigOffset + ((configWidth - mapConfigOffset) / 2) + 2;
            param.Y = startY++;
        }
    
        startY -= animalParams.Count() * 2 - 3; // Add spacing between sections
    
        // Assign coordinates for Events
        for (int i = 0; i < eventParams.Count(); i++)
        {
            if (i < gamerulesHeight + structuresHeight - mapConfigHeight - 2)
            {
                eventParams[i].X = startX + mapConfigOffset + 2;
                eventParams[i].Y = startY++;
            }
            else
            {
                eventParams[i].X = startX + configWidth - eventParams[i].PropertyName.Length - 6;
                eventParams[i].Y = startY++ - gamerulesHeight - structuresHeight + mapConfigHeight + 2;
            }
        }
    
        // Ensure all parameters have valid coordinates
        foreach (var list in new List<List<ParamCoordinate>> 
        { 
            mapConfigParams, 
            gameruleParams, 
            structureParams, 
            economyParams, 
            animalParams, 
            disasterParams, 
            visualParams, 
            eventParams 
        })
        {
            foreach (var param in list)
            {
                // Clamp X and Y to console boundaries
                param.X = Math.Clamp(param.X, 0, Console.WindowWidth - 1);
                param.Y = Math.Clamp(param.Y, 0, Console.WindowHeight - 1);
            }
        }
    }
    private void DrawAllParams()
    {
        foreach (var param in mapConfigParams)
        {
            GUI.SetCursorPosition(param.X, param.Y);
            GUI.Write($"{param.PropertyName} - {GetParamValue(param)}");
        }
        foreach (var param in gameruleParams)
        {
            GUI.SetCursorPosition(param.X, param.Y);
            GUI.Write($"{param.PropertyName} - {GetParamValue(param)}");
        }
        foreach (var param in structureParams)
        {
            GUI.SetCursorPosition(param.X, param.Y);
            GUI.Write($"{param.PropertyName} - {GetParamValue(param)}");
        }
        foreach (var param in economyParams)
        {
            GUI.SetCursorPosition(param.X, param.Y);
            GUI.Write($"{param.PropertyName} - {GetParamValue(param)}");
        }
        foreach (var param in animalParams)
        {
            GUI.SetCursorPosition(param.X, param.Y);
            GUI.Write($"{param.PropertyName} - {GetParamValue(param)}");
        }
        foreach (var param in disasterParams)
        {
            GUI.SetCursorPosition(param.X, param.Y);
            GUI.Write($"{param.PropertyName} - {GetParamValue(param)}");
        }
        foreach (var param in visualParams)
        {
            GUI.SetCursorPosition(param.X, param.Y);
            GUI.Write($"{param.PropertyName} - {GetParamValue(param)}");
        }
        foreach (var param in eventParams)
        {
            GUI.SetCursorPosition(param.X, param.Y);
            GUI.Write($"{param.PropertyName} - {GetParamValue(param)}");
        }
    }
    public string GetParamValue(ParamCoordinate param)
    {
        switch (param.ParamType)
        {
            case ParamType.Bool:
                return GetParamBool(param);
            case ParamType.Int:
                return GetParamInt(param).ToString();
            case ParamType.Double:
                return GetParamDouble(param).ToString();
            case ParamType.String:
                return GetParamString(param);
            default:
                return "";
        }
    }
    private string GetParamBool(ParamCoordinate param)
    {
        // Get the property value from conf using reflection
        bool value = conf.GetType().GetProperty(param.PropertyName)?.GetValue(conf) as bool? ?? false;
        // Choose the display character based on the value
        string displayChar = value ? "✔" : "✖";

        // Return the display character instead of writing to console
        return displayChar;
    }
    private int GetParamInt(ParamCoordinate param)
    {
        // Get the property value from conf using reflection
        int value = conf.GetType().GetProperty(param.PropertyName)?.GetValue(conf) as int? ?? 0;

        // Return the value instead of writing to console
        return value;
    }
    private double GetParamDouble(ParamCoordinate param)
    {
        // Get the property value from conf using reflection
        double value = conf.GetType().GetProperty(param.PropertyName)?.GetValue(conf) as double? ?? 0.0f;

        // Return the value instead of writing to console
        return value;
    }
    private string GetParamString(ParamCoordinate param)
    {
        // Get the property value from conf using reflection
        string value = conf.GetType().GetProperty(param.PropertyName)?.GetValue(conf) as string ?? "";

        // Return the value instead of writing to console
        return value;
    }
    public void ManageParamNavigation()
    {
        void DrawParam(ParamCoordinate param, int redrawDistance, bool isSelected, string? tempStringValue = null)
        {
            GUI.SetCursorPosition(param.X, param.Y);
            // Base value from config
            string value = param.ParamType switch
            {
                ParamType.Bool => GetParamBool(param),
                ParamType.Int => GetParamInt(param).ToString(),
                ParamType.Double => GetParamDouble(param).ToString("0.##"),
                ParamType.String => GetParamString(param),
                _ => ""
            };
            // If we're editing any parameter type, show the temporary string instead
            if (tempStringValue != null && (param.ParamType == ParamType.String || param.ParamType == ParamType.Int || param.ParamType == ParamType.Double))
            {
                value = tempStringValue;
            }

            GUI.SetCursorPosition(param.X, param.Y);
            if (isSelected)
            {
                if (param.ParamType != ParamType.Bool)
                {
                    GUI.Write(
                        GUI.SetBackgroundColor(selectColor.r, selectColor.g, selectColor.b) +
                        GUI.SetForegroundColor(ColorSpectrum.BLACK.r, ColorSpectrum.BLACK.g, ColorSpectrum.BLACK.b) +
                        $"{param.PropertyName} - {value}{GUI.ResetColor()}{new string(' ', Math.Max(redrawDistance, 0))}"
                    );
                }
                else
                {
                    GUI.Write(
                        GUI.SetBackgroundColor(selectColor.r, selectColor.g, selectColor.b) +
                        GUI.SetForegroundColor(ColorSpectrum.BLACK.r, ColorSpectrum.BLACK.g, ColorSpectrum.BLACK.b) +
                        $"{param.PropertyName} - {value}{GUI.ResetColor()}"
                    );
                }
            }
            else
            {
                if (param.ParamType != ParamType.Bool)
                    GUI.Write($"{param.PropertyName} - {value}{new string(' ', redrawDistance)}");
                else
                    GUI.Write($"{param.PropertyName} - {value}");
            }
        }

        currentParamX = 2;
        currentParamY = 1;
        List<ParamCoordinate> currentList = allParams[currentParamX, currentParamY];
        int listParamIndex = currentList.Count - 1;
        int redrawDistance = listParamIndex >= 0
            ? GetRedrawDistance(currentList, currentList[listParamIndex]) : 0;
        
        bool typingString = false;
        string tempStringValue = "";
        bool isSave = true;
        bool selecting = true;
        while (selecting)
        {
            ConsoleKeyInfo key = Console.ReadKey(true);
            if (!typingString)
            {
                int oldIndex = listParamIndex;
                int oldX = currentParamX;
                int oldY = currentParamY;
                ParamCoordinate ?oldParam = oldIndex >= 0 ? currentList[oldIndex] : null;
                int oldRedrawDistance = oldParam != null
                    ? GetRedrawDistance(currentList, oldParam, true) : 0;
                switch (key.Key)
                {
                    case ConsoleKey.Escape:
                        conf.ShouldSave = false;
                        return;
                    case ConsoleKey.UpArrow:
                    case ConsoleKey.W:
                        // Special 2-column logic for x=1, y=1
                        if (currentParamX == 1 && currentParamY == 1 && currentList.Count > 1)
                        {
                            int halfCount = (int)Math.Ceiling(currentList.Count / 2.0);
                            bool isLeftSide = listParamIndex < halfCount;
                            if (isLeftSide)
                            {
                                // If not at top, move up; else go x-1
                                if (listParamIndex > 0)
                                    listParamIndex--;
                                else
                                {
                                    currentParamX--;
                                    currentList = allParams[currentParamX, currentParamY];
                                    listParamIndex = currentList.Count - 1;
                                }
                            }
                            else
                            {
                                // If not at top of right, move up; else go x-1
                                if (listParamIndex > halfCount)
                                    listParamIndex--;
                                else
                                {
                                    currentParamX--;
                                    currentList = allParams[currentParamX, currentParamY];
                                    listParamIndex = currentList.Count - 1;
                                }
                            }
                        }
                        else
                        {
                            if (listParamIndex > 0 && !isSave)
                                listParamIndex--;
                            else if (currentParamX == 2 && currentParamY == 1 & !isSave)
                            {
                                currentParamX--;
                                currentList = eventParams;
                                listParamIndex = (int)Math.Ceiling(currentList.Count / 2.0) - 1;
                            }
                            else if (currentParamX == 2 && currentParamY == 2 && !isSave)
                            {
                                currentParamX--;
                                currentParamY--;
                                currentList = eventParams;
                                listParamIndex = currentList.Count - 1;
                            }
                            else if (!isSave)
                            {
                                if (currentParamX > 0
                                    && allParams[currentParamX - 1, currentParamY] != null
                                    && allParams[currentParamX - 1, currentParamY].Count > 0)
                                {
                                    currentParamX--;
                                    currentList = allParams[currentParamX, currentParamY];
                                    listParamIndex = currentList.Count - 1;
                                }
                            }
                            else isSave = false;
                        }
                        break;

                    case ConsoleKey.DownArrow:
                    case ConsoleKey.S:
                        // Special 2-column logic for x=1, y=1
                        if (currentParamX == 1 && currentParamY == 1 && currentList.Count > 1)
                        {
                            int halfCount = (int)Math.Ceiling(currentList.Count / 2.0);
                            bool isLeftSide = listParamIndex < halfCount;
                            if (isLeftSide)
                            {
                                // Go down if not bottom; else x + 1
                                if (listParamIndex < halfCount - 1)
                                    listParamIndex++;
                                else
                                {
                                    currentParamX++;
                                    currentList = allParams[currentParamX, currentParamY];
                                    listParamIndex = 0;
                                }
                            }
                            else
                            {
                                // Go down if not bottom; else x+1
                                if (listParamIndex < currentList.Count - 1)
                                    listParamIndex++;
                                else
                                {
                                    currentParamX++;
                                    currentParamY++;
                                    currentList = allParams[currentParamX, currentParamY];
                                    listParamIndex = 0;
                                }
                            }
                        }
                        else
                        {
                            if (listParamIndex == currentList.Count - 1 && currentParamY == 1 && currentParamX == 2 && !isSave)
                            {
                                isSave = true;
                                DrawParam(currentList[listParamIndex], redrawDistance, false);
                            }
                            else if (listParamIndex < currentList.Count - 1 && listParamIndex >= 0 && !isSave)
                                listParamIndex++;
                            else if (!isSave)
                            {
                                if (currentParamX < allParams.GetLength(0) - 1
                                    && allParams[currentParamX + 1, currentParamY] != null
                                    && allParams[currentParamX + 1, currentParamY].Count > 0)
                                {
                                    currentParamX++;
                                    currentList = allParams[currentParamX, currentParamY];
                                    listParamIndex = 0;
                                }
                            }
                        }
                        break;

                    case ConsoleKey.LeftArrow:
                    case ConsoleKey.A:
                        // Check if Ctrl is held for numeric value modification
                        if ((key.Modifiers & ConsoleModifiers.Control) != 0 && listParamIndex >= 0 && !isSave)
                        {
                            var param = currentList[listParamIndex];
                            if (param.ParamType == ParamType.Int || param.ParamType == ParamType.Double)
                            {
                                ModifyParamValue(param, false); // Decrease value
                                DrawParam(param, redrawDistance, true);
                                break;
                            }
                        }
                        
                        // Special 2-column logic for x=1, y=1
                        if (currentParamX == 1 && currentParamY == 1 && currentList.Count > 1)
                        {
                            int halfCount = (int)Math.Ceiling(currentList.Count / 2.0);
                            bool isLeftSide = listParamIndex < halfCount;
                            if (!isLeftSide)
                            {
                                // Move to first item on left side
                                listParamIndex = 0;
                            }
                            else
                            {
                                // Regular behavior if on left already
                                if (currentParamY > 0
                                    && allParams[currentParamX, currentParamY - 1] != null
                                    && allParams[currentParamX, currentParamY - 1].Count > 0)
                                {
                                    currentParamY--;
                                    currentList = allParams[currentParamX, currentParamY];
                                    listParamIndex = 0;
                                }
                            }
                        }
                        else
                        {
                            if (currentParamY == 2 && currentParamX == 3 && !isSave)
                            {
                                currentParamY--;
                                currentParamX--;
                                currentList = allParams[currentParamX, currentParamY];
                                listParamIndex = currentList.Count - 1;
                            }
                            else if (currentParamY > 0
                                && allParams[currentParamX, currentParamY - 1] != null
                                && allParams[currentParamX, currentParamY - 1].Count > 0
                                && !isSave)
                            {
                                currentParamY--;
                                currentList = allParams[currentParamX, currentParamY];
                                listParamIndex = 0;
                            }
                        }
                        break;

                    case ConsoleKey.RightArrow:
                    case ConsoleKey.D:
                        // Check if Ctrl is held for numeric value modification
                        if ((key.Modifiers & ConsoleModifiers.Control) != 0 && listParamIndex >= 0 && !isSave)
                        {
                            var param = currentList[listParamIndex];
                            if (param.ParamType == ParamType.Int || param.ParamType == ParamType.Double)
                            {
                                ModifyParamValue(param, true); // Increase value
                                DrawParam(param, redrawDistance, true);
                                break;
                            }
                        }
                        
                        // Special 2-column logic for x=1, y=1
                        if (currentParamX == 1 && currentParamY == 1 && currentList.Count > 1)
                        {
                            int halfCount = (int)Math.Ceiling(currentList.Count / 2.0);
                            bool isLeftSide = listParamIndex < halfCount;
                            if (isLeftSide)
                            {
                                // Move to the right side at matching row
                                listParamIndex = halfCount + listParamIndex;
                                if (listParamIndex >= currentList.Count) listParamIndex = currentList.Count - 1;
                            }
                        }
                        else
                        {
                            if (currentParamX == 2 && currentParamY == 1 && listParamIndex > (int)Math.Ceiling(currentList.Count / 2.0) - 1 && !isSave)
                            {
                                currentParamX++;
                                currentParamY++;
                                currentList = allParams[currentParamX, currentParamY];
                                listParamIndex = 0;
                            }
                            else if (currentParamY < allParams.GetLength(1) - 1
                                && allParams[currentParamX, currentParamY + 1] != null
                                && allParams[currentParamX, currentParamY + 1].Count > 0
                                && !isSave)
                            {
                                currentParamY++;
                                currentList = allParams[currentParamX, currentParamY];
                                listParamIndex = 0;
                            }
                        }
                        break;
                    case ConsoleKey.Spacebar:
                    case ConsoleKey.Enter:
                        if (listParamIndex >= 0 && !isSave)
                        {
                            var p = currentList[listParamIndex];
                            if (p.ParamType == ParamType.Bool)
                            {
                                ToggleBoolParam(p);
                                DrawParam(p, redrawDistance, true);
                            }
                            else if (p.ParamType == ParamType.String)
                            {
                                typingString = true;
                                tempStringValue = GetParamString(p);
                                DrawParam(p, redrawDistance, true, tempStringValue);
                            }
                            else if (p.ParamType == ParamType.Int || p.ParamType == ParamType.Double)
                            {
                                // Start editing mode for numeric values
                                typingString = true;
                                tempStringValue = p.ParamType == ParamType.Int ? 
                                    GetParamInt(p).ToString() : 
                                    GetParamDouble(p).ToString("0.##");
                                DrawParam(p, redrawDistance, true, tempStringValue);
                            }
                        }
                        else selecting = false;
                        break;
                }

                if (oldIndex >= 0 && oldParam != null && oldIndex < (allParams[oldX, oldY] ?? new()).Count && !isSave)
                {
                    DrawParam(allParams[oldX, oldY][oldIndex], oldRedrawDistance, false);
                }
                if (listParamIndex >= 0 && listParamIndex < currentList.Count && !isSave)
                {
                    redrawDistance = GetRedrawDistance(currentList, currentList[listParamIndex]);
                    DrawParam(currentList[listParamIndex], redrawDistance, true);
                }
                else if (isSave)
                {
                    GUI.SetCursorPosition(terminalCentre.x - saveWidth / 2 + saveWidth / 2 - 2 , terminalCentre.y + gamerulesHeight + structuresHeight - mapConfigHeight + heightOffset + bottomHeight + 1);
                    GUI.Write(
                        GUI.SetBackgroundColor(selectColor.r, selectColor.g, selectColor.b) +
                        GUI.SetForegroundColor(ColorSpectrum.BLACK.r, ColorSpectrum.BLACK.g, ColorSpectrum.BLACK.b) +
                        $"SAVE{GUI.ResetColor()}"
                    );
                }
                if (!isSave)
                {
                    GUI.SetCursorPosition(terminalCentre.x - saveWidth / 2 + saveWidth / 2 - 2 , terminalCentre.y + gamerulesHeight + structuresHeight - mapConfigHeight + heightOffset + bottomHeight + 1);
                    GUI.Write("SAVE");
                }
            }
            else
            {
                if (key.Key == ConsoleKey.Backspace)
                {
                    if (tempStringValue.Length > 0)
                    {
                        tempStringValue = tempStringValue[..^1];
                        DrawParam(currentList[listParamIndex], redrawDistance, true, tempStringValue);
                    }
                }
                else if (key.Key == ConsoleKey.Enter)
                {
                    typingString = false;
                    if (listParamIndex >= 0)
                    {
                        var param = currentList[listParamIndex];
                        object? parsedValue = null;
                        
                        // Parse the input based on parameter type
                        switch (param.ParamType)
                        {
                            case ParamType.String:
                                parsedValue = tempStringValue;
                                break;
                            case ParamType.Int:
                                if (int.TryParse(tempStringValue, out int intVal))
                                    parsedValue = intVal;
                                else
                                {
                                    // Invalid input, revert to original value
                                    tempStringValue = GetParamInt(param).ToString();
                                    DrawParam(param, redrawDistance, true);
                                    continue;
                                }
                                break;
                            case ParamType.Double:
                                if (double.TryParse(tempStringValue, out double doubleVal))
                                    parsedValue = Math.Round(doubleVal, 2);
                                else
                                {
                                    // Invalid input, revert to original value
                                    tempStringValue = GetParamDouble(param).ToString("0.##");
                                    DrawParam(param, redrawDistance, true);
                                    continue;
                                }
                                break;
                        }
                        
                        if (parsedValue != null)
                        {
                            SetParamValue(param, parsedValue, conf);
                        }
                        DrawParam(param, redrawDistance, true);
                    }
                }
                else if (key.Key == ConsoleKey.Escape)
                {
                    typingString = false;
                    if (listParamIndex >= 0)
                    {
                        var param = currentList[listParamIndex];
                        // Restore original value based on parameter type
                        tempStringValue = param.ParamType switch
                        {
                            ParamType.String => GetParamString(param),
                            ParamType.Int => GetParamInt(param).ToString(),
                            ParamType.Double => GetParamDouble(param).ToString("0.##"),
                            _ => ""
                        };
                        DrawParam(param, redrawDistance, true);
                    }
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    if (tempStringValue.Length < 20 && listParamIndex >= 0)
                    {
                        var param = currentList[listParamIndex];
                        bool isValidChar = param.ParamType switch
                        {
                            ParamType.String => true, // Allow any character for strings
                            ParamType.Int => char.IsDigit(key.KeyChar) || (key.KeyChar == '-' && tempStringValue.Length == 0), // Digits and minus at start
                            ParamType.Double => char.IsDigit(key.KeyChar) || key.KeyChar == '.' || (key.KeyChar == '-' && tempStringValue.Length == 0), // Digits, decimal point, and minus at start
                            _ => false
                        };

                        if (isValidChar)
                        {
                            // Additional validation for decimal point in doubles
                            if (param.ParamType == ParamType.Double && key.KeyChar == '.' && tempStringValue.Contains('.'))
                            {
                                // Don't allow multiple decimal points
                                return;
                            }

                            tempStringValue += key.KeyChar;
                            DrawParam(param, redrawDistance, true, tempStringValue);
                        }
                    }
                }
            }
            // GUI.SetCursorPosition(0, 0);
            // GUI.Write(isSave);
        }
    }
    private int GetRedrawDistance(List<ParamCoordinate> paramList, ParamCoordinate param, bool old = false)
    {
        return !old ? Math.Max(GetConfWindowWidth(paramList) - 2 - param.PropertyName.Length - 6 - GetParamValue(param).Length, 0) :
        Math.Max(GetConfWindowWidth(paramList) - 2 - param.PropertyName.Length - 6 - GetParamValue(param).Length, 0);
    }
    private void ModifyParamValue(ParamCoordinate param, bool increase)
    {
        if (param.ParamType == ParamType.Int)
        {
            int val = GetParamInt(param);
            int newVal = val + (increase ? 1 : -1);
            
            // Add reasonable constraints for certain parameters
            if (param.PropertyName.Contains("Size") || param.PropertyName.Contains("Width"))
            {
                newVal = Math.Max(1, newVal); // Minimum size of 1
            }
            else if (param.PropertyName == "NumberOfWaves")
            {
                newVal = Math.Max(0, Math.Min(100, newVal)); // Waves between 0-100
            }
            
            SetParamValue(param, newVal, conf);
        }
        else if (param.ParamType == ParamType.Double)
        {
            double val = GetParamDouble(param);
            double increment = increase ? 0.1 : -0.1;
            
            // Use different increment for scale values
            if (param.PropertyName.Contains("Scale"))
            {
                increment = increase ? 1.0 : -1.0;
            }
            
            double newVal = val + increment;
            
            // Add reasonable constraints
            if (param.PropertyName.Contains("Scale"))
            {
                newVal = Math.Max(0.1, newVal); // Minimum scale
            }
            else if (param.PropertyName.Contains("Factor"))
            {
                newVal = Math.Max(0, newVal); // Non-negative factors
            }
            
            SetParamValue(param, Math.Round(newVal, 2), conf);
        }
    }
    private void ToggleBoolParam(ParamCoordinate param)
    {
        bool current = GetParamBool(param) == "✔";
        SetParamValue(param, !current, conf);
    }
    private static void SetParamValue(ParamCoordinate param, object newValue, Config config)
    {
        var prop = config.GetType().GetProperty(param.PropertyName);
        if (prop != null && prop.CanWrite)
        {
            try
            {
                var convertedValue = Convert.ChangeType(newValue, prop.PropertyType);
                prop.SetValue(config, convertedValue);
            }
            catch (Exception ex)
            {
                GUI.WriteLine($"Error setting property {param.PropertyName}: {ex.Message}");
            }
        }
    }
    public void SelectParam(ParamCoordinate param)
    {
        string value = param.ParamType switch
        {
            ParamType.Bool => GetParamBool(param),
            ParamType.Int => GetParamInt(param).ToString(),
            ParamType.Double => GetParamDouble(param).ToString(),
            ParamType.String => GetParamString(param),
            _ => ""
        };
        GUI.SetCursorPosition(param.X, param.Y);
        GUI.Write(
            $"{GUI.SetBackgroundColor(selectColor.r, selectColor.g, selectColor.g)}" +
            $"{GUI.SetForegroundColor(ColorSpectrum.BLACK.r, ColorSpectrum.BLACK.g, ColorSpectrum.BLACK.b)}" +
            $"{param.PropertyName} - {value}{GUI.ResetColor()}"
        );
    }
    public void DisplayMapConfig()
    {
        GUI.Clear();
        (int r, int g, int b) tColor = ColorSpectrum.CYAN;
        DrawColoredBox(terminalCentre.x - configWidth / 2, terminalCentre.y - configHeight / 2 + heightOffset, configWidth, 10, "", ColorSpectrum.LIGHT_CYAN); // Title
        DrawColoredBox(terminalCentre.x - configWidth / 2 + mapConfigOffset, terminalCentre.y - configHeight / 2 + 10 + heightOffset, configWidth - mapConfigOffset, mapConfigHeight, "Map Config", ColorSpectrum.BURNT_ORANGE); // Map Config
        DrawColoredBox(terminalCentre.x - configWidth / 2 + mapConfigOffset, terminalCentre.y - configHeight / 2 + 10 + configHeight / 2 - 10 + heightOffset, configWidth - mapConfigOffset, gamerulesHeight + structuresHeight - mapConfigHeight, "Events", ColorSpectrum.YELLOW); // Events
        DrawColoredBox(terminalCentre.x - configWidth / 2, terminalCentre.y - configHeight / 2 + 10 + heightOffset, mapConfigOffset - 1, gamerulesHeight, "Gamerules", ColorSpectrum.LIGHT_CORAL); // Gamerules
        DrawColoredBox(terminalCentre.x - configWidth / 2, terminalCentre.y - configHeight / 2 + 10 + configHeight / 2 - 20 + heightOffset, mapConfigOffset - 1, structuresHeight, "Structures", ColorSpectrum.BROWN); // Structures
        DrawColoredBox(terminalCentre.x - configWidth / 2, terminalCentre.y - configHeight / 2 + 10 + configHeight / 2 - 20 + structuresHeight + heightOffset, mapConfigOffset - 1, bottomHeight, "Economy", ColorSpectrum.GREEN); // Economy
        DrawColoredBox(terminalCentre.x - configWidth / 2 + mapConfigOffset, terminalCentre.y + gamerulesHeight + structuresHeight - mapConfigHeight + heightOffset, (configWidth - mapConfigOffset) / 2 - 1, bottomHeight, "Animals", ColorSpectrum.PALE_TURQUOISE); // Animals
        DrawColoredBox(terminalCentre.x - configWidth / 2 + mapConfigOffset + (configWidth - mapConfigOffset) / 2, terminalCentre.y + gamerulesHeight + structuresHeight - mapConfigHeight + heightOffset, (configWidth - mapConfigOffset) / 2, bottomHeight / 2, "Disasters", ColorSpectrum.INDIAN_RED); // Disasters
        DrawColoredBox(terminalCentre.x - configWidth / 2 + mapConfigOffset + (configWidth - mapConfigOffset) / 2, terminalCentre.y + gamerulesHeight + structuresHeight - mapConfigHeight + bottomHeight / 2 + heightOffset, (configWidth - mapConfigOffset) / 2, bottomHeight % 2 == 0 ? bottomHeight / 2 : bottomHeight / 2 + 1, "Visuals", ColorSpectrum.LIGHT_STEEL_BLUE);  // Visuals
        DrawColoredBox(terminalCentre.x - saveWidth / 2, terminalCentre.y + gamerulesHeight + structuresHeight - mapConfigHeight + heightOffset + bottomHeight, saveWidth, 3, "", ColorSpectrum.LIGHT_CYAN); // Bottom
        DisplayCenteredTextAtCords(title, terminalCentre.x, terminalCentre.y - configHeight / 2 + heightOffset + 5, tColor);
        GUI.SetCursorPosition(terminalCentre.x - saveWidth / 2 + saveWidth / 2 - 2 , terminalCentre.y + gamerulesHeight + structuresHeight - mapConfigHeight + heightOffset + bottomHeight + 1);
        GUI.Write("SAVE");
        CalculateParamCoordinates();
        DrawAllParams();
    }
    #endregion
}
