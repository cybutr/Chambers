using System;
using System.Collections.Generic;
using System.Linq;
using Internal;
using static Internal.GUI;

public partial class Map
{
    public void DisplayMap(bool displayGUI = true)
    {
        // Adjust padding based on GUI state
        int effectiveLeftPadding = displayGUI ? leftPadding : 0;
        int effectiveTopPadding = displayGUI ? topPadding : 2;

        // Don't clear console, just move cursor to start position
        GUI.SetCursorPosition(effectiveLeftPadding, effectiveTopPadding);

        for (int y = 0; y < height; y++)
        {
            // Set cursor position at start of each line
            GUI.SetCursorPosition(effectiveLeftPadding, y + effectiveTopPadding);
            
            for (int x = 0; x < width; x++)
            {
                (int r, int g, int b) color = GetColor(mapData[x, y], x, y);
                GUI.Write(GUI.SetBackgroundColor(color.r, color.g, color.b) + "  " + GUI.ResetColor());
            }
        }
        DisplayDarkenedTiles(displayGUI);
        // Continue with overlay tile rendering...
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
                {
                    continue;
                }

                if (overlayData[x, y] != EntityId.None)
                {
                    GUI.SetCursorPosition(effectiveLeftPadding + x * 2, y + effectiveTopPadding);
                    (int r, int g, int b) bgColor = GetDarkenedColor(x, y);
                    (int r, int g, int b) fgColor = GetOverlayColor(overlayData[x, y]);
                    string bg = GUI.SetBackgroundColor(bgColor.r, bgColor.g, bgColor.b);
                    string fg = GUI.SetForegroundColor(fgColor.r, fgColor.g, fgColor.b);
                    GUI.Write(bg + fg + GetSpeciesIcon(overlayData[x, y]) + GUI.ResetColor());
                }
            }
        }
        GUI.ResetColor();
        //Update();
        
        // Only run animations and updates when simulation is active
        if (Program.isUpdating)
        {
            AnimateWater(displayGUI);
            DisplayCloudShadows(displayGUI);
            if (isCloudsRendering) RenderClouds(displayGUI);
        }
        else
        {
            // When paused, draw current state without updating
            DrawCurrentDarkness(displayGUI);
            DrawCurrentWaves(displayGUI);
            DrawCurrentCloudShadows(displayGUI);
            DrawCurrentClouds(displayGUI);
        }

        if (displayGUI) DisplayGUI();
    }
    #region display functions
    private (int r, int g, int b) GetOceanColor(double avgTemp, double avgHumidity)
    {
        if (avgTemp > 0.7)
        {
            // Warm climate ocean
            return (64, 164, 223); // Caribbean Blue
        }
        else if (avgTemp < 0.3)
        {
            // Cold climate ocean
            return (25, 25, 112); // Midnight Blue
        }
        else
        {
            // Temperate climate ocean
            return (70, 130, 180); // Steel Blue
        }
    }
    public bool HasMapChanged()
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (mapData[x, y] != previousMapData[x, y])
                {
                    return true;
                }
            }
        }
        return false;
    }
    public bool HasTileChanged(int x, int y)
    {
        return mapData[x, y] != previousMapData[x, y];
    }
    public bool HasOverlayTileChanged(int x, int y)
    {
        return overlayData[x, y] != previousOverlayData[x, y];
    }
    public void UpdatePreviousMapData()
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                previousMapData[x, y] = mapData[x, y];
                previousOverlayData[x, y] = overlayData[x, y];
            }
        }
    }
    public void UpdatePreviousOverlayData()
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                previousOverlayData[x, y] = overlayData[x, y];
            }
        }
    }
    public void UpdateTile(int x, int y, bool displayGUI = true)
    {
        int effectiveLeftPadding = displayGUI ? leftPadding : 0;
        int effectiveTopPadding = displayGUI ? topPadding : 2;
        GUI.SetCursorPosition(effectiveLeftPadding + x * 2, y + effectiveTopPadding);
        (int r, int g, int b) bgColor = GetColor(mapData[x, y], x, y); // Retrieve background color from ColorSpectrum
        string background = GUI.SetBackgroundColor(bgColor.r, bgColor.g, bgColor.b);
        GUI.Write(background + "  " + GUI.ResetColor());
    }
    public void UpdateOverlayTile(int x, int y, bool displayGUI = true)
    {
        int effectiveLeftPadding = displayGUI ? leftPadding : 0;
        int effectiveTopPadding = displayGUI ? topPadding : 2;
        // Prevent overlay data from being displayed on the edges
        if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
        {
            return;
        }
        UpdateTile(x, y, displayGUI);

        bool isNight = false;
        bool isDarkening = false;
        if (GetDarkenedTileIntensity(x, y) > 10)
        {
            isDarkening = true;
        }
        if (GetDarkenedTileIntensity(x, y) > 45)
        {
            isNight = true;
            isDarkening = false;
        }
        else if (GetDarkenedTileIntensity(x, y) < 5)
        {
            isNight = false;
        }
        (int r, int g, int b) bgColor = GetColor(mapData[x, y], x, y); // Background color based on current chamber's mapData
        (int r, int g, int b) fgColor = GetOverlayColor(overlayData[x, y]); // Foreground color based on current chamber's overlayData
        int darkenedIntensity = isDarkening ? (int)Math.Round(GetDarkenedTileIntensity(x, y)) : 0;
        // Apply shadow if the tile is under a cloud shadow
        if (currentShadowPositions.TryGetValue((x, y), out double shadowIntensity) && isCloudsShadowsRendering)
        {
            int shadowFactor = (int)(shadowIntensityFactor * shadowIntensity); // Adjust shadow intensity as needed
            bgColor = (
                Math.Max(0, bgColor.r - shadowFactor),
                Math.Max(0, bgColor.g - shadowFactor),
                Math.Max(0, bgColor.b - shadowFactor)
            );
        }
        if (isDarkening)
        {
            bgColor = (
                Math.Max(0, bgColor.r - darkenedIntensity),
                Math.Max(0, bgColor.g - darkenedIntensity),
                Math.Max(0, bgColor.b - darkenedIntensity)
            );
        }
        else if (isNight)
        {
            bgColor = (
                Math.Max(0, bgColor.r - 50),
                Math.Max(0, bgColor.g - 50),
                Math.Max(0, bgColor.b - 50)
            );
        }

        string background = GUI.SetBackgroundColor(bgColor.r, bgColor.g, bgColor.b);
        string foreground = GUI.SetForegroundColor(fgColor.r, fgColor.g, fgColor.b);

        if (!isCloudsRendering || (isCloudsRendering && !IsTileUnderCloud(x, y)))
        {
            // Write the background color first
            GUI.SetCursorPosition(effectiveLeftPadding + x * 2, y + effectiveTopPadding);
            GUI.Write(background + "  " + GUI.ResetColor());

            // Write the overlay character with the correct background and foreground colors
            GUI.SetCursorPosition(effectiveLeftPadding + x * 2, y + effectiveTopPadding);
            GUI.Write(background + "  " + GUI.ResetColor());
            GUI.SetCursorPosition(effectiveLeftPadding + x * 2, y + effectiveTopPadding);
            GUI.Write(background + foreground + GetSpeciesIcon(overlayData[x, y]) + GUI.ResetColor());
        }
    }
    public string GetSpeciesIcon(EntityId species)
    {
        return species switch
        {
            EntityId.Crab   => "󰃤 ",
            EntityId.Turtle => "󰳗 ",
            EntityId.Cow    => "󰆚 ",
            EntityId.Sheep  => "󰳆 ",
            _ => "  "
        };
    }
    private bool IsThereAnOverlayTile(int x, int y)
    {
        return overlayData[x, y] != EntityId.None;
    }
    public void DisplayCharacterOnTile(int x, int y, char character, string characterColor, bool displayGUI = true)
    {
        int effectiveLeftPadding = displayGUI ? leftPadding : 0;
        int effectiveTopPadding = displayGUI ? topPadding : 2;
        // Move cursor to position
        GUI.SetCursorPosition(effectiveLeftPadding + x * 2, y + effectiveTopPadding);

        // Get RGB color based on characterColor using ColorSpectrum
        (int r, int g, int b) rgb = GetRGBFromColorCode(characterColor);
        string fg = GUI.SetForegroundColor(rgb.r, rgb.g, rgb.b);
        GUI.Write(fg + character + GUI.ResetColor());
    }
    public (int r, int g, int b) GetRGBFromColorCode(string colorCode)
    {
        return colorCode switch
        {
            "red" => ColorSpectrum.RED,
            "green" => ColorSpectrum.GREEN,
            "blue" => ColorSpectrum.BLUE,
            "yellow" => ColorSpectrum.YELLOW,
            "cyan" => ColorSpectrum.CYAN,
            "magenta" => ColorSpectrum.MAGENTA,
            "white" => ColorSpectrum.WHITE,
            "black" => ColorSpectrum.BLACK,
            _ => ColorSpectrum.WHITE
        };
    }
    private (int r, int g, int b) GetColor(TileId tile, int x, int y)
    {
        var def = TileRegistry.Get(tile);
        var baseColor = def.BaseColor;

        if (tile == TileId.Border) return baseColor;

        if (def.IsWater)
        {
            double waterTemp = (avarageTempature - 0.6) * 100;
            double waterHum  = (avarageHumidity  - 0.6) * 8;
            return (
                Math.Clamp(baseColor.r - (int)waterHum,  0, 255),
                Math.Clamp(baseColor.g + (int)waterTemp, 0, 255),
                Math.Clamp(baseColor.b - (int)waterHum,  0, 255)
            );
        }

        if (tile == TileId.Mountain || tile == TileId.MountainDeep)
        {
            double mountainTemp = (avarageTempature - 0.5) * 60;
            double mountainHum  = (avarageHumidity  - 0.5) * 28;
            return (
                Math.Clamp(baseColor.r + (int)mountainTemp,    0, 255),
                Math.Clamp(baseColor.g + (int)mountainHum,     0, 255),
                Math.Clamp(baseColor.b - (int)mountainHum * 2, 0, 255)
            );
        }

        int rAdj = 0, gAdj = 0, bAdj = 0;
        if (conf.EnableTempatureBiomeChanges)
        {
            switch (temperatureData[x, y])
            {
                case 1: rAdj -= 20; bAdj -= 20; break; // Very Cold
                case 2: gAdj -=  5; bAdj -= 15; break; // Cold
                case 3: gAdj +=  5; bAdj += 10; break; // Cool
                case 4: rAdj +=  5; gAdj += 10; break; // Temperate
                case 5: rAdj += 10; gAdj += 15; break; // Warm
            }
        }
        if (conf.EnableHumidityBiomeChanges)
        {
            switch (humidityData[x, y])
            {
                case 1: gAdj -= 15; bAdj -=  5; break; // Very Dry
                case 2: gAdj -= 10;              break; // Dry
                case 3:                           break; // Moderate
                case 4: gAdj += 15; bAdj += 10; break; // Humid
                case 5: gAdj += 20; bAdj +=  5; break; // Very Humid
            }
        }
        return (
            Math.Clamp(baseColor.r + rAdj, 0, 255),
            Math.Clamp(baseColor.g + gAdj, 0, 255),
            Math.Clamp(baseColor.b + bAdj, 0, 255)
        );
    }
    public (int r, int g, int b) GetOverlayColor(EntityId overlayTile)
    {
        return overlayTile switch
        {
            EntityId.Crab     => ColorSpectrum.BRIGHT_RED,
            EntityId.Turtle   => ColorSpectrum.DARK_GREEN,
            EntityId.Cow      => ColorSpectrum.BLACK,
            EntityId.Sheep    => ColorSpectrum.BROWN,
            EntityId.Wolf     => ColorSpectrum.GREY,
            EntityId.Bear     => ColorSpectrum.BROWN,
            EntityId.Goat     => ColorSpectrum.BROWN,
            EntityId.Fish     => ColorSpectrum.BRIGHT_RED,
            EntityId.Bird     => ColorSpectrum.BRIGHT_RED,
            EntityId.Villager => ColorSpectrum.BROWN,
            _                 => ColorSpectrum.BLACK,
        };
    }

    #endregion
    #region temperature and humidity noise
    public void RenderTemperatureNoise()
    {
        GUI.DrawGrid(width, height, leftPadding, topPadding, (x, y) => {
            int tempValue = temperatureData[x, y];
            return TemperatureZoneToColor(tempValue);
        });
    }
    public void RenderHumidityNoise()
    {
        GUI.DrawGrid(width, height, leftPadding, topPadding, (x, y) => {
            int humidityValue = humidityData[x, y];
            return HumidityZoneToColor(humidityValue);
        });
    }
    private (int r, int g, int b) TemperatureToColor(double value)
    {
        int r = (int)(value * 255);
        int g = 0;
        int b = (int)((1 - value) * 255);
        return (r, g, b);
    }
    private (int r, int g, int b) HumidityToColor(double value)
    {
        int r = 0;
        int g = (int)(value * 255);
        int b = (int)((1 - value) * 255);
        return (r, g, b);
    }
    private (int r, int g, int b) TemperatureZoneToColor(int zone)
    {
        return zone switch
        {
            1 => ColorSpectrum.LIGHT_BLUE,
            2 => ColorSpectrum.BLUE,
            3 => ColorSpectrum.BLUE_VIOLET,
            4 => ColorSpectrum.PURPLE,
            5 => ColorSpectrum.RED,
            _ => ColorSpectrum.WHITE
        };
    }
    private (int r, int g, int b) HumidityZoneToColor(int zone)
    {
        return zone switch
        {
            1 => ColorSpectrum.ANTIQUE_WHITE,
            2 => ColorSpectrum.LIGHT_YELLOW,
            3 => ColorSpectrum.YELLOW,
            4 => ColorSpectrum.GREEN,
            5 => ColorSpectrum.DARK_GREEN,
            _ => ColorSpectrum.WHITE
        };
    }
    #endregion
}
