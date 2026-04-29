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
        double time = Math.Round(dayNight.TimeOfDay, 2);
        double season = Math.Round(dayNight.Season, 2);
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
        GUIConfig config = new(consoleWidth, consoleHeight);

        // Check console size
        if (consoleWidth < config.MinConsoleWidth || consoleHeight < config.MinConsoleHeight)
        {
            Clear();
            SetCursorPosition(0, 0);
            Write("Please resize the console window to a larger size.");
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
            // Weather Radar / Stats / Time — only when wide enough
            if (config.StatsWidth > 0)
            {
                _guiBuf.DrawBox(0, 0, config.RadarWidth, config.RadarHeight, "Weather Radar", ColorSpectrum.GREEN, HashCode.Combine(config.RadarWidth, config.RadarHeight));
                DisplayWeatherRadar(0, 0, config.RadarWidth, config.RadarHeight);
                DisplayTimeInfo(time, season, config.RadarWidth, config.TimeWidth, config.TimeHeight);
                DisplayWeatherStats(
                    currentWeather, nextWeather, temperature, humidity, pressure, windSpeed, windDirection,
                    config.RadarWidth, config.StatsWidth, config.StatsHeight
                );
            }
            // Thanks Info
            if (Console.WindowWidth > 200 && rightMargin >= 20) DisplayThanksMessage(config.ThanksWidth, config.ThanksHeight);
            // Title
            DisplayTitleAndSignature(config.TitleWidth, config.TitleHeight, "Chambers");
            // Output Log
            DisplayOutputLog(config.OutputWidth, config.OutputHeight, config.TitleWidth);
        }
        else
        {
            // Display message if the console is too small
            Clear();
            SetCursorPosition(0, 0);
            Write("Please resize the console window to a larger size.");
            shouldSimulationContinue = false;
        }
        if (rightMargin >= 20)
        {
            // Help Info
            DisplayHelpInfo(config.HelpWidth, config.HelpHeight);
            // Tile Info
            DisplayTileInfo(config.TileWidth, config.TileHeight);
        }

    }
    private void DisplayWeatherRadar(int x, int y, int radarW, int radarH)
    {
        int innerW = radarW - 2;
        int innerH = radarH - 2;
        int cloudW = cloudData.GetLength(0);
        int cloudH = cloudData.GetLength(1);
        double windDeg = weather.WindDirection % 360.0;
        if (windDeg < 0) windDeg += 360.0;

        if (_radarCache.GetLength(0) != innerW || _radarCache.GetLength(1) != innerH || tick - _radarCacheTick >= 3)
        {
            _radarCache = new int[innerW, innerH];
            int stepX = Math.Max(1, cloudW / innerW);
            int stepY = Math.Max(1, cloudH / innerH);
            for (int ry = 0; ry < innerH; ry++)
            {
                int y0 = ry * cloudH / innerH;
                int y1 = Math.Min(cloudH, y0 + stepY);
                for (int rx = 0; rx < innerW; rx++)
                {
                    int x0 = rx * cloudW / innerW;
                    int x1 = Math.Min(cloudW, x0 + stepX);
                    int cloudCount = 0, totalCells = 0, maxIntensity = 0;
                    for (int cy = y0; cy < y1; cy++)
                    for (int cx = x0; cx < x1; cx++)
                    {
                        totalCells++;
                        if (cloudData[cx, cy] == CloudType.None) continue;
                        cloudCount++;
                        int ti = cloudData[cx, cy] switch
                        {
                            CloudType.Cirrus       => 1,
                            CloudType.Stratus      => 1,
                            CloudType.Altocumulus  => 2,
                            CloudType.Cumulus      => 2,
                            CloudType.Nimbostratus => 3,
                            CloudType.Cumulonimbus => 5,
                            _                      => 2
                        } + cloudDepthData[cx, cy] / 2;
                        if (ti > maxIntensity) maxIntensity = ti;
                    }
                    _radarCache[rx, ry] = (cloudCount == 0 || cloudCount * 4 < totalCells) ? 0 : maxIntensity;
                }
            }
            _radarCacheTick = tick;
        }

        for (int ry = 0; ry < innerH; ry++)
        for (int rx = 0; rx < innerW; rx++)
        {
            int maxIntensity = _radarCache[rx, ry];
            if (maxIntensity == 0)
            {
                _guiBuf.Set(x + 1 + rx, y + 1 + ry, ' ', default, false, default, false, HashCode.Combine(0, rx, ry));
                continue;
            }
            (char ch, (int r, int g, int b) fg) = maxIntensity switch
            {
                1    => ('.', (0,   180, 0)),
                2    => ('o', (0,   230, 0)),
                3    => ('*', (230, 230, 0)),
                4    => ('*', (230, 100, 0)),
                >= 5 => ('@', (210, 0,   210)),
                _    => (' ', (0,   0,   0))
            };
            _guiBuf.Set(x + 1 + rx, y + 1 + ry, ch, fg, true, default, false, HashCode.Combine(maxIntensity, rx, ry));
        }

        int arrowIdx = (int)(((windDeg + 22.5) / 45.0) % 8);
        string arrow = arrowIdx switch
        {
            0 => "→",
            1 => "↘",
            2 => "↓",
            3 => "↙",
            4 => "←",
            5 => "↖",
            6 => "↑",
            7 => "↗",
            _ => "+"
        };
        _guiBuf.Set(x + 1 + innerW / 2, y + 1 + innerH / 2, arrow[0], (0, 200, 220), true, default, false, arrowIdx + 1);
    }
    private void DisplayWeatherStats(WeatherType currentWeather, WeatherType nextWeather, double temperature, double humidity,
    double pressure, double windSpeed, double windDirection, int radarWidth, int statsWidth, int statsHeight)
    {
        string curDisp = statsWidth < 43 ? GetShortWeatherName(currentWeather) : currentWeather.ToString();
        string nxtDisp = statsWidth < 43 ? GetShortWeatherName(nextWeather) : nextWeather.ToString();
        int bv = HashCode.Combine(radarWidth, statsWidth);
        _guiBuf.DrawBox(radarWidth - 1, 0, statsWidth + 2, 3, "Weather Stats", ColorSpectrum.GREEN, bv);
        _guiBuf.WriteLine(radarWidth + 1, 1, "Current: ", ColorSpectrum.YELLOW, $"{curDisp}, Next: {nxtDisp}".PadRight(statsWidth - 10), ColorSpectrum.WHITE, statsWidth - 1, HashCode.Combine(curDisp, nxtDisp));
        _guiBuf.DrawBox(radarWidth - 1, 2, statsWidth + 2, statsHeight, " ", ColorSpectrum.GREEN, HashCode.Combine(bv, 2));
        _guiBuf.WriteLine(radarWidth + 1, 3, "Cloud Formations: ", ColorSpectrum.CYAN, cloudFormations.ToString().PadRight(7), ColorSpectrum.WHITE, statsWidth - 1, cloudFormations);
        _guiBuf.WriteLine(radarWidth + 1, 4, "Cloud Tiles: ", ColorSpectrum.CYAN, cloudTileCount.ToString().PadRight(7), ColorSpectrum.WHITE, statsWidth - 1, cloudTileCount);
        _guiBuf.WriteLine(radarWidth + 1, 5, "Temperature: ", ColorSpectrum.GREEN, $"{temperature}°C".PadRight(9), ColorSpectrum.WHITE, statsWidth - 1, HashCode.Combine(temperature));
        _guiBuf.WriteLine(radarWidth + 1, 6, "Humidity: ", ColorSpectrum.BLUE, $"{humidity}%".PadRight(9), ColorSpectrum.WHITE, statsWidth - 1, HashCode.Combine(humidity));
        _guiBuf.WriteLine(radarWidth + 1, 7, "Pressure: ", ColorSpectrum.MAGENTA, $"{pressure}hPa".PadRight(9), ColorSpectrum.WHITE, statsWidth - 1, HashCode.Combine(pressure));
        _guiBuf.WriteLine(radarWidth + 1, 8, "Wind Speed: ", ColorSpectrum.ORANGE, $"{windSpeed}m/s".PadRight(9), ColorSpectrum.WHITE, statsWidth - 1, HashCode.Combine(windSpeed));
        _guiBuf.WriteLine(radarWidth + 1, 9, "Wind Direction: ", ColorSpectrum.PURPLE, $"{windDirection}°".PadRight(9), ColorSpectrum.WHITE, statsWidth - 1, HashCode.Combine(windDirection));
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
        int bx = radarWidth + statsWidth;
        int titleX = Console.WindowWidth / 2 - 25;
        int roundedStatsWidth = Math.Max(4, titleX - (bx - 2) + 1);
        _guiBuf.DrawBox(bx - 2, 0, roundedStatsWidth, statsHeight + 2, "Time Info", ColorSpectrum.GREEN, HashCode.Combine(bx, roundedStatsWidth));
        _guiBuf.WriteLine(bx, 1, " Time: ", ColorSpectrum.LIGHT_BLUE, $"{time:F2}h".PadRight(12), ColorSpectrum.WHITE, statsWidth - 3, HashCode.Combine(time));
        _guiBuf.WriteLine(bx, 2, " Season: ", ColorSpectrum.LIGHT_GREEN, $"{season}".PadRight(10), ColorSpectrum.WHITE, statsWidth - 3, HashCode.Combine(season));
        _guiBuf.WriteLine(bx, 3, " Sunrise: ", ColorSpectrum.ORANGE, $"{dayNight.SunriseTime}h".PadRight(10), ColorSpectrum.WHITE, statsWidth - 3, HashCode.Combine(dayNight.SunriseTime));
        _guiBuf.WriteLine(bx, 4, " Sunset: ", ColorSpectrum.ORANGE, $"{dayNight.SunsetTime}h".PadRight(10), ColorSpectrum.WHITE, statsWidth - 3, HashCode.Combine(dayNight.SunsetTime));

        string untilLabel;
        (int r, int g, int b) untilColor;
        string untilValue;
        if (time < dayNight.SunriseTime)
        {
            untilLabel = " Time Until Sunrise: ";
            untilColor = ColorSpectrum.GREEN;
            untilValue = $"{dayNight.SunriseTime - time:F2}h".PadRight(10);
        }
        else if (time >= dayNight.SunriseTime && time < dayNight.SunsetTime)
        {
            untilLabel = " Time Until Sunset: ";
            untilColor = ColorSpectrum.RED;
            untilValue = $"{dayNight.SunsetTime - time:F2}h".PadRight(10);
        }
        else
        {
            untilLabel = " Time Until Sunrise: ";
            untilColor = ColorSpectrum.GREEN;
            untilValue = $"{24.0 - time + dayNight.SunriseTime:F2}h".PadRight(10);
        }
        _guiBuf.WriteLine(bx, 5, untilLabel, untilColor, untilValue, ColorSpectrum.WHITE, statsWidth - 3, HashCode.Combine(untilValue));
        _guiBuf.WriteLine(bx, 6, " Day: ", ColorSpectrum.PINK, $"{DayCount}".PadRight(10), ColorSpectrum.WHITE, statsWidth - 3, HashCode.Combine(DayCount));
    }
    private void DisplayHelpInfo(int helpWidth, int helpHeight)
    {
        int bx = Console.WindowWidth - rightPadding * 2;
        int by = Console.WindowHeight - bottomPadding - helpHeight;
        int cx = bx + 2;
        string line = new('-', helpWidth - 3);
        _guiBuf.DrawBox(bx, by, helpWidth, helpHeight, "Help Menu", ColorSpectrum.GREEN, HashCode.Combine(bx, by, helpWidth, helpHeight));
        void Row(int offset, string text, bool isKey = false) =>
            _guiBuf.Write(cx, by + offset, text, isKey ? ColorSpectrum.YELLOW : ColorSpectrum.WHITE, isKey, default, false, text.GetHashCode());
        if (helpHeight < 30)
        {
            Row(1,  "P/Space:",              isKey: true);
            Row(2,  "Toggle updating");
            Row(3,  "PgUp/PgDn:",            isKey: true);
            Row(4,  "Increase/Decrease speed");
            Row(5,  "Q:",                    isKey: true);
            Row(6,  "Toggle cloud rendering");
            Row(7,  "Up/Down:",              isKey: true);
            Row(8,  "Go to last/first chamber");
            Row(9,  "Left/Right:",           isKey: true);
            Row(10, "Previous/Next chamber");
            Row(11, "1 - 9:",                isKey: true);
            Row(12, "Go to chamber 1 - 9");
            Row(13, "C:",                    isKey: true);
            Row(14, "Open console");
        }
        else
        {
            Row(1,  "P/Space:",              isKey: true);
            Row(2,  "Toggle updating");
            Row(3,  line);
            Row(4,  "PgUp/PgDn:",            isKey: true);
            Row(5,  "Increase/Decrease speed");
            Row(6,  line);
            Row(7,  "Q:",                    isKey: true);
            Row(8,  "Toggle cloud rendering");
            Row(9,  line);
            Row(10, "Up/Down:",              isKey: true);
            Row(11, "Go to last/first chamber");
            Row(12, line);
            Row(13, "Left/Right:",           isKey: true);
            Row(14, "Previous/Next chamber");
            Row(15, line);
            Row(16, "1 - 9:",                isKey: true);
            Row(17, "Go to chamber 1 - 9");
            Row(18, line);
            Row(19, "C:",                    isKey: true);
            Row(20, "Open console");
        }
    }
    private void DisplayTileInfo(int tileWidth, int tileHeight)
    {
        int bx = Console.WindowWidth - tileWidth;
        int by = topPadding + 1;
        int cx = bx + 2;
        _guiBuf.DrawBox(bx, by, tileWidth, tileHeight, "Tile Info", ColorSpectrum.GREEN, HashCode.Combine(bx, by, tileWidth));
        if (rightPadding >= 20)
        {
            _guiBuf.WriteLine(cx, by + 1, "Dark Green:", ColorSpectrum.DARK_GREEN, " Forest",              ColorSpectrum.WHITE, tileWidth - 3, 5001);
            _guiBuf.WriteLine(cx, by + 2, "Green:",      ColorSpectrum.GREEN,      " Plains",              ColorSpectrum.WHITE, tileWidth - 3, 5002);
            _guiBuf.WriteLine(cx, by + 3, "Grey / ",     ColorSpectrum.GREY,       "Dark Gray: Mountain",  ColorSpectrum.DARK_GREY, tileWidth - 3, 5003);
            _guiBuf.WriteLine(cx, by + 4, "White:",      ColorSpectrum.WHITE,      " Snow Peak",           ColorSpectrum.WHITE, tileWidth - 3, 5004);
            _guiBuf.WriteLine(cx, by + 5, "Blue:",       ColorSpectrum.BLUE,       " Water",               ColorSpectrum.WHITE, tileWidth - 3, 5005);
            _guiBuf.WriteLine(cx, by + 6, "Yellow:",     ColorSpectrum.YELLOW,     " Beach",               ColorSpectrum.WHITE, tileWidth - 3, 5006);
        }
        else
        {
            _guiBuf.Write(cx, by + 1,  "Dark Green:", ColorSpectrum.DARK_GREEN, true,  default, false, 5001);
            _guiBuf.Write(cx, by + 2,  "Forest",      ColorSpectrum.WHITE,      false, default, false, 5002);
            _guiBuf.Write(cx, by + 3,  "Green:",      ColorSpectrum.GREEN,      true,  default, false, 5003);
            _guiBuf.Write(cx, by + 4,  "Plains",      ColorSpectrum.WHITE,      false, default, false, 5004);
            _guiBuf.Write(cx, by + 5,  "Grey:",       ColorSpectrum.GREY,       true,  default, false, 5005);
            _guiBuf.Write(cx, by + 6,  "Mountain",    ColorSpectrum.WHITE,      false, default, false, 5006);
            _guiBuf.Write(cx, by + 7,  "White:",      ColorSpectrum.WHITE,      true,  default, false, 5007);
            _guiBuf.Write(cx, by + 8,  "Snow Peak",   ColorSpectrum.WHITE,      false, default, false, 5008);
            _guiBuf.Write(cx, by + 9,  "Blue:",       ColorSpectrum.BLUE,       true,  default, false, 5009);
            _guiBuf.Write(cx, by + 10, "Water",       ColorSpectrum.WHITE,      false, default, false, 5010);
            _guiBuf.Write(cx, by + 11, "Yellow:",     ColorSpectrum.YELLOW,     true,  default, false, 5011);
            _guiBuf.Write(cx, by + 12, "Beach",       ColorSpectrum.WHITE,      false, default, false, 5012);
        }
    }
    private void DisplayThanksMessage(int thanksWidth, int thanksHeight)
    {
        string thanks = "Thanks";
        int bx = Console.WindowWidth - thanksWidth;
        string line = new('-', thanksWidth - 3);
        string halfLine = new('-', thanksWidth / 2 - 2 - thanks.Length / 2 - 1);
        _guiBuf.DrawBox(bx, 0, thanksWidth, thanksHeight, "Other", ColorSpectrum.GREEN, HashCode.Combine(bx, thanksWidth));
        _guiBuf.Write(bx + 2, 1, "Thanks for playing!", ColorSpectrum.PURPLE, true, default, false, 6001);
        _guiBuf.Write(bx + 2, 2, line, ColorSpectrum.WHITE, false, default, false, 6002);
        _guiBuf.Write(bx + 2, 3, "This project was created as", ColorSpectrum.CYAN, true, default, false, 6003);
        _guiBuf.Write(bx + 2, 4, "a starting project for learning C#. ", ColorSpectrum.CYAN, true, default, false, 6004);
        _guiBuf.Write(bx + 2, 5, line, ColorSpectrum.WHITE, false, default, false, 6005);
        _guiBuf.WriteLine(bx + 2, thanksHeight - 4, halfLine, ColorSpectrum.WHITE, $" {thanks} ", ColorSpectrum.GREEN, thanksWidth - 3, 6006);
    }
    private void DisplayTitleAndSignature(int titleWidth, int titleHeight, string title)
    {
        int x = Console.WindowWidth / 2 - titleWidth / 2;
        _guiBuf.DrawBox(x, 0, titleWidth, titleHeight + 2, " ", ColorSpectrum.GREEN, HashCode.Combine(x, titleWidth));

        string leftSide = "~~//", rightSide = "//~~";
        string name = leftSide + title + rightSide;
        int maxNameLength = titleWidth - 2;
        if (name.Length > maxNameLength)
        {
            int maxTitleLength = maxNameLength - leftSide.Length - rightSide.Length;
            title = title[..Math.Max(maxTitleLength, 0)];
            name = leftSide + title + rightSide;
        }
        int nameStartX = x + (titleWidth - name.Length) / 2;
        _guiBuf.Write(nameStartX, 1, name, ColorSpectrum.CYAN, true, default, false, name.GetHashCode());

        int centerX = Console.WindowWidth / 2;
        int centerY = topPadding / 2;
        string sig1 = "** Made by: @cybutr **";
        string sig2 = "* On GitHub *";
        _guiBuf.Write(centerX - sig1.Length / 2, centerY, sig1, ColorSpectrum.MAGENTA, true, default, false, sig1.GetHashCode());
        _guiBuf.Write(centerX - sig2.Length / 2, centerY + 1, sig2, ColorSpectrum.CYAN, true, default, false, sig2.GetHashCode());
    }
    public void DisplayOutputLog(int outputWidth, int outputHeight, int titleWidth)
    {
        int startX = Console.WindowWidth / 2 + titleWidth / 2 - 1;
        int startY = 2;
        if (startX < 0) startX = 0;

        string text = eventBuffer.LastOrDefault() ?? "";
        int xPosition = startX + (outputWidth - text.Length) / 2;
        int maxLines = outputHeight - 5;
        int cursorX = startX + 2;
        int cursorY = startY + 1;

        _guiBuf.DrawBox(startX, 0, outputWidth, 3, "Current Event", ColorSpectrum.GREEN, HashCode.Combine(startX, outputWidth, 7001));
        _guiBuf.Clear(startX + 1, 1, outputWidth - 2, 1, HashCode.Combine(text, startX, 7002));
        _guiBuf.Write(xPosition, 1, text, ColorSpectrum.CYAN, true, default, false, HashCode.Combine(text, xPosition, 7003));
        _guiBuf.DrawBox(startX, startY, outputWidth, outputHeight - 3, " ", ColorSpectrum.GREEN, HashCode.Combine(startX, outputWidth, 7004));

        int logVersion = HashCode.Combine(actualOutputBuffer.Count, actualOutputBuffer.LastOrDefault()?.GetHashCode() ?? 0);
        _guiBuf.Clear(startX + 1, startY + 1, outputWidth - 2, maxLines, logVersion);

        Dictionary<string, (int r, int g, int b)> keywordColors = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Added",       ColorSpectrum.GREEN       },
            { "Removed",     ColorSpectrum.RED         },
            { "Updated",     ColorSpectrum.YELLOW      },
            { "Regenerated", ColorSpectrum.CYAN        },
            { "Chamber",     ColorSpectrum.DARKER_BLUE },
            { "Error",       ColorSpectrum.RED         },
            { "Warning",     ColorSpectrum.YELLOW      },
            { "Info",        ColorSpectrum.CYAN        },
            { "Debug",       ColorSpectrum.DARK_GREY   },
            { "Crab",        ColorSpectrum.RED         },
            { "Turtle",      ColorSpectrum.GREEN       },
            { "Sheep",       ColorSpectrum.SILVER      },
            { "Cow",         ColorSpectrum.SILVER      },
            { "Pig",         ColorSpectrum.PINK        },
            { "Chicken",     ColorSpectrum.YELLOW      },
            { "Fox",         ColorSpectrum.ORANGE      },
            { "Rabbit",      ColorSpectrum.GREY        },
            { "Wolf",        ColorSpectrum.GREY        },
            { "Bear",        ColorSpectrum.BROWN       },
            { "Goat",        ColorSpectrum.DARK_GREY   },
            { "Fish",        ColorSpectrum.BLUE_VIOLET },
            { "Bird",        ColorSpectrum.DARK_GREY   },
            { "Weather",     ColorSpectrum.CYAN        },
            { "Time",        ColorSpectrum.LIGHT_BLUE  },
            { "Season",      ColorSpectrum.LIGHT_GREEN },
            { "Temperature", ColorSpectrum.GREEN       },
            { "Humidity",    ColorSpectrum.BLUE        },
            { "Pressure",    ColorSpectrum.MAGENTA     },
            { "Wind",        ColorSpectrum.ORANGE      },
            { "Sunrise",     ColorSpectrum.ORANGE      },
            { "Sunset",      ColorSpectrum.ORANGE      },
            { "Day",         ColorSpectrum.PINK        },
            { "Cloud",       ColorSpectrum.SILVER      },
            { "Plains",      ColorSpectrum.GREEN       },
            { "Forest",      ColorSpectrum.DARK_GREEN  },
            { "Mountain",    ColorSpectrum.GREY        },
            { "Snow",        ColorSpectrum.WHITE       },
            { "Water",       ColorSpectrum.BLUE        },
            { "Beach",       ColorSpectrum.YELLOW      },
        };

        List<string> logLines = [.. actualOutputBuffer.Skip(Math.Max(0, actualOutputBuffer.Count - maxLines))
                                    .Take(maxLines)
                                    .Reverse()];

        for (int i = 0; i < logLines.Count; i++)
        {
            string[] words = logLines[i].Split(' ');
            int wx = cursorX;
            int wy = cursorY + i;
            foreach (string word in words)
            {
                string displayWord = word + " ";
                if (wx + displayWord.Length > startX + outputWidth - 1) break;
                string trimmedWord = word.Trim(',', '.', '!', '?');
                bool hasColor = keywordColors.TryGetValue(trimmedWord, out var wc);
                _guiBuf.Write(wx, wy, displayWord, hasColor ? wc : default, hasColor, default, false, HashCode.Combine(displayWord, wx, wy));
                wx += displayWord.Length;
            }
        }
    }
    #endregion
}
