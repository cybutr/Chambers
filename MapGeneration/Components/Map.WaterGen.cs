using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Internal;
using static Internal.GUI;

public partial class Map
{
    #region river functions
    private void CreateRiver()
    {
        int maxRivers = 1; // Maximum number of rivers to generate
        for (int r = 0; r < maxRivers; r++)
        {
            // Choose a random starting point on any edge, ensuring it's at least 10 tiles away from corners
            int startX, startY;
            int startEdge; // 0 = top, 1 = bottom, 2 = left, 3 = right
            switch (rng.Next(2))
            {
                case 0:
                    startX = rng.Next(10, width - 10);
                    if (rng.Next(2) == 0)
                    {
                        startY = 0; // Top edge
                        startEdge = 0;
                    }
                    else
                    {
                        startY = height - 1; // Bottom edge
                        startEdge = 1;
                    }
                    break;
                default:
                    startY = rng.Next(10, Math.Max(11, height - 10));
                    if (rng.Next(2) == 0)
                    {
                        startX = 0; // Left edge
                        startEdge = 2;
                    }
                    else
                    {
                        startX = width - 1; // Right edge
                        startEdge = 3;
                    }
                    break;
            }

            int x = startX;
            int y = startY;

            // Define the river path
            while (true)
            {
                int riverWidth = rng.Next(conf.MinRiverWidth, conf.MaxRiverWidth + 1);
                for (int i = -riverWidth / 2; i <= riverWidth / 2; i++)
                {
                    if (x + i >= 0 && x + i < width) mapData[x + i, y] = TileId.River;
                    if (y + i >= 0 && y + i < height) mapData[x, y + i] = TileId.River;
                }

                // Randomly choose the next direction, with a bias towards moving forward
                int direction = rng.Next(100);
                if (direction < 30)
                {
                    if (startEdge == 2 || startEdge == 3) y += rng.Next(2) == 0 ? 1 : -1;
                    else x += rng.Next(2) == 0 ? 1 : -1;
                }
                else if (direction < 60)
                {
                    if (startEdge == 2 || startEdge == 3) x += startEdge == 2 ? 1 : -1;
                    else y += startEdge == 0 ? 1 : -1;
                }
                else
                {
                    if (startEdge == 2 || startEdge == 3) y += rng.Next(2) == 0 ? 1 : -1;
                    else x += rng.Next(2) == 0 ? 1 : -1;
                }

                if (x < 0) x = 0;
                if (x >= width) x = width - 1;
                if (y < 0) y = 0;
                if (y >= height) y = height - 1;

                if ((startEdge == 0 && y == height - 1) || (startEdge == 1 && y == 0) ||
                    (startEdge == 2 && x == width - 1) || (startEdge == 3 && x == 0) ||
                    (startEdge != 0 && startEdge != 1 && (y == 0 || y == height - 1)) ||
                    (startEdge != 2 && startEdge != 3 && (x == 0 || x == width - 1))) break;
            }

            // Ensure the river reaches an edge
            while (true)
            {
                int riverWidth = rng.Next(conf.MinRiverWidth, conf.MaxRiverWidth + 1);
                for (int i = -riverWidth / 2; i <= riverWidth / 2; i++)
                {
                    if (x + i >= 0 && x + i < width) mapData[x + i, y] = TileId.River;
                    if (y + i >= 0 && y + i < height) mapData[x, y + i] = TileId.River;
                }

                if (x > 0 && x < width - 1) x += x < width / 2 ? 1 : -1;
                else if (y > 0 && y < height - 1) y += y < height / 2 ? 1 : -1;

                if (x < 0) x = 0;
                if (x >= width) x = width - 1;
                if (y < 0) y = 0;
                if (y >= height) y = height - 1;

                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                {
                    if ((startEdge == 2 || startEdge == 3) && Math.Abs(y - startY) < height / 3)
                    {
                        y = (y + height / 3) % height;
                        if (Math.Abs(y - startY) < height / 3) _ = (y + height / 2) % height;
                    }
                    else if ((startEdge == 0 || startEdge == 1) && Math.Abs(x - startX) < width / 3)
                    {
                        x = (x + width / 3) % width;
                        if (Math.Abs(x - startX) < width / 3) _ = (x + width / 2) % width;
                    }
                    break;
                }
            }
        }

        SmoothRiverEdges();
    }
    private void SmoothRiverEdges()
    {
        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                if (mapData[x, y] == TileId.River)
                {
                    int riverCount = CountSurroundingBiomes(x, y, TileId.River);
                    if (riverCount < 3) mapData[x, y] = GetMostSurroundedBiome(x, y);
                }
            }
        }
    }
    #endregion
    #region lake functions
    private void CreateLakes()
    {
        int maxLakes = rng.Next(1, 4);
        int minRadius = 5;
        int maxRadius = 15;

        for (int i = 0; i < maxLakes; i++)
        {
            (int x, int y) startPoint = FindValidStartingPoint();
            if (startPoint == (-1, -1)) continue;

            int targetCount = rng.Next(1, 4);
            List<(int x, int y)> targetPoints = new(targetCount);

            for (int j = 0; j < targetCount; j++)
            {
                (int x, int y) targetPoint = FindValidTargetPoint(startPoint, minRadius, maxRadius);
                if (targetPoint != (-1, -1)) targetPoints.Add(targetPoint);
            }

            if (targetPoints.Count > 0) GenerateLakePath(startPoint, targetPoints);
        }

        SmoothLakeEdges();
    }
    private (int x, int y) FindValidStartingPoint()
    {
        for (int attempts = 0; attempts < 100; attempts++)
        {
            int x = rng.Next(0, width);
            int y = rng.Next(0, height);

            if (IsValidStartingPoint(x, y)) return (x, y);
        }
        return (-1, -1);
    }
    private bool IsValidStartingPoint(int x, int y)
    {
        TileId tile = mapData[x, y];
        if (tile != TileId.Plains && tile != TileId.Forest) return false;

        int minX = Math.Max(0, x - 6);
        int maxX = Math.Min(width - 1, x + 6);
        int minY = Math.Max(0, y - 6);
        int maxY = Math.Min(height - 1, y + 6);

        for (int nx = minX; nx <= maxX; nx++)
        {
            for (int ny = minY; ny <= maxY; ny++)
            {
                TileId neighborTile = mapData[nx, ny];
                if (neighborTile == TileId.Ocean || neighborTile == TileId.Lake || neighborTile == TileId.River ||
                    neighborTile == TileId.Mountain || neighborTile == TileId.MountainDeep || neighborTile == TileId.Snow) return false;
            }
        }
        return true;
    }
    private (int x, int y) FindValidTargetPoint((int x, int y) startPoint, int minRadius, int maxRadius)
    {
        for (int attempts = 0; attempts < 100; attempts++)
        {
            int radius = rng.Next(minRadius, maxRadius + 1);
            double angle = rng.NextDouble() * 2 * Math.PI;
            int x = startPoint.x + (int)(radius * Math.Cos(angle));
            int y = startPoint.y + (int)(radius * Math.Sin(angle));

            if (IsValidTargetPoint(x, y)) return (x, y);
        }
        return (-1, -1);
    }
    private bool IsValidTargetPoint(int x, int y)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) return false;

        TileId tile = mapData[x, y];
        if (tile == TileId.Mountain || tile == TileId.MountainDeep || tile == TileId.Snow) return false;

        int distToMountain = GetClosestDistanceOfType(x, y, TileId.Mountain);
        int distToDeepMountain = GetClosestDistanceOfType(x, y, TileId.MountainDeep);
        if (distToMountain != -1 && distToMountain < 3) return false;
        if (distToDeepMountain != -1 && distToDeepMountain < 3) return false;

        int minX = Math.Max(0, x - 8);
        int maxX = Math.Min(width - 1, x + 8);
        int minY = Math.Max(0, y - 8);
        int maxY = Math.Min(height - 1, y + 8);

        for (int nx = minX; nx <= maxX; nx++)
        {
            for (int ny = minY; ny <= maxY; ny++)
            {
                TileId neighborTile = mapData[nx, ny];
                if (neighborTile == TileId.Ocean || neighborTile == TileId.Lake || neighborTile == TileId.River) return false;
            }
        }
        return true;
    }
    private void GenerateLakePath((int x, int y) startPoint, List<(int x, int y)> targetPoints)
    {
        foreach ((int x, int y) target in targetPoints)
        {
            int dx = target.x - startPoint.x;
            int dy = target.y - startPoint.y;
            int steps = Math.Max(Math.Abs(dx), Math.Abs(dy));

            if (steps == 0) continue;

            double stepX = dx / (double)steps;
            double stepY = dy / (double)steps;

            for (int i = 0; i <= steps; i++)
            {
                int x = startPoint.x + (int)(i * stepX);
                int y = startPoint.y + (int)(i * stepY);
                FillCircle(x, y, TileId.Lake, 2, 5);
            }
        }
    }
    private void SmoothLakeEdges()
    {
        const int smoothingThreshold = 4;

        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                if (mapData[x, y] != TileId.Lake)
                {
                    int lakeNeighbors = 0;
                    bool assigned = false;
                    for (int dx = -1; dx <= 1 && !assigned; dx++)
                    {
                        for (int dy = -1; dy <= 1 && !assigned; dy++)
                        {
                            if ((dx != 0 || dy != 0) && mapData[x + dx, y + dy] == TileId.Lake)
                            {
                                lakeNeighbors++;
                                if (lakeNeighbors >= smoothingThreshold)
                                {
                                    mapData[x, y] = TileId.Lake;
                                    assigned = true;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    #endregion
    #region stream functions
    private void CreateStreams()
    {
        AStar aStar = new();
        List<(int, int)>? values;
        List<(int, int)> streamPositions = [];
        int attempts = 0;
        bool isInBorder = false;
        do
        {
            streamPositions.Clear();
            (int, int) start, end, closestMountain;
            List<TileId> tilePool = [TileId.River, TileId.Ocean, TileId.Lake, TileId.Lake, TileId.Lake];
            end = GetRandomPointInBiomeWithTilePool(tilePool);
            closestMountain = GetClosestTileOfType(end.Item1, end.Item2, TileId.Mountain);
            int distance = (int)Math.Round(GetDistance(end.Item1, end.Item2, closestMountain.Item1, closestMountain.Item2));
            if (distance < 15)
            {
                attempts++;
                continue;
            }
            start = GetRandomPointInBiomeInRange(TileId.Mountain, end.Item1, end.Item2, distance, distance + 2);
            values = aStar.FindPath(mapData, start.Item1, start.Item2, end.Item1, end.Item2);
            if (values == null) return;
            isInBorder = false;
            foreach ((int, int) value in values)
            {
                bool hasWater = false;
                var neighbors = GetCardinalNeighbors(value.Item1, value.Item2);
                streamPositions.Add((value.Item1, value.Item2));
                foreach (var neighbor in neighbors)
                {
                    TileId neighborTile = mapData[neighbor.Item1, neighbor.Item2];
                    if (neighborTile == TileId.Lake || neighborTile == TileId.River ||
                        neighborTile == TileId.Ocean || neighborTile == TileId.Stream) hasWater = true;
                }
                if (mapData[value.Item1, value.Item2] == TileId.Border) isInBorder = true;
                if (hasWater) break;
            }
            attempts++;
        } while (isInBorder && streamPositions.Count < 15 && attempts < 50);
        foreach ((int, int) position in streamPositions) mapData[position.Item1, position.Item2] = TileId.Stream;
    }
    #endregion
    #region beach functions
    private void CreateBeaches()
    {
        double avarageTempatureFactor = avarageTempature switch
        {
            < 0.0 => throw new ArgumentOutOfRangeException(nameof(avarageTempature), "Temperature cannot be negative."),
            < 0.1 => 0.02, // Very Cold
            < 0.3 => 0.03, // Cold
            < 0.5 => 0.03, // Cool
            < 0.7 => 0.06, // Temperate
            _ => 0.09,      // Warm
        };
        double beachChance = Math.Clamp((avarageTempature - 0.4) * avarageTempatureFactor, 0.001, 0.03);
        int minBeachSize = (int)Math.Clamp(25 + (avarageTempature - 0.5) * 20, 20, 50);
        int maxBeachSize = (int)Math.Clamp(35 + (avarageTempature - 0.5) * 20, 30, 68);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (mapData[x, y] == TileId.Ocean && IsNextToLand(x, y) && !IsNextToMountain(x, y))
                {
                    if (rng.NextDouble() < beachChance) CreateSmoothBeach(x, y, minBeachSize, maxBeachSize);
                }
            }
        }
        BeachesDepth();
        SmoothBeachEdges();
    }
    private bool IsNextToLand(int x, int y)
    {
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = x + dx;
                int ny = y + dy;
                if (nx >= 0 && ny >= 0 && nx < width && ny < height &&
                    (mapData[nx, ny] == TileId.Plains || mapData[nx, ny] == TileId.Forest)) return true;
            }
        }
        return false;
    }
    private bool IsNextToMountain(int x, int y)
    {
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = x + dx;
                int ny = y + dy;
                if (nx >= 0 && ny >= 0 && nx < width && ny < height &&
                    (mapData[nx, ny] == TileId.Mountain || mapData[nx, ny] == TileId.MountainDeep || mapData[nx, ny] == TileId.Snow)) return true;
            }
        }
        return false;
    }
    private void CreateSmoothBeach(int startX, int startY, int minSize, int maxSize)
    {
        int beachSize = rng.Next(minSize, maxSize + 1);
        Queue<(int, int)> queue = [];
        queue.Enqueue((startX, startY));
        bool[,] visited = new bool[width, height];
        visited[startX, startY] = true;

        while (queue.Count > 0 && beachSize > 0)
        {
            (int x, int y) = queue.Dequeue();
            if (mapData[x, y] == TileId.Plains || mapData[x, y] == TileId.Forest)
            {
                mapData[x, y] = TileId.Beach;
                beachSize--;
            }

            foreach ((int nx, int ny) in GetNeighbors(x, y))
            {
                if (nx >= 0 && nx < width && ny >= 0 && ny < height && !visited[nx, ny])
                {
                    queue.Enqueue((nx, ny));
                    visited[nx, ny] = true;
                }
            }
        }
    }
    private void BeachesDepth()
    {
        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                if (mapData[x, y] == TileId.Beach)
                {
                    if (rng.NextDouble() < 0.1)
                    {
                        mapData[x, y] = TileId.BeachDark;
                        SpreadTile(x, y, 0.5, 1, 3);
                    }
                }
            }
        }
    }
    private void SmoothBeachEdges()
    {
        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                if (mapData[x, y] == TileId.Beach && CountSurroundingBiomes(x, y, TileId.Beach) < 5) mapData[x, y] = GetMostSurroundingBiome(x, y);
            }
        }
    }
    #endregion
    private void WaterDepth()
    {
        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                // A water tile becomes its depth variant when all 8 neighbors are water
                int allWater = CountSurroundingBiomesAny(x, y,
                    TileId.Lake, TileId.LakeShallow,
                    TileId.Ocean, TileId.OceanShallow,
                    TileId.River, TileId.RiverShallow,
                    TileId.Stream);
                var def = TileRegistry.Get(mapData[x, y]);
                if (def.DeepVariant.HasValue && allWater == 8) mapData[x, y] = def.DeepVariant.Value;
            }
        }
    }
}
