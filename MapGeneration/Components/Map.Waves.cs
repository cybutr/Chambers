using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Internal;
using static Internal.GUI;

public partial class Map
{
    #region water system
    #region waves
    public List<Wave> waves = new List<Wave>();
    public const double WAVE_SPEED = 0.2;
    private void InitializeWaves()
    {
        waves.Clear();
        for (int i = 0; i < numberOfWaves; i++)
        {
            AddNewWave();
        }
    }
    public static object consoleLock {get; set;} = new object();
    public HashSet<(int x, int y)> wavePositions {get; set;} = new HashSet<(int x, int y)>();
    public void AnimateWater(bool displayGUI = true)
    {
        // Clear the wavePositions at the start
        wavePositions.Clear();

        var wavesToRemove = new List<Wave>();
        foreach (Wave wave in waves)
        {
            // Adjust wave intensity based on time of day
            if (!wave.IsNight && wave.Intensity < 1.0)
                wave.Intensity = Math.Min(wave.Intensity + 0.1, 1.0);
            else if (wave.IsNight && wave.Intensity < 1.0)
                wave.Intensity = Math.Max(wave.Intensity + 0.1, 0.0);
            // Snapshot old positions then reuse the set
            var oldPoints = new HashSet<(int x, int y)>(wave.PreviousPoints);
            wave.PreviousPoints.Clear();

            // Introduce curvature by modifying the direction slightly
            wave.Direction += (rng.NextDouble() - 0.5) * wave.Curvature;

            bool removeWave = false;
            List<(double x, double y)> newPoints = new List<(double x, double y)>();

            foreach ((double x, double y) in wave.Points)
            {
                double newX = x + Math.Cos(wave.Direction) * wave.Speed;
                double newY = y + Math.Sin(wave.Direction) * wave.Speed;

                int checkX = (int)Math.Round(newX);
                int checkY = (int)Math.Round(newY);

                if (checkX < 0 || checkX >= width || checkY < 0 || checkY >= height || !IsWaterTile(checkX, checkY))
                {
                    removeWave = true;
                    break;
                }

                newPoints.Add((newX, newY));
                wave.PreviousPoints.Add((checkX, checkY));
                if (GetDarkenedTileIntensity(checkX, checkY) > 10)
                {
                    wave.IsDarkening = true;
                }
                if (GetDarkenedTileIntensity(checkX, checkY) > 45)
                {
                    wave.IsNight = true;
                    wave.IsDarkening = false;
                }
                else if (GetDarkenedTileIntensity(checkX, checkY) < 5)
                {
                    wave.IsNight = false;
                }
            }

            // Clear old wave positions
            foreach ((int x, int y) point in oldPoints)
            {
                if (!wave.PreviousPoints.Contains(point))
                {
                    if ((isCloudsRendering && !IsTileUnderCloud(point.x, point.y)) || !isCloudsRendering) UpdateWaterTile(point.x, point.y, false, wave.IsNight, wave.IsDarkening, 1.0, displayGUI);
                    // Remove the point from wavePositions
                    wavePositions.Remove(point);
                }
            }

            // Draw new wave positions
            foreach ((int x, int y) in wave.PreviousPoints)
            {
                // Check if point is under cloud
                bool isUnderCloud = IsTileUnderCloud(x, y);
                if (!isCloudsRendering || (isCloudsRendering && !isUnderCloud))
                {
                    UpdateWaterTile(x, y, true, wave.IsNight, wave.IsDarkening, wave.Intensity, displayGUI);
                }
                // Add the point to wavePositions
                wavePositions.Add((x, y));
            }

            if (removeWave)
            {
                // Clear final positions before removing
                foreach ((int x, int y) in wave.PreviousPoints)
                {
                    UpdateWaterTile(x, y, false, wave.IsNight, wave.IsDarkening, 1.0, displayGUI);
                    wavePositions.Remove((x, y));
                }
                wavesToRemove.Add(wave);
            }
            else
            {
                wave.Points = newPoints;
            }
        }
        foreach (var w in wavesToRemove)
        {
            waves.Remove(w);
            AddNewWave();
        }
    }
    private void UpdateWaterTile(int x, int y, bool isWave, bool isNight, bool isDarkening, double intensity = 1.0, bool displayGUI = true)
    {
        int effectiveLeftPadding = displayGUI ? leftPadding : 0;
        int effectiveTopPadding = displayGUI ? topPadding : 2;
        if (x < 0 || x >= width || y < 0 || y >= height) return;
        TileId tile = mapData[x, y];

        (int r, int g, int b) baseColor;

        if (isNight)
        {
            baseColor = GetDarkenedTileColor(tile, x, y);
        }
        else
        {
            baseColor = IsThereACloudShadow(x, y) && isCloudsShadowsRendering
                ? GetShadowColor(x, y)
                : GetColor(tile, x, y);
        }

        (int r, int g, int b) finalColor;

        if (isWave && intensity > 0.0)
        {
            // Apply wave color intensity effect
            (int r, int g, int b) waveColor = GetColor(TileId.Ocean, x, y);
            (int r, int g, int b) darkenedWaveColor = GetDarkenedColor(x, y);
            int darkenedIntensity = isDarkening ? (int)Math.Round(GetDarkenedTileIntensity(x, y)) : 0;
            switch (isNight)
            {
                case true:
                    finalColor = (
                        Math.Clamp((int)(baseColor.r + (darkenedWaveColor.r - baseColor.r) * intensity), 0, 255),
                        Math.Clamp((int)(baseColor.g + (darkenedWaveColor.g - baseColor.g) * intensity), 0, 255),
                        Math.Clamp((int)(baseColor.b + (darkenedWaveColor.b - baseColor.b) * intensity), 0, 255)
                    );
                    break;
                case false:
                    finalColor = (
                        Math.Clamp((int)(baseColor.r + (waveColor.r - baseColor.r) * intensity - darkenedIntensity), 0, 255),
                        Math.Clamp((int)(baseColor.g + (waveColor.g - baseColor.g) * intensity - darkenedIntensity), 0, 255),
                        Math.Clamp((int)(baseColor.b + (waveColor.b - baseColor.b) * intensity - darkenedIntensity), 0, 255)
                    );
                    break;
            }
        }
        else if (darkenedPositionsIntensities.TryGetValue((x, y), out int darkBaseIntensity))
        {
            finalColor.r = GetTileBaseColor(x, y).r - darkBaseIntensity;
            finalColor.g = GetTileBaseColor(x, y).g - darkBaseIntensity;
            finalColor.b = GetTileBaseColor(x, y).b - darkBaseIntensity;
        }
        else if (IsTileDarkened(x, y))
        {
            finalColor.r = GetTileBaseColor(x, y).r - 50;
            finalColor.g = GetTileBaseColor(x, y).g - 50;
            finalColor.b = GetTileBaseColor(x, y).b - 50;
        }
        else
        {
            finalColor = GetColor(tile, x, y);
        }
        if (currentShadowPositions.TryGetValue((x, y), out double shadowIntensity) && isCloudsShadowsRendering)
        {
            int shadowFactor = (int)(shadowIntensityFactor * shadowIntensity); // Adjust shadow intensity as needed
            finalColor.r = GetTileBaseColor(x, y).r - shadowFactor;
            finalColor.g = GetTileBaseColor(x, y).g - shadowFactor;
            finalColor.b = GetTileBaseColor(x, y).b - shadowFactor;
        }


        lock (consoleLock)
        {
            GUI.SetCursorPosition(effectiveLeftPadding + x * 2, y + effectiveTopPadding);
            string background = GUI.SetBackgroundColor(finalColor.r, finalColor.g, finalColor.b);
            GUI.Write(background + "  " + GUI.ResetColor());
        }

        if (!isWave)
        {
            // Ensure the map data retains the original water tile
            if ((isCloudsRendering && !IsTileUnderCloud(x, y)) || !isCloudsRendering) mapData[x, y] = tile;
        }
    }
    private bool IsTileUnderCloud(int x, int y)
    {
        if (!isCloudsRendering)
        {
            return false;
        }

        int cloudX = x + cloudDataOffsetX;
        int cloudY = y + cloudDataOffsetY;

        // Check if indices are within bounds
        if (cloudX < 0 || cloudX >= cloudDataWidth || cloudY < 0 || cloudY >= cloudDataHeight)
        {
            return false;
        }

        // Ensure mapData and cloudData arrays are properly initialized
        if (cloudData == null || cloudData.Length == 0)
        {
            return false;
        }

        return cloudData[cloudX, cloudY] != CloudType.None;
    }
    private void AddNewWave()
    {
        List<(int x, int y)> validPositions = new List<(int x, int y)>();

        // First scan the map for all valid positions
        for (int xx = 0; xx < width; xx++)
        {
            for (int yy = 0; yy < height; yy++)
            {
                if (IsShorelineWater(xx, yy) && !IsNearLand(xx, yy, 3))
                {
                    validPositions.Add((xx, yy));
                }
            }
        }

        // If no valid positions found, return without creating a wave
        if (validPositions.Count == 0) return;

        // Pick a random valid position
        int index = rng.Next(validPositions.Count);
        (int x, int y) = validPositions[index];

        // Calculate wave direction towards nearest land
        double direction = GetWaveDirectionTowardsLand(x, y);

        Wave wave = new Wave
        {
            Direction = direction,
            Speed = WAVE_SPEED * (0.8 + rng.NextDouble() * 0.4),
            Length = rng.Next(5, 10),
            Curvature = rng.NextDouble() * 0.2 - 0.1
        };

        // Create wave points perpendicular to movement direction
        for (int i = -wave.Length / 2; i <= wave.Length / 2; i++)
        {
            double offsetX = Math.Cos(wave.Direction + Math.PI / 2) * i;
            double offsetY = Math.Sin(wave.Direction + Math.PI / 2) * i;
            double wx = x + offsetX;
            double wy = y + offsetY;
            wave.Points.Add((wx, wy));
        }

        waves.Add(wave);
    }
    private bool IsNearLand(int x, int y, int distance)
    {
        for (int dx = -distance; dx <= distance; dx++)
        {
            for (int dy = -distance; dy <= distance; dy++)
            {
                int nx = x + dx;
                int ny = y + dy;
                if (nx >= 0 && nx < width && ny >= 0 && ny < height && IsLandTile(nx, ny))
                {
                    return true;
                }
            }
        }
        return false;
    }
    private bool IsLandTile(int x, int y)
        => TileRegistry.Get(mapData[x, y]).IsLand;
    private double GetWaveDirectionTowardsLand(int x, int y)
    {
        int nearestLandX = -1;
        int nearestLandY = -1;
        double minDistance = double.MaxValue;

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (IsLandTile(i, j))
                {
                    double distance = (i - x) * (i - x) + (j - y) * (j - y);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearestLandX = i;
                        nearestLandY = j;
                    }
                }
            }
        }

        if (nearestLandX == -1)
        {
            // No land found, default to random direction
            return rng.NextDouble() * 2 * Math.PI;
        }

        // Calculate direction towards land
        double angleToLand = Math.Atan2(nearestLandY - y, nearestLandX - x);
        return angleToLand;
    }
    // Open water: tiles that have a shoreline variant (Ocean, Lake, River, Stream)
    private bool IsOpenWater(int x, int y)
        => TileRegistry.Get(mapData[x, y]).DeepVariant != null;
    // Shoreline water: water tiles without a further variant (OceanShallow, LakeShallow, RiverShallow)
    private bool IsShorelineWater(int x, int y)
    {
        var def = TileRegistry.Get(mapData[x, y]);
        return def.IsWater && def.DeepVariant == null;
    }
    private bool IsWaterTile(int x, int y)
        => TileRegistry.Get(mapData[x, y]).IsWater;
    private bool IsThereAWaveTile(int x, int y)
    {
        return waves.Any(w => w.PreviousPoints.Contains((x, y)));
    }
    private (int r, int g, int b) GetWaveColor(int x, int y, double intensity = 1.0)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return (0, 0, 0);
        TileId tile = mapData[x, y];
        (int r, int g, int b) baseColor = GetColor(tile, x, y);
        (int r, int g, int b) finalColor;

        if (IsShorelineWater(x, y))
        {
            (int r, int g, int b) waveColor = GetColor(TileId.Ocean, x, y);
            finalColor = (
                (int)(baseColor.r + (waveColor.r - baseColor.r) * intensity),
                (int)(baseColor.g + (waveColor.g - baseColor.g) * intensity),
                (int)(baseColor.b + (waveColor.b - baseColor.b) * intensity)
            );
        }
        else
        {
            finalColor = baseColor;
        }
        return finalColor;
    }
    #endregion
    #endregion
    #region draw rawing Methods
    /// <summary>
    /// Draw current wave positions without updating wave movement
    /// </summary>
    public void DrawCurrentWaves(bool displayGUI = true)
    {
        int effectiveLeftPadding = displayGUI ? leftPadding : 0;
        int effectiveTopPadding = displayGUI ? topPadding : 2;
        // Clear and rebuild wavePositions for consistency
        wavePositions.Clear();

        foreach (Wave wave in waves)
        {
            foreach ((int x, int y) in wave.PreviousPoints)
            {
                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    // Add to wavePositions for consistency with other methods
                    wavePositions.Add((x, y));
                    bool isUnderCloud = IsTileUnderCloud(x, y);
                    if (!isCloudsRendering || (isCloudsRendering && !isUnderCloud))
                    {
                        TileId tile = mapData[x, y];
                        (int r, int g, int b) baseColor;

                        if (wave.IsNight)
                        {
                            baseColor = GetDarkenedTileColor(tile, x, y);
                        }
                        else
                        {
                            if (IsThereACloudShadow(x, y) && isCloudsShadowsRendering)
                            {
                                baseColor = GetShadowColor(x, y);
                            }
                            else
                            {
                                baseColor = GetColor(tile, x, y);
                            }
                        }

                        (int r, int g, int b) waveColor = GetColor(TileId.Ocean, x, y);
                        (int r, int g, int b) darkenedWaveColor = GetDarkenedColor(x, y);
                        int darkenedIntensity = wave.IsDarkening ? (int)Math.Round(GetDarkenedTileIntensity(x, y)) : 0;

                        (int r, int g, int b) finalColor;
                        if (wave.IsNight)
                        {
                            finalColor = (
                                Math.Clamp((int)(baseColor.r + (darkenedWaveColor.r - baseColor.r) * wave.Intensity), 0, 255),
                                Math.Clamp((int)(baseColor.g + (darkenedWaveColor.g - baseColor.g) * wave.Intensity), 0, 255),
                                Math.Clamp((int)(baseColor.b + (darkenedWaveColor.b - baseColor.b) * wave.Intensity), 0, 255)
                            );
                        }
                        else
                        {
                            finalColor = (
                                Math.Clamp((int)(baseColor.r + (waveColor.r - baseColor.r) * wave.Intensity - darkenedIntensity), 0, 255),
                                Math.Clamp((int)(baseColor.g + (waveColor.g - baseColor.g) * wave.Intensity - darkenedIntensity), 0, 255),
                                Math.Clamp((int)(baseColor.b + (waveColor.b - baseColor.b) * wave.Intensity - darkenedIntensity), 0, 255)
                            );
                        }

                        // Apply cloud shadows if present
                        if (currentShadowPositions.TryGetValue((x, y), out double shadowIntensity) && isCloudsShadowsRendering)
                        {
                            int shadowFactor = (int)(shadowIntensityFactor * shadowIntensity);
                            finalColor.r = Math.Max(0, finalColor.r - shadowFactor);
                            finalColor.g = Math.Max(0, finalColor.g - shadowFactor);
                            finalColor.b = Math.Max(0, finalColor.b - shadowFactor);
                        }

                        lock (consoleLock)
                        {
                            GUI.SetCursorPosition(effectiveLeftPadding + x * 2, y + effectiveTopPadding);
                            string background = GUI.SetBackgroundColor(finalColor.r, finalColor.g, finalColor.b);
                            GUI.Write(background + "  " + GUI.ResetColor());
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Draw current cloud shadow positions without updating shadow calculations
    /// </summary>
    public void DrawCurrentCloudShadows(bool displayGUI = true)
    {
        int effectiveLeftPadding = displayGUI ? leftPadding : 0;
        int effectiveTopPadding = displayGUI ? topPadding : 2;
        if (conf.DisplayShadows && shadowIntensityFactor > 0)
        {
            foreach (KeyValuePair<(int x, int y), double> pair in currentShadowPositions)
            {
                (int x, int y) pos = pair.Key;
                double intensity = pair.Value;

                if (intensity > 0 && pos.x > 0 && pos.x < width - 1 && pos.y > 0 && pos.y < height - 1)
                {
                    bool isUnderCloud = IsTileUnderCloud(pos.x, pos.y);

                    if (((isCloudsRendering && !isUnderCloud) || !isCloudsRendering) && !IsThereAWaveTile(pos.x, pos.y))
                    {
                        (int r, int g, int b) baseColor = GetTileBaseColor(pos.x, pos.y);
                        int shadowFactor = (int)(shadowIntensityFactor * intensity);

                        (int r, int g, int b) shadowColor = (
                            Math.Max(0, baseColor.r - shadowFactor),
                            Math.Max(0, baseColor.g - shadowFactor),
                            Math.Max(0, baseColor.b - shadowFactor)
                        );

                        if (IsThereAnOverlayTile(pos.x, pos.y))
                        {
                            (int r, int g, int b) overlayColor = GetOverlayColor(overlayData[pos.x, pos.y]);
                            int or = Math.Max(0, overlayColor.r - shadowFactor);
                            int og = Math.Max(0, overlayColor.g - shadowFactor);
                            int ob = Math.Max(0, overlayColor.b - shadowFactor);

                            string background = GUI.SetBackgroundColor(shadowColor.r, shadowColor.g, shadowColor.b);
                            string foreground = GUI.SetForegroundColor(or, og, ob);
                            GUI.SetCursorPosition(effectiveLeftPadding + pos.x * 2, pos.y + effectiveTopPadding);
                            GUI.Write(background + foreground + GetSpeciesIcon(overlayData[pos.x, pos.y]) + GUI.ResetColor());
                        }
                        else
                        {
                            GUI.SetCursorPosition(effectiveLeftPadding + pos.x * 2, pos.y + effectiveTopPadding);
                            GUI.Write(GUI.SetBackgroundColor(shadowColor.r, shadowColor.g, shadowColor.b) + "  " + GUI.ResetColor());
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Draw current cloud positions without updating cloud movement
    /// </summary>
    public void DrawCurrentClouds(bool displayGUI = true)
    {
        int effectiveLeftPadding = displayGUI ? leftPadding : 0;
        int effectiveTopPadding = displayGUI ? topPadding : 2;
        if (isCloudsRendering)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    (int cloudX, int cloudY) = MapDataCordsToCloudData(x, y);
                    if (IsInCloudBounds(cloudX, cloudY) && cloudData[cloudX, cloudY] != CloudType.None)
                    {
                        (int r, int g, int b) cloudColor = GetCloudColor(cloudX, cloudY);
                        GUI.SetCursorPosition(effectiveLeftPadding + x * 2, y + effectiveTopPadding);
                        GUI.Write(GUI.SetBackgroundColor(cloudColor.r, cloudColor.g, cloudColor.b) + "  " + GUI.ResetColor());
                    }
                }
            }
        }
    }

    /// <summary>
    /// Draw current darkness/night effects without updating day/night cycle
    /// </summary>
    public void DrawCurrentDarkness(bool displayGUI = true)
    {
        int effectiveLeftPadding = displayGUI ? leftPadding : 0;
        int effectiveTopPadding = displayGUI ? topPadding : 2;
        // Draw darkened positions with their current intensities
        foreach (KeyValuePair<(int x, int y), int> kvp in darkenedPositionsIntensities)
        {
            (int x, int y) pos = kvp.Key;
            int intensity = kvp.Value;

            if (pos.x > 0 && pos.x < width - 1 && pos.y > 0 && pos.y < height - 1)
            {
                if ((isCloudsRendering && !IsTileUnderCloud(pos.x, pos.y)) || !isCloudsRendering)
                {
                    (int r, int g, int b) baseColor = GetTileBaseColor(pos.x, pos.y);
                    (int r, int g, int b) darkenedColor = (
                        Math.Max(0, baseColor.r - intensity),
                        Math.Max(0, baseColor.g - intensity),
                        Math.Max(0, baseColor.b - intensity)
                    );

                    if (IsThereAnOverlayTile(pos.x, pos.y))
                    {
                        (int r, int g, int b) overlayColor = GetOverlayColor(overlayData[pos.x, pos.y]);
                        (int r, int g, int b) darkenedOverlayColor = (
                            Math.Max(0, overlayColor.r - intensity),
                            Math.Max(0, overlayColor.g - intensity),
                            Math.Max(0, overlayColor.b - intensity)
                        );

                        GUI.SetCursorPosition(effectiveLeftPadding + pos.x * 2, pos.y + effectiveTopPadding);
                        string bg = GUI.SetBackgroundColor(darkenedColor.r, darkenedColor.g, darkenedColor.b);
                        string fg = GUI.SetForegroundColor(darkenedOverlayColor.r, darkenedOverlayColor.g, darkenedOverlayColor.b);
                        GUI.Write(bg + fg + GetSpeciesIcon(overlayData[pos.x, pos.y]) + GUI.ResetColor());
                    }
                    else
                    {
                        GUI.SetCursorPosition(effectiveLeftPadding + pos.x * 2, pos.y + effectiveTopPadding);
                        GUI.Write(GUI.SetBackgroundColor(darkenedColor.r, darkenedColor.g, darkenedColor.b) + "  " + GUI.ResetColor());
                    }
                }
            }
        }
    }
    #endregion
}