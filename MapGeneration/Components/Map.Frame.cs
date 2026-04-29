using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Internal;
using static Internal.GUI;

public partial class Map
{
    #region map frame
    private void FrameMap(TileId frameChar)
    {
        // Top and bottom borders
        for (int x = 0; x < width; x++)
        {
            mapData[x, 0] = frameChar;
            mapData[x, height - 1] = frameChar;
        }

        // Left and right borders
        for (int y = 0; y < height; y++)
        {
            mapData[0, y] = frameChar;
            mapData[width - 1, y] = frameChar;
        }
    }
    private void CreateComplexFrame()
    {
        (int, int) topLeft = (0, 0);
        (int, int) bottomRight = (width - 1, height - 1);
        (int, int) topRight = (width - 1, 0);
        (int, int) bottomLeft = (0, height - 1);

        List<(int x, int y)> startingPoints = [
            GetRandomPointOnPath(GetPath(topLeft, bottomRight), 10, 55),
            GetRandomPointOnPath(GetPath(topRight, bottomLeft), 10, 55),
            GetRandomPointOnPath(GetPath(bottomRight, topLeft), 10, 55),
            GetRandomPointOnPath(GetPath(bottomLeft, topRight), 10, 55)
        ];

        List<(int x, int y)> points = [.. startingPoints];

        // Generate additional random points on each line
        int maxAdditionalLinePoints = 4;
        points.AddRange(GenerateRandomPointsOnLineWithDistance(topLeft, topRight, 2, 6, 8, maxAdditionalLinePoints));
        points.AddRange(GenerateRandomPointsOnLineWithDistance(topRight, bottomRight, 2, 6, 8, maxAdditionalLinePoints));
        points.AddRange(GenerateRandomPointsOnLineWithDistance(bottomRight, bottomLeft, 2, 6, 8, maxAdditionalLinePoints));
        points.AddRange(GenerateRandomPointsOnLineWithDistance(bottomLeft, topLeft, 2, 6, 8, maxAdditionalLinePoints));

        // Connect the points using the nearest neighbor approach
        ConnectPointsNearestNeighbor(points);
        ReplaceFrameWithWater();
        DeployLandEaters();
        SmoothContinent();
    }
    private void ReplaceFrameWithWater()
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (mapData[x, y] == TileId.Border) FillCircle(x, y, TileId.Ocean, 2, 4);
            }
        }
    }
    private void DeployLandEaters()
    {
        // Deploy eaters on each tile on the top and bottom edges
        for (int x = 0; x < width; x++)
        {
            SpreadWaterUntilHit(x, 0); // Top edge
            SpreadWaterUntilHit(x, height - 1); // Bottom edge
        }

        // Deploy eaters on each tile on the left and right edges
        for (int y = 0; y < height; y++)
        {
            SpreadWaterUntilHit(0, y); // Left edge
            SpreadWaterUntilHit(width - 1, y); // Right edge
        }
    }
    private void SpreadWaterUntilHit(int startX, int startY)
    {
        if (IsOceanTile(startX, startY)) return;

        Queue<(int, int)> queue = [];
        queue.Enqueue((startX, startY));
        bool[,] visited = new bool[width, height];
        visited[startX, startY] = true;

        while (queue.Count > 0)
        {
            (int x, int y) = queue.Dequeue();
            mapData[x, y] = TileId.Ocean;

            foreach ((int nx, int ny) in GetNeighbors(x, y))
            {
                if (nx >= 0 && nx < width && ny >= 0 && ny < height && !visited[nx, ny])
                {
                    if (!IsOceanTile(nx, ny)) queue.Enqueue((nx, ny));
                    visited[nx, ny] = true;
                }
            }
        }
    }
    private (int x, int y) GetRandomPointOnPath(List<(int x, int y)> path, int minRange, int maxRange)
    {
        Random rng = new(seed);
        int index = rng.Next(minRange, Math.Min(maxRange, path.Count));
        return path[index];
    }
    private List<(int x, int y)> GenerateRandomPointsOnLineWithDistance((int x, int y) start, (int x, int y) end, int minRange, int maxRange, int minDistanceFromStart, int maxAdditionalPoints)
    {
        List<(int x, int y)> points = [];
        List<(int x, int y)> path = GetPath(start, end);
        Random rng = new(seed);
        int additionalPointsCount = 0;

        for (int i = minRange; i < path.Count - minRange && additionalPointsCount < maxAdditionalPoints; i++)
        {
            if (rng.NextDouble() < 0.1) // Small chance to generate a point
            {
                (int x, int y) point = path[i];
                if (GetDistance(start.x, start.y, point.x, point.y) >= minDistanceFromStart && GetDistance(end.x, end.y, point.x, point.y) >= minDistanceFromStart)
                {
                    points.Add(point);
                    additionalPointsCount++;
                }
            }
        }

        return points;
    }
    private void ConnectPointsNearestNeighbor(List<(int x, int y)> points)
    {
        List<(int x, int y)> remainingPoints = [.. points];
        List<(int x, int y)> connectedPoints = [remainingPoints[0]];
        remainingPoints.RemoveAt(0);

        while (remainingPoints.Count > 0)
        {
            (int x, int y) = connectedPoints[^1];
            (int x, int y) nearestPoint = remainingPoints.OrderBy(p => GetDistance(x, y, p.x, p.y)).First();
            connectedPoints.Add(nearestPoint);
            remainingPoints.Remove(nearestPoint);
        }

        // Connect the points in order
        for (int i = 0; i < connectedPoints.Count - 1; i++)
        {
            ConnectPoints(connectedPoints[i], connectedPoints[i + 1]);
        }
        // Connect the last point to the first to close the loop
        ConnectPoints(connectedPoints[^1], connectedPoints[0]);
    }
    private List<(int x, int y)> GenerateRandomPointsOnLine((int x, int y) start, (int x, int y) end, int minRange, int maxRange, int minDistanceFromStart)
    {
        List<(int x, int y)> points = [];
        List<(int x, int y)> path = GetPath(start, end);
        Random rng = new(seed);

        for (int i = minRange; i < path.Count - minRange; i++)
        {
            if (rng.NextDouble() < 0.1) // Small chance to generate a point
            {
                (int x, int y) point = path[i];
                if (GetDistance(start.x, start.y, point.x, point.y) >= minDistanceFromStart && GetDistance(end.x, end.y, point.x, point.y) >= minDistanceFromStart) points.Add(point);
            }
        }

        return points;
    }
    private TileId GetMostSurroundingBiome(int x, int y)
    {
        Dictionary<TileId, int> biomeCounts = [];
        foreach ((int nx, int ny) in GetNeighbors(x, y))
        {
            TileId biome = mapData[nx, ny];
            if (biomeCounts.ContainsKey(biome)) biomeCounts[biome]++;
            else biomeCounts[biome] = 1;
        }
        return biomeCounts.OrderByDescending(b => b.Value).First().Key;
    }
    private void SmoothContinent()
    {
        // First pass: Basic smoothing
        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                if (mapData[x, y] == TileId.Plains || mapData[x, y] == TileId.Forest)
                {
                    int surroundingWater = CountSurroundingBiomesAny(x, y, TileId.Ocean, TileId.OceanShallow);
                    int surroundingPlains = CountSurroundingBiomes(x, y, TileId.Plains);
                    int surroundingForest = CountSurroundingBiomes(x, y, TileId.Forest);

                    if (surroundingWater > surroundingPlains + surroundingForest) mapData[x, y] = TileId.Ocean;
                    else if (surroundingForest > surroundingPlains) mapData[x, y] = TileId.Forest;
                    else if (surroundingPlains > surroundingForest) mapData[x, y] = TileId.Plains;
                }
            }
        }

        // Second pass: Advanced smoothing to create rounded edges
        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                if (mapData[x, y] == TileId.Plains || mapData[x, y] == TileId.Forest)
                {
                    int surroundingWater = CountSurroundingBiomesAny(x, y, TileId.Ocean, TileId.OceanShallow);
                    int surroundingPlains = CountSurroundingBiomes(x, y, TileId.Plains);
                    int surroundingForest = CountSurroundingBiomes(x, y, TileId.Forest);

                    if (surroundingWater > surroundingPlains + surroundingForest) mapData[x, y] = TileId.Ocean;
                    else if (surroundingForest > surroundingPlains) mapData[x, y] = TileId.Forest;
                    else if (surroundingPlains > surroundingForest) mapData[x, y] = TileId.Plains;
                }
            }
        }

        // Third pass: Final smoothing to ensure consistency
        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                if (mapData[x, y] == TileId.Plains || mapData[x, y] == TileId.Forest)
                {
                    int surroundingWater = CountSurroundingBiomesAny(x, y, TileId.Ocean, TileId.OceanShallow);
                    int surroundingPlains = CountSurroundingBiomes(x, y, TileId.Plains);
                    int surroundingForest = CountSurroundingBiomes(x, y, TileId.Forest);

                    if (surroundingWater > surroundingPlains + surroundingForest) mapData[x, y] = TileId.Ocean;
                    else if (surroundingForest > surroundingPlains) mapData[x, y] = TileId.Forest;
                    else if (surroundingPlains > surroundingForest) mapData[x, y] = TileId.Plains;
                }
            }
        }
    }
    private List<(int x, int y)> GetPath((int x, int y) start, (int x, int y) end)
    {
        List<(int x, int y)> path = [];
        int dx = end.x - start.x;
        int dy = end.y - start.y;
        int steps = Math.Max(Math.Abs(dx), Math.Abs(dy));
        double stepX = dx / (double)steps;
        double stepY = dy / (double)steps;

        for (int i = 0; i <= steps; i++)
        {
            int x = start.x + (int)(i * stepX);
            int y = start.y + (int)(i * stepY);
            path.Add((x, y));
        }

        return path;
    }
    private void ConnectPoints((int x, int y) start, (int x, int y) end)
    {
        List<(int x, int y)> path = GetPath(start, end);
        foreach ((int x, int y) in path)
        {
            if (x >= 0 && x < width && y >= 0 && y < height) mapData[x, y] = TileId.Border;
        }
    }
    #endregion
}
