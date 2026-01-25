using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Internal;
using static Internal.GUI;

public partial class Map
{
    #region mountain functions
    private void CreateMountains()
    {
        if (rng.NextDouble() < 1)
        {
            GenerateMountainRanges();
            if (debug) GUI.WriteLine("Generated mountain ranges");
            MountainDepth();
            if (debug) GUI.WriteLine("Added mountain depth");
            //ErodeMountainRanges();
            if (debug) GUI.WriteLine("Eroded mountain ranges");
            GenerateSnowPeaks();
            if (debug) GUI.WriteLine("Generated snow peaks");
            //DeleteBadSnowPeaks();
            if (debug) GUI.WriteLine("Deleted bad snow peaks");
            ForestMountains();
            RemoveObscureMountains();
            if (debug) GUI.WriteLine("Added Mountain Forests");
        }
    }
    private void GenerateMountainRanges()
    {
        int maxMountains = 2;
        int maxAdditionalMountains = 4;
        
        List<(int x, int y)> validTiles = new List<(int x, int y)>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (mapData[x, y] == 'P' || mapData[x, y] == 'F')
                {
                    validTiles.Add((x, y));
                }
            }
        }
        
        if (validTiles.Count < maxMountains * 2)
        {
            if (debug) GUI.WriteLine("Insufficient valid tiles for mountain generation");
            return;
        }
        
        for (int m = 0; m < maxMountains; m++)
        {
            int startIndex = rng.Next(validTiles.Count);
            (int startX, int startY) = validTiles[startIndex];
            
            (int endX, int endY) = FindValidEndPoint(validTiles, startX, startY);
            
            CreateMountainRange(startX, startY, endX, endY);
            CreateAdditionalMountainRange(startX, startY, maxAdditionalMountains);
        }
    }
    private (int x, int y) FindValidEndPoint(List<(int x, int y)> validTiles, int startX, int startY)
    {
        const int maxAttempts = 20;
        
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            int endIndex = rng.Next(validTiles.Count);
            (int candidateX, int candidateY) = validTiles[endIndex];
            
            double distance = GetDistance(startX, startY, candidateX, candidateY);
            if (distance >= 10 && distance <= 80)
            {
                return (candidateX, candidateY);
            }
        }
        
        double angle = rng.NextDouble() * 2 * Math.PI;
        int distance2 = rng.Next(10, Math.Min(80, Math.Min(width, height) / 2));
        int endX = Math.Clamp(startX + (int)(distance2 * Math.Cos(angle)), 1, width - 2);
        int endY = Math.Clamp(startY + (int)(distance2 * Math.Sin(angle)), 1, height - 2);
        
        return (endX, endY);
    }
    private void CreateAdditionalMountainRange(int oldStartX, int oldStartY, int maxAttempts)
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            if (rng.NextDouble() >= 0.5) continue;
            
            (int newStartX, int newStartY) = GetRandomPointInBiome('M');
            if (newStartX == -1) break;
            
            if (GetDistance(oldStartX, oldStartY, newStartX, newStartY) < 20) continue;
            
            (int endX, int endY) = GetRandomPointInRange(newStartX, newStartY, 10, 80);
            CreateMountainRange(newStartX, newStartY, endX, endY);
            
            oldStartX = newStartX;
            oldStartY = newStartY;
        }
    }
    private void CreateMountainRange(int startX, int startY, int endX, int endY)
    {
        int x = startX;
        int y = startY;
        int maxSteps = Math.Min(width * height, 10000);
        
        for (int step = 0; step < maxSteps; step++)
        {
            int mountainWidth = rng.Next(conf.MinMountainWidth, conf.MaxMountainWidth + 1);
            int halfWidth = mountainWidth / 2;
            
            for (int i = -halfWidth; i <= halfWidth; i++)
            {
                int nx = x + i;
                int ny = y + i;
                
                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                {
                    if (Math.Abs(i) <= halfWidth && y >= 0 && y < height)
                        mapData[nx, y] = 'M';
                    if (Math.Abs(i) <= halfWidth && x >= 0 && x < width)
                        mapData[x, ny] = 'M';
                }
            }
            
            if (x == endX && y == endY) break;
            
            int dx = endX - x;
            int dy = endY - y;
            int direction = rng.Next(100);
            
            if (direction < 60)
            {
                if (Math.Abs(dx) > Math.Abs(dy))
                    x += dx > 0 ? 1 : -1;
                else
                    y += dy > 0 ? 1 : -1;
            }
            else
            {
                x += rng.Next(3) - 1;
                y += rng.Next(3) - 1;
            }
            
            x = Math.Clamp(x, 0, width - 1);
            y = Math.Clamp(y, 0, height - 1);
        }
    }
    private void MountainDepth()
    {
        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                if ((mapData[x, y] == 'M' || mapData[x, y] == 'm') &&
                    CountSurroundingBiomes(x, y, 'M') + CountSurroundingBiomes(x, y, 'm') == 8)
                {
                    mapData[x, y] = 'm';
                }
            }
        }

        int maxAttempts = 500;
        int successfulAttempts = 0;

        for (int attempt = 0; attempt < maxAttempts && successfulAttempts < 42; attempt++)
        {
            if (rng.NextDouble() >= 0.6) continue;

            (int px, int py) = GetRandomPointInBiome('m');
            if (px == -1) break;

            mapData[px, py] = 'M';
            SpreadTile(px, py, 0.5, 1, 5);
            successfulAttempts++;
        }
    }
    private void ErodeMountainRanges()
    {
        const int maxIterations = 2;
        const int maxNoChange = 100;
        const double erosionThreshold = 0.01;
        const double windFactor = 0.1;
        const double waterFactor = 0.2;
        const double tempFactor = 0.05;
        
        int noChangeCounter = 0;
        
        for (int iteration = 0; iteration < maxIterations; iteration++)
        {
            bool changed = false;
            
            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    char tile = mapData[x, y];
                    if (tile != 'M' && tile != 'm' && tile != 'S') continue;
                    
                    double totalErosion = 0;
                    
                    foreach ((int nx, int ny) in GetNeighbors(x, y))
                    {
                        char neighbor = mapData[nx, ny];
                        if (neighbor == 'P' || neighbor == 'F')
                            totalErosion += windFactor;
                        if (neighbor == 'O' || neighbor == 'L' || neighbor == 'R')
                            totalErosion += waterFactor;
                    }
                    
                    totalErosion += rng.NextDouble() * tempFactor;
                    
                    if (totalErosion > erosionThreshold)
                    {
                        if (totalErosion > 0.5)
                            mapData[x, y] = GetMostSurroundedBiome(x, y);
                        else if (totalErosion > 0.2)
                            mapData[x, y] = 'm';
                        changed = true;
                    }
                }
            }
            
            if (!changed)
            {
                noChangeCounter++;
                if (noChangeCounter >= maxNoChange) break;
            }
            else
            {
                noChangeCounter = 0;
            }
        }
        
        SmoothMountainEdges();
    }
    private void SmoothMountainEdges()
    {
        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                if (mapData[x, y] == 'M' && CountSurroundingBiomes(x, y, 'M') < 5)
                {
                    mapData[x, y] = 'P';
                }
            }
        }
    }
    private void ForestMountains()
    {
        List<(int, int)> startPositions = new List<(int, int)>();
        List<(int, int)> forestPositions = new List<(int, int)>();
        int numberOfForests = rng.Next(2, 5);
        for (int i = 0; i < numberOfForests; i++)
        {
            (int x, int y) = GetStartingForestPosition();
            if (x != -1 && y != -1) startPositions.Add((x, y));
        }
        foreach ((int startX, int startY) in startPositions)
        {
            var newForestTiles = GetSpreadTiles(startX, startY, 0.6, 20, 100);
            foreach ((int fx, int fy) in newForestTiles)
            {
                if (mapData[fx, fy] == 'M')
                {
                    mapData[fx, fy] = 'F';
                    forestPositions.Add((fx, fy));
                }
            }
        }
        forestPositions.AddRange(SpreadMountainForests(forestPositions));
        var obscuredForests = new List<(int, int)>();
        for(int x = 0; x <= 3; x++) obscuredForests.AddRange(FillObscureForests(forestPositions));
        foreach ((int x, int y) in obscuredForests) forestPositions.Remove((x, y));
        forestPositions.AddRange(SmoothMountainForests(forestPositions));
        var carvedForests = CarveOutMountainForests(forestPositions);
        foreach ((int x, int y) in carvedForests) forestPositions.Remove((x, y));
        RemoveObscurePlainsNearMountains();
        forestPositions.AddRange(FillEncosedMountainPlains());
        forestPositions.AddRange(MakeMountainForestOpenings(forestPositions));
        var harshForests = new List<(int, int)>();
        for(int x = 0; x <= 2; x++) harshForests.AddRange(RemoveHarshForests(0.65));
        foreach ((int x, int y) in harshForests) forestPositions.Remove((x, y));

    }
    private (int, int) GetStartingForestPosition()
    {
        const int maxAttempts = 100;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var (x, y) = GetRandomPointInBiome('M');
            if (x == -1) return (-1, -1);
            
            if (CountSurroundingBiomes(x, y, 'P') + CountSurroundingBiomes(x, y, 'F') >= 5)
            {
                return (x, y);
            }
        }
        return (-1, -1);
    }
    private List<(int, int)> SpreadMountainForests(List<(int, int)> forestPositions)
    {
        if (forestPositions.Count == 0) return new List<(int, int)>();
        
        HashSet<(int, int)> newForests = new HashSet<(int, int)>();
        HashSet<(int, int)> processed = new HashSet<(int, int)>(forestPositions);
        
        foreach ((int x, int y) in forestPositions)
        {
            if (CountSurroundingBiomes(x, y, 'F') <= 3) continue;
            
            var spreadTiles = GetSpreadTiles(x, y, 0.95, 15, 30);
            foreach ((int sx, int sy) in spreadTiles)
            {
                if (processed.Contains((sx, sy))) continue;
                if (mapData[sx, sy] != 'P') continue;
                if (GetClosestDistanceOfType(sx, sy, 'M') > 2) continue;
                
                mapData[sx, sy] = 'F';
                newForests.Add((sx, sy));
                processed.Add((sx, sy));
            }
        }
        
        return newForests.ToList();
    }
    private List<(int, int)> FillObscureForests(List<(int, int)> forestPositions)
    {
        if (forestPositions.Count == 0) return new List<(int, int)>();
        
        HashSet<(int, int)> removedForests = new HashSet<(int, int)>();
        
        foreach ((int x, int y) in forestPositions)
        {
            int mountainCount = CountSurroundingBiomes(x, y, 'm') + CountSurroundingBiomes(x, y, 'M');
            if (mountainCount >= 1 && CountSurroundingBiomes(x, y, 'M') >= 5)
            {
                mapData[x, y] = 'M';
                removedForests.Add((x, y));
            }
        }
        
        return removedForests.ToList();
    }
    private List<(int, int)> SmoothMountainForests(List<(int, int)> forestPositions)
    {
        if (forestPositions.Count == 0) return new List<(int, int)>();
        
        HashSet<(int, int)> allForests = new HashSet<(int, int)>(forestPositions);
        HashSet<(int, int)> newForests = new HashSet<(int, int)>();
        const int maxIterations = 10;
        
        for (int iteration = 0; iteration < maxIterations; iteration++)
        {
            HashSet<(int, int)> candidates = new HashSet<(int, int)>();
            
            foreach ((int x, int y) in allForests)
            {
                foreach ((int nx, int ny) in GetNeighbors(x, y))
                {
                    if (mapData[nx, ny] == 'P' && !allForests.Contains((nx, ny)))
                    {
                        candidates.Add((nx, ny));
                    }
                }
            }
            
            if (candidates.Count == 0) break;
            
            bool addedAny = false;
            foreach ((int nx, int ny) in candidates)
            {
                int forestNeighbors = CountSurroundingBiomes(nx, ny, 'F');
                int distanceToMountain = GetClosestDistanceOfType(nx, ny, 'M');
                
                if (forestNeighbors >= 5 || distanceToMountain <= 2)
                {
                    mapData[nx, ny] = 'F';
                    allForests.Add((nx, ny));
                    newForests.Add((nx, ny));
                    addedAny = true;
                }
            }
            
            if (!addedAny) break;
        }
        
        return newForests.ToList();
    }
    private List<(int, int)> CarveOutMountainForests(List<(int, int)> forestPositions)
    {
        if (forestPositions.Count == 0) return new List<(int, int)>();
        
        HashSet<(int, int)> removedForests = new HashSet<(int, int)>();
        
        foreach ((int x, int y) in forestPositions)
        {
            if (CountSurroundingBiomes(x, y, 'P') >= 5)
            {
                mapData[x, y] = 'P';
                removedForests.Add((x, y));
            }
        }
        
        return removedForests.ToList();
    }
    private void RemoveObscureMountains()
    {
        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                if (mapData[x, y] != 'M' && mapData[x, y] != 'm') continue;
                
                int nonMountainCount = CountSurroundingBiomes(x, y, 'P') + CountSurroundingBiomes(x, y, 'F');
                if (nonMountainCount == 8)
                {
                    mapData[x, y] = GetMostSurroundedBiome(x, y);
                }
            }
        }
    }
    private List<(int, int)> MakeMountainForestOpenings(List<(int, int)> forestPositions)
    {
        var newForestPositions = new List<(int, int)>();
        foreach ((int x, int y) in forestPositions)
        {
            var neighbors = GetNeighbors(x, y);
            foreach ((int nx, int ny) in neighbors)
            {
                if (mapData[nx, ny] == 'M' && CountSurroundingBiomes(nx, ny, 'F') >= 4 && CountSurroundingBiomes(nx, ny, 'm') < 1)
                {
                    mapData[nx, ny] = 'F';
                    newForestPositions.Add((nx, ny));
                }
            }
        }
        return newForestPositions;
    }
    private List<(int, int)> RemoveHarshForests(double removalChance)
    {
        var removedForests = new List<(int, int)>();
        bool type = rng.NextDouble() > removalChance;
        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                if (mapData[x, y] != 'F') continue;
                int surroundingMountains = CountSurroundingBiomes(x, y, 'M') + CountSurroundingBiomes(x, y, 'm');
                if (surroundingMountains >= 6 || CountSurroundingBiomes(x, y, 'm') >= 1 && type)
                {
                    mapData[x, y] = 'M';
                    removedForests.Add((x, y));
                }
                else if (surroundingMountains >= 6 || CountSurroundingBiomes(x, y, 'm') >= 1 && !type)
                {
                    var neighbors = GetCardinalNeighbors(x, y);
                    foreach ((int nx, int ny) in neighbors)
                    {
                        if (mapData[nx, ny] == 'm')
                        {
                            mapData[nx, ny] = 'M';
                        }
                    }
                }
            }
        }
        return removedForests;
    }
    private void RemoveObscurePlainsNearMountains()
    {
        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                if (mapData[x, y] != 'P') continue;

                int surroundingCount = CountSurroundingBiomes(x, y, 'M') + CountSurroundingBiomes(x, y, 'm') + CountSurroundingBiomes(x, y, 'F');

                if (surroundingCount >= 8)
                {
                    mapData[x, y] = 'M';
                }
            }
        }
    }
    private List<(int, int)> FillEncosedMountainPlains()
    {
        const int enclosedSizeThreshold = 50;
        HashSet<(int, int)> processed = new HashSet<(int, int)>();
        List<(int, int)> filledPositions = new List<(int, int)>();
        
        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                if (processed.Contains((x, y))) continue;
                if (mapData[x, y] != 'P' && mapData[x, y] != 'F') continue;
                
                int surroundingCount = CountSurroundingBiomes(x, y, 'M') + CountSurroundingBiomes(x, y, 'm') + CountSurroundingBiomes(x, y, 'F');
                
                if (surroundingCount < 5) continue;
                
                var region = GetConnectedRegion(x, y, 'P', processed);
                if (region.Count > 0 && region.Count <= enclosedSizeThreshold)
                {
                    foreach ((int rx, int ry) in region)
                    {
                        mapData[rx, ry] = 'F';
                        filledPositions.Add((rx, ry));
                    }
                }
            }
        }
        
        return filledPositions;
    }
    private List<(int, int)> GetConnectedRegion(int startX, int startY, char targetBiome, HashSet<(int, int)> processedRegions)
    {
        List<(int, int)> region = new List<(int, int)>();
        Queue<(int, int)> queue = new Queue<(int, int)>();
        HashSet<(int, int)> visited = new HashSet<(int, int)>();
        
        queue.Enqueue((startX, startY));
        visited.Add((startX, startY));

        while (queue.Count > 0)
        {
            (int x, int y) = queue.Dequeue();
            
            if (mapData[x, y] != targetBiome) continue;
            
            region.Add((x, y));
            processedRegions.Add((x, y));

            foreach ((int nx, int ny) in GetNeighbors(x, y))
            {
                if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                if (visited.Contains((nx, ny))) continue;
                if (mapData[nx, ny] != targetBiome) continue;
                
                queue.Enqueue((nx, ny));
                visited.Add((nx, ny));
            }
        }

        return region;
    }
    private void FillInMountainGaps()
    {
        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                if (mapData[x, y] == 'P' || mapData[x, y] == 'F')
                {
                    int surroundingMountains = CountSurroundingBiomes(x, y, 'M') + CountSurroundingBiomes(x, y, 'm');
                    if (surroundingMountains >= 5)
                    {
                        mapData[x, y] = 'M';
                    }
                }
            }
        }
    }
    public void GenerateSnowPeaks()
    {
        const double snowPeakChance = 0.40;
        bool[,] visited = new bool[width, height];
        
        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                if (visited[x, y]) continue;
                
                char tile = mapData[x, y];
                if (tile != 'm' && tile != 'S') continue;
                
                int surrounding = CountSurroundingBiomes(x, y, 'm') + CountSurroundingBiomes(x, y, 'S');
                if (surrounding >= 7 && rng.NextDouble() < snowPeakChance)
                {
                    SpreadSnowPeaks(x, y, visited);
                }
            }
        }
    }
    private void SpreadSnowPeaks(int startX, int startY, bool[,] visited)
    {
        Queue<(int, int)> queue = new Queue<(int, int)>();
        queue.Enqueue((startX, startY));
        visited[startX, startY] = true;
        int processed = 0;
        const int maxProcessed = 1000;
        
        while (queue.Count > 0 && processed < maxProcessed)
        {
            (int x, int y) = queue.Dequeue();
            mapData[x, y] = 'S';
            processed++;
            
            foreach ((int nx, int ny) in GetNeighbors(x, y))
            {
                if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                if (visited[nx, ny] || mapData[nx, ny] != 'M') continue;
                if (CountSurroundingBiomes(nx, ny, 'M') < 8) continue;
                if (rng.NextDouble() >= 0.8) continue;
                
                queue.Enqueue((nx, ny));
                visited[nx, ny] = true;
            }
        }
    }
    public void DeleteBadSnowPeaks()
    {
        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                if (mapData[x, y] == 'S' && 
                    CountSurroundingBiomes(x, y, 'M') + CountSurroundingBiomes(x, y, 'm') < 8)
                {
                    mapData[x, y] = 'M';
                }
            }
        }
    }
    #endregion
}
