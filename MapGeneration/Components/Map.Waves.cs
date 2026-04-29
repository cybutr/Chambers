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
    public List<Wave> waves = [];
    public const double WAVE_SPEED = 0.2;
    private List<(int x, int y)> _shorelineCache = [];
    private void InitializeWaves()
    {
        _shorelineCache.Clear();
        for (int xx = 0; xx < width; xx++)
            for (int yy = 0; yy < height; yy++)
                if (IsShorelineWater(xx, yy) && !IsNearLand(xx, yy, 3)) _shorelineCache.Add((xx, yy));

        waves.Clear();
        for (int i = 0; i < numberOfWaves; i++)
            AddNewWave();
    }
    public void AnimateWater()
    {
        Array.Clear(waveIntensityData, 0, waveIntensityData.Length);

        List<Wave> wavesToRemove = [];
        foreach (Wave wave in waves)
        {
            if (wave.Intensity < 1.0) wave.Intensity = Math.Min(wave.Intensity + 0.1, 1.0);
            wave.Direction += (rng.NextDouble() - 0.5) * wave.Curvature;

            bool removeWave = false;
            List<(double x, double y)> newPoints = [];

            foreach ((double x, double y) in wave.Points)
            {
                double newX = x + Math.Cos(wave.Direction) * wave.Speed;
                double newY = y + Math.Sin(wave.Direction) * wave.Speed;
                int checkX  = (int)Math.Round(newX);
                int checkY  = (int)Math.Round(newY);

                if (checkX < 0 || checkX >= width || checkY < 0 || checkY >= height || !IsWaterTile(checkX, checkY))
                {
                    removeWave = true;
                    break;
                }

                newPoints.Add((newX, newY));
                if (checkX > 0 && checkX < width - 1 && checkY > 0 && checkY < height - 1)
                    waveIntensityData[checkX, checkY] = wave.Intensity;
            }

            if (removeWave) wavesToRemove.Add(wave);
            else wave.Points = newPoints;
        }

        foreach (var w in wavesToRemove)
        {
            waves.Remove(w);
            AddNewWave();
        }
    }
    private bool IsTileUnderCloud(int x, int y)
    {
        if (!isCloudsRendering) return false;

        int cloudX = x + cloudDataOffsetX;
        int cloudY = y + cloudDataOffsetY;

        if (cloudX < 0 || cloudX >= cloudDataWidth || cloudY < 0 || cloudY >= cloudDataHeight) return false;
        if (cloudData == null || cloudData.Length == 0) return false;

        return cloudData[cloudX, cloudY] != CloudType.None;
    }
    private void AddNewWave()
    {
        if (_shorelineCache.Count == 0)
        {
            for (int xx = 0; xx < width; xx++)
                for (int yy = 0; yy < height; yy++)
                    if (IsShorelineWater(xx, yy) && !IsNearLand(xx, yy, 3)) _shorelineCache.Add((xx, yy));
            if (_shorelineCache.Count == 0) return;
        }
        (int x, int y) = _shorelineCache[rng.Next(_shorelineCache.Count)];

        double direction = GetWaveDirectionTowardsLand(x, y);

        Wave wave = new() {
            Direction = direction,
            Speed = WAVE_SPEED * (0.8 + rng.NextDouble() * 0.4),
            Length = rng.Next(5, 10),
            Curvature = rng.NextDouble() * 0.2 - 0.1
        };

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
                if (nx >= 0 && nx < width && ny >= 0 && ny < height && IsLandTile(nx, ny)) return true;
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

        if (nearestLandX == -1) return rng.NextDouble() * 2 * Math.PI;

        double angleToLand = Math.Atan2(nearestLandY - y, nearestLandX - x);
        return angleToLand;
    }
    private bool IsOpenWater(int x, int y)
        => TileRegistry.Get(mapData[x, y]).DeepVariant != null;
    private bool IsShorelineWater(int x, int y)
    {
        var def = TileRegistry.Get(mapData[x, y]);
        return def.IsWater && def.DeepVariant == null;
    }
    private bool IsWaterTile(int x, int y)
        => TileRegistry.Get(mapData[x, y]).IsWater;
    private (int r, int g, int b) GetWaveColor(int x, int y, double intensity = 1.0)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return (0, 0, 0);
        TileId tile = mapData[x, y];
        (int r, int g, int b) baseColor = GetColor(tile, x, y);
        (int r, int g, int b) finalColor;

        if (IsShorelineWater(x, y))
        {
            (int r, int g, int b) = GetColor(TileId.Ocean, x, y);
            finalColor = (
                (int)(baseColor.r + (r - baseColor.r) * intensity),
                (int)(baseColor.g + (g - baseColor.g) * intensity),
                (int)(baseColor.b + (b - baseColor.b) * intensity)
            );
        }
        else finalColor = baseColor;
        return finalColor;
    }
    #endregion
    #endregion
}
