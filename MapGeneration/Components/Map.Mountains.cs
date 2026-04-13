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
                if (mapData[x, y] == TileId.Plains || mapData[x, y] == TileId.Forest)
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

            (int newStartX, int newStartY) = GetRandomPointInBiome(TileId.Mountain);
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
                        mapData[nx, y] = TileId.Mountain;
                    if (Math.Abs(i) <= halfWidth && x >= 0 && x < width)
                        mapData[x, ny] = TileId.Mountain;
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
                if ((mapData[x, y] == TileId.Mountain || mapData[x, y] == TileId.MountainDeep) &&
                    CountSurroundingBiomesAny(x, y, TileId.Mountain, TileId.MountainDeep) == 8)
                {
                    mapData[x, y] = TileId.MountainDeep;
                }
            }
        }

        int maxAttempts = 500;
        int successfulAttempts = 0;

        for (int attempt = 0; attempt < maxAttempts && successfulAttempts < 42; attempt++)
        {
            if (rng.NextDouble() >= 0.6) continue;

            (int px, int py) = GetRandomPointInBiome(TileId.MountainDeep);
            if (px == -1) break;

            mapData[px, py] = TileId.Mountain;
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
                    TileId tile = mapData[x, y];
                    if (tile != TileId.Mountain && tile != TileId.MountainDeep && tile != TileId.Snow) continue;

                    double totalErosion = 0;

                    foreach ((int nx, int ny) in GetNeighbors(x, y))
                    {
                        TileId neighbor = mapData[nx, ny];
                        if (neighbor == TileId.Plains || neighbor == TileId.Forest)
                            totalErosion += windFactor;
                        if (neighbor == TileId.Ocean || neighbor == TileId.Lake || neighbor == TileId.River)
                            totalErosion += waterFactor;
                    }

                    totalErosion += rng.NextDouble() * tempFactor;

                    if (totalErosion > erosionThreshold)
                    {
                        if (totalErosion > 0.5)
                            mapData[x, y] = GetMostSurroundedBiome(x, y);
                        else if (totalErosion > 0.2)
                            mapData[x, y] = TileId.MountainDeep;
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
                if (mapData[x, y] == TileId.Mountain && CountSurroundingBiomes(x, y, TileId.Mountain) < 5)
                {
                    mapData[x, y] = TileId.Plains;
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
                if (mapData[fx, fy] == TileId.Mountain)
                {
                    mapData[fx, fy] = TileId.Forest;
                    forestPositions.Add((fx, fy));
                }
            }
        }
        forestPositions.AddRange(SpreadMountainForests(forestPositions));
        var obscuredForests = new List<(int, int)>();
        for (int x = 0; x <= 3; x++) obscuredForests.AddRange(FillObscureForests(forestPositions));
        var obscuredSet = new HashSet<(int, int)>(obscuredForests);
        forestPositions.RemoveAll(p => obscuredSet.Contains(p));
        forestPositions.AddRange(SmoothMountainForests(forestPositions));
        var carvedForests = new HashSet<(int, int)>(CarveOutMountainForests(forestPositions));
        forestPositions.RemoveAll(p => carvedForests.Contains(p));
        RemoveObscurePlainsNearMountains();
        forestPositions.AddRange(FillEncosedMountainPlains());
        forestPositions.AddRange(MakeMountainForestOpenings(forestPositions));
        var harshForests = new List<(int, int)>();
        for (int x = 0; x <= 2; x++) harshForests.AddRange(RemoveHarshForests(0.65));
        var harshSet = new HashSet<(int, int)>(harshForests);
        forestPositions.RemoveAll(p => harshSet.Contains(p));

    }
    private (int, int) GetStartingForestPosition()
    {
        const int maxAttempts = 100;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var (x, y) = GetRandomPointInBiome(TileId.Mountain);
            if (x == -1) return (-1, -1);

            if (CountSurroundingBiomesAny(x, y, TileId.Plains, TileId.Forest) >= 5)
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
            if (CountSurroundingBiomes(x, y, TileId.Forest) <= 3) continue;

            var spreadTiles = GetSpreadTiles(x, y, 0.95, 15, 30);
            foreach ((int sx, int sy) in spreadTiles)
            {
                if (processed.Contains((sx, sy))) continue;
                if (mapData[sx, sy] != TileId.Plains) continue;
                if (GetClosestDistanceOfType(sx, sy, TileId.Mountain) > 2) continue;

                mapData[sx, sy] = TileId.Forest;
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
            int mountainCount = CountSurroundingBiomes(x, y, TileId.MountainDeep) + CountSurroundingBiomes(x, y, TileId.Mountain);
            if (mountainCount >= 1 && CountSurroundingBiomes(x, y, TileId.Mountain) >= 5)
            {
                mapData[x, y] = TileId.Mountain;
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
                    if (mapData[nx, ny] == TileId.Plains && !allForests.Contains((nx, ny)))
                    {
                        candidates.Add((nx, ny));
                    }
                }
            }

            if (candidates.Count == 0) break;

            bool addedAny = false;
            foreach ((int nx, int ny) in candidates)
            {
                int forestNeighbors = CountSurroundingBiomes(nx, ny, TileId.Forest);
                int distanceToMountain = GetClosestDistanceOfType(nx, ny, TileId.Mountain);

                if (forestNeighbors >= 5 || distanceToMountain <= 2)
                {
                    mapData[nx, ny] = TileId.Forest;
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
            if (CountSurroundingBiomes(x, y, TileId.Plains) >= 5)
            {
                mapData[x, y] = TileId.Plains;
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
                if (mapData[x, y] != TileId.Mountain && mapData[x, y] != TileId.MountainDeep) continue;

                int nonMountainCount = CountSurroundingBiomesAny(x, y, TileId.Plains, TileId.Forest);
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
                if (mapData[nx, ny] == TileId.Mountain && CountSurroundingBiomes(nx, ny, TileId.Forest) >= 4 && CountSurroundingBiomesAny(nx, ny, TileId.MountainDeep) < 1)
                {
                    mapData[nx, ny] = TileId.Forest;
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
                if (mapData[x, y] != TileId.Forest) continue;
                int surroundingMountains = CountSurroundingBiomesAny(x, y, TileId.Mountain, TileId.MountainDeep);
                int surroundingDeep = CountSurroundingBiomes(x, y, TileId.MountainDeep);
                if (surroundingMountains >= 6 || surroundingDeep >= 1 && type)
                {
                    mapData[x, y] = TileId.Mountain;
                    removedForests.Add((x, y));
                }
                else if (surroundingMountains >= 6 || surroundingDeep >= 1 && !type)
                {
                    var neighbors = GetCardinalNeighbors(x, y);
                    foreach ((int nx, int ny) in neighbors)
                    {
                        if (mapData[nx, ny] == TileId.MountainDeep)
                        {
                            mapData[nx, ny] = TileId.Mountain;
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
                if (mapData[x, y] != TileId.Plains) continue;

                int surroundingCount = CountSurroundingBiomesAny(x, y, TileId.Mountain, TileId.MountainDeep, TileId.Forest);

                if (surroundingCount >= 8)
                {
                    mapData[x, y] = TileId.Mountain;
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
                if (mapData[x, y] != TileId.Plains && mapData[x, y] != TileId.Forest) continue;

                int surroundingCount = CountSurroundingBiomesAny(x, y, TileId.Mountain, TileId.MountainDeep, TileId.Forest);

                if (surroundingCount < 5) continue;

                var region = GetConnectedRegion(x, y, TileId.Plains, processed);
                if (region.Count > 0 && region.Count <= enclosedSizeThreshold)
                {
                    foreach ((int rx, int ry) in region)
                    {
                        mapData[rx, ry] = TileId.Forest;
                        filledPositions.Add((rx, ry));
                    }
                }
            }
        }

        return filledPositions;
    }
    private List<(int, int)> GetConnectedRegion(int startX, int startY, TileId targetBiome, HashSet<(int, int)> processedRegions)
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
                if (mapData[x, y] == TileId.Plains || mapData[x, y] == TileId.Forest)
                {
                    int surroundingMountains = CountSurroundingBiomesAny(x, y, TileId.Mountain, TileId.MountainDeep);
                    if (surroundingMountains >= 5)
                    {
                        mapData[x, y] = TileId.Mountain;
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

                TileId tile = mapData[x, y];
                if (tile != TileId.MountainDeep && tile != TileId.Snow) continue;

                int surrounding = CountSurroundingBiomesAny(x, y, TileId.MountainDeep, TileId.Snow);
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
            mapData[x, y] = TileId.Snow;
            processed++;

            foreach ((int nx, int ny) in GetNeighbors(x, y))
            {
                if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                if (visited[nx, ny] || mapData[nx, ny] != TileId.Mountain) continue;
                if (CountSurroundingBiomes(nx, ny, TileId.Mountain) < 8) continue;
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
                if (mapData[x, y] == TileId.Snow &&
                    CountSurroundingBiomesAny(x, y, TileId.Mountain, TileId.MountainDeep) < 8)
                {
                    mapData[x, y] = TileId.Mountain;
                }
            }
        }
    }
    #endregion
}
