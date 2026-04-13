using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Internal;
using static Internal.GUI;

public partial class Map
{
    #region useful functions
    private int GetClosestDistanceOfType(int startX, int startY, TileId tileType)
    {
        (int x, int y) = GetClosestTileOfType(startX, startY, tileType);
        if (x == -1 && y == -1)
        {
            return -1;
        }
        return (int)GetDistance(startX, startY, x, y);
    }
    private (int, int) GetClosestTileOfType(int startX, int startY, TileId tileType)
    {
        Queue<(int, int)> toVisit = new Queue<(int, int)>();
        HashSet<(int, int)> visited = new HashSet<(int, int)>();
        toVisit.Enqueue((startX, startY));
        visited.Add((startX, startY));

        while (toVisit.Count > 0)
        {
            (int x, int y) = toVisit.Dequeue();

            if (IsInBiome(x, y, tileType))
            {
                return (x, y);
            }

            foreach ((int nx, int ny) in GetNeighbors(x, y))
            {
                if (!visited.Contains((nx, ny)))
                {
                    toVisit.Enqueue((nx, ny));
                    visited.Add((nx, ny));
                }
            }
        }

        return (-1, -1);
    }
    private (int, int) GetRandomPoint()
    {
        int x = rng.Next(0, width);
        int y = rng.Next(0, height);
        return (x, y);
    }
    private (int, int) GetRandomPointInRange(int startX, int startY, int minDistance, int maxDistance)
    {
        int endX, endY;
        int attempts = 0;
        int maxAttempts = 100;
        int minDistSq = minDistance * minDistance;

        do
        {
            endX = rng.Next(startX - maxDistance, startX + maxDistance + 1);
            endY = rng.Next(startY - maxDistance, startY + maxDistance + 1);
            attempts++;

            if (attempts >= maxAttempts)
            {
                // Fallback to start position if no valid point found
                return (startX, startY);
            }
        } while ((endX - startX) * (endX - startX) + (endY - startY) * (endY - startY) < minDistSq ||
                endX < 0 || endX >= width || endY < 0 || endY >= height);
        
        return (endX, endY);
    }
    public (int, int) GetRandomPointInBiome(TileId biome)
    {
        List<(int, int)> biomePoints = new List<(int, int)>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (mapData[x, y] == biome)
                {
                    biomePoints.Add((x, y));
                }
            }
        }

        if (biomePoints.Count == 0)
        {
            outputBuffer.Add($"No points found in biome '{biome}'");
        }

        int randomIndex = rng.Next(biomePoints.Count);
        if (biomePoints.Count > 0)
        {
            return biomePoints[randomIndex];
        }
        return (-1, -1);
    }
    public (int, int) GetRandomPointInBiomeWithTilePool(List<TileId> tilePool)
    {
        // Pre-build weight map so we do O(1) lookup per tile instead of O(tilePool) per tile
        var weights = new Dictionary<TileId, int>();
        foreach (TileId t in tilePool)
            weights[t] = weights.GetValueOrDefault(t, 0) + 1;

        var biomePoints = new List<(int, int)>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (weights.TryGetValue(mapData[x, y], out int w))
                    for (int i = 0; i < w; i++)
                        biomePoints.Add((x, y));
            }
        }

        if (biomePoints.Count == 0)
        {
            outputBuffer.Add($"No points found in specified tile pool.");
            return (-1, -1);
        }

        return biomePoints[rng.Next(biomePoints.Count)];
    }

    public (int, int) GetRandomPointInBiomeInRange(TileId biome, int centerX, int centerY, int minDistance, int maxDistance)
    {
        List<(int, int)> biomePoints = new List<(int, int)>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (mapData[x, y] == biome)
                {
                    double distance = GetDistance(centerX, centerY, x, y);
                    if (distance >= minDistance && distance <= maxDistance)
                    {
                        biomePoints.Add((x, y));
                    }
                }
            }
        }

        if (biomePoints.Count == 0)
        {
            outputBuffer.Add($"No points found in biome '{biome}' within range.");
        }

        int randomIndex = rng.Next(biomePoints.Count);
        if (biomePoints.Count > 0)
        {
            return biomePoints[randomIndex];
        }
        return (-1, -1);
    }
    private bool IsInBiome(int x, int y, TileId biome)
    {
        return x >= 0 && x < width && y >= 0 && y < height && mapData[x, y] == biome;
    }
    private double GetDistance(int x1, int y1, int x2, int y2)
    {
        double dx = x2 - x1, dy = y2 - y1;
        return Math.Sqrt(dx * dx + dy * dy);
    }
    private int CountSurroundingBiomes(int x, int y, TileId biome)
    {
        int count = 0;
        if (x > 0 && mapData[x - 1, y] == biome) count++;
        if (x < width - 1 && mapData[x + 1, y] == biome) count++;
        if (y > 0 && mapData[x, y - 1] == biome) count++;
        if (y < height - 1 && mapData[x, y + 1] == biome) count++;
        if (x > 0 && y > 0 && mapData[x - 1, y - 1] == biome) count++;
        if (x < width - 1 && y > 0 && mapData[x + 1, y - 1] == biome) count++;
        if (x > 0 && y < height - 1 && mapData[x - 1, y + 1] == biome) count++;
        if (x < width - 1 && y < height - 1 && mapData[x + 1, y + 1] == biome) count++;
        return count;
    }
    // Counts neighbors matching any of the given tile types in a single 8-neighbor pass.
    private int CountSurroundingBiomesAny(int x, int y, params TileId[] biomes)
    {
        var set = new HashSet<TileId>(biomes);
        int count = 0;
        if (x > 0              && set.Contains(mapData[x - 1, y    ])) count++;
        if (x < width - 1      && set.Contains(mapData[x + 1, y    ])) count++;
        if (y > 0              && set.Contains(mapData[x,     y - 1])) count++;
        if (y < height - 1     && set.Contains(mapData[x,     y + 1])) count++;
        if (x > 0 && y > 0              && set.Contains(mapData[x - 1, y - 1])) count++;
        if (x < width - 1 && y > 0      && set.Contains(mapData[x + 1, y - 1])) count++;
        if (x > 0 && y < height - 1     && set.Contains(mapData[x - 1, y + 1])) count++;
        if (x < width - 1 && y < height - 1 && set.Contains(mapData[x + 1, y + 1])) count++;
        return count;
    }
    private void ReplaceBiome(TileId oldBiome, TileId newBiome)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (mapData[x, y] == oldBiome)
                {
                    mapData[x, y] = newBiome;
                }
            }
        }
    }
    private IEnumerable<(int, int)> GetNeighbors(int x, int y)
    {
        if (x > 0) yield return (x - 1, y);
        if (x < width - 1) yield return (x + 1, y);
        if (y > 0) yield return (x, y - 1);
        if (y < height - 1) yield return (x, y + 1);
        if (x > 0 && y > 0) yield return (x - 1, y - 1);
        if (x < width - 1 && y > 0) yield return (x + 1, y - 1);
        if (x > 0 && y < height - 1) yield return (x - 1, y + 1);
        if (x < width - 1 && y < height - 1) yield return (x + 1, y + 1);
    }
    private IEnumerable<(int, int)> GetCardinalNeighbors(int x, int y)
    {
        if (x > 0) yield return (x - 1, y);
        if (x < width - 1) yield return (x + 1, y);
        if (y > 0) yield return (x, y - 1);
        if (y < height - 1) yield return (x, y + 1);
    }
    private double[,] GeneratePerlinNoiseMap(int width, int height)
    {
        double[,] noiseMap = new double[width, height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int sampleX = x;
                int sampleY = y;
                _ = Perlin.GeneratePerlinNoise(width, height, conf.NoiseScale, rng.Next())[x, y];
                double noiseValue = noiseMap[x, y];
                noiseMap[x, y] = noiseValue;
            }
        }
        return noiseMap;
    }
    private void SmoothMap(int smoothFactor)
    {
        // Dead code — retained for potential future use.
        // Originally averaged tile chars numerically; not applicable with typed TileId.
    }
    private List<(int, int)> ReconstructPath(Dictionary<(int, int), (int, int)> cameFrom, (int, int) current)
    {
        List<(int, int)> path = new List<(int, int)>();
        while (cameFrom.ContainsKey(current))
        {
            path.Add(current);
            current = cameFrom[current];
        }
        path.Reverse();
        return path;
    }
    private int CountMissingTiles(int x, int y)
    {
        int missingTiles = 8;
        int[,] directions = new int[,] {
            { -1, -1 }, { 0, -1 }, { 1, -1 },
            { -1, 0 },           { 1, 0 },
            { -1, 1 }, { 0, 1 }, { 1, 1 }
        };

        for (int i = 0; i < directions.GetLength(0); i++)
        {
            int nx = x + directions[i, 0];
            int ny = y + directions[i, 1];

            if (nx >= 0 && nx < width && ny >= 0 && ny < height)
            {
                missingTiles--;
            }
        }

        return missingTiles;
    }
    private int GetListNeighbors(int x, int y, List<(int, int)> tileList)
    {
        var neighbours = GetNeighbors(x, y);
        int neighbourCount = 0;
        foreach (var neighbour in neighbours) if (tileList.Contains(neighbour)) neighbourCount++;
        return neighbourCount;
    }
    private void SpreadTile(int startX, int startY, double spreadChance, int minSpread, int maxSpread)
    {
        TileId tile = mapData[startX, startY];
        Queue<(int, int)> queue = new Queue<(int, int)>();
        queue.Enqueue((startX, startY));
        int spreadCount = 0;

        while (queue.Count > 0 && spreadCount < maxSpread)
        {
            (int x, int y) = queue.Dequeue();
            if (rng.NextDouble() <= spreadChance)
            {
                mapData[x, y] = tile;
                spreadCount++;

                foreach ((int nx, int ny) in GetNeighbors(x, y))
                {
                    if (spreadCount < maxSpread && mapData[nx, ny] != tile)
                    {
                        queue.Enqueue((nx, ny));
                    }
                }
            }
        }

        // Ensure minimum spread
        while (spreadCount < minSpread && queue.Count > 0)
        {
            (int x, int y) = queue.Dequeue();
            mapData[x, y] = tile;
            spreadCount++;

            foreach ((int nx, int ny) in GetNeighbors(x, y))
            {
                if (spreadCount < maxSpread && mapData[nx, ny] != tile)
                {
                    queue.Enqueue((nx, ny));
                }
            }
        }
    }
    private void SpreadTileOnTile(int startX, int startY, TileId targetTile, double spreadChance, int minSpread, int maxSpread)
    {
        TileId tile = mapData[startX, startY];
        Queue<(int, int)> queue = new Queue<(int, int)>();
        HashSet<(int, int)> visited = new HashSet<(int, int)>();
        queue.Enqueue((startX, startY));
        visited.Add((startX, startY));
        int spreadCount = 0;

        while (queue.Count > 0 && spreadCount < maxSpread)
        {
            (int x, int y) = queue.Dequeue();
            
            foreach ((int nx, int ny) in GetNeighbors(x, y))
            {
                if (visited.Contains((nx, ny)) || spreadCount >= maxSpread)
                    continue;
                    
                if (mapData[nx, ny] == targetTile && rng.NextDouble() <= spreadChance)
                {
                    mapData[nx, ny] = tile;
                    spreadCount++;
                    visited.Add((nx, ny));
                    queue.Enqueue((nx, ny));
                }
                else if (mapData[nx, ny] == targetTile)
                {
                    visited.Add((nx, ny));
                    queue.Enqueue((nx, ny));
                }
            }
        }

        // Ensure minimum spread
        while (spreadCount < minSpread && queue.Count > 0)
        {
            (int x, int y) = queue.Dequeue();
            
            foreach ((int nx, int ny) in GetNeighbors(x, y))
            {
                if (visited.Contains((nx, ny)) || spreadCount >= maxSpread)
                    continue;
                    
                if (mapData[nx, ny] == targetTile)
                {
                    mapData[nx, ny] = tile;
                    spreadCount++;
                    visited.Add((nx, ny));
                    
                    if (spreadCount >= minSpread)
                        break;
                        
                    queue.Enqueue((nx, ny));
                }
            }
        }
    }
    private List<(int, int)> GetSpreadTiles(int startX, int startY, double spreadChance, int minSpread, int maxSpread)
    {
        List<(int, int)> spreadTiles = new List<(int, int)>();
        Queue<(int, int)> queue = new Queue<(int, int)>();
        queue.Enqueue((startX, startY));
        int spreadCount = 0;

        while (queue.Count > 0 && spreadCount < maxSpread)
        {
            (int x, int y) = queue.Dequeue();
            if (rng.NextDouble() <= spreadChance)
            {
                spreadTiles.Add((x, y));
                spreadCount++;

                foreach ((int nx, int ny) in GetNeighbors(x, y))
                {
                    if (spreadCount < maxSpread && !spreadTiles.Contains((nx, ny)))
                    {
                        queue.Enqueue((nx, ny));
                    }
                }
            }
        }

        // Ensure minimum spread
        while (spreadCount < minSpread && queue.Count > 0)
        {
            (int x, int y) = queue.Dequeue();
            spreadTiles.Add((x, y));
            spreadCount++;

            foreach ((int nx, int ny) in GetNeighbors(x, y))
            {
                if (spreadCount < maxSpread && !spreadTiles.Contains((nx, ny)))
                {
                    queue.Enqueue((nx, ny));
                }
            }
        }

        return spreadTiles;
    }
    public (int, int) GetMapCenter()
    {
        int centerX = width / 2;
        int centerY = height / 2;

        // Adjust for even dimensions
        if (width % 2 == 0) centerX -= 1;
        if (height % 2 == 0) centerY -= 1;

        return (centerX, centerY);
    }
    private void FillCircle(int x, int y, TileId tile, int minRadius, int maxRadius)
    {
        Random rng = new Random(seed);
        int radius = rng.Next(minRadius, maxRadius + 1);

        for (int i = -radius; i <= radius; i++)
        {
            for (int j = -radius; j <= radius; j++)
            {
                int nx = x + i;
                int ny = y + j;

                if (nx >= 0 && nx < width && ny >= 0 && ny < height && i * i + j * j <= radius * radius)
                {
                    mapData[nx, ny] = tile;
                }
            }
        }
    }
    private bool IsOceanTile(int x, int y)
        => mapData[x, y] == TileId.Ocean;
    private void RemoveSeperatedOceanTiles()
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (mapData[x, y] == TileId.Ocean)
                {
                    int plainsCount = 0;
                    int forestCount = 0;

                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            int nx = x + dx;
                            int ny = y + dy;

                            if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                            {
                                if (mapData[nx, ny] == TileId.Plains) plainsCount++;
                                if (mapData[nx, ny] == TileId.Forest) forestCount++;
                            }
                        }
                    }

                    if (plainsCount >= 5) mapData[x, y] = TileId.Plains;
                    else if (forestCount >= 5) mapData[x, y] = TileId.Forest;
                }
            }
        }
    }
    private void FloodFillRegion(int startX, int startY, TileId biomeType, bool[,] visited, List<(int x, int y)> region)
    {
        Queue<(int, int)> queue = new Queue<(int, int)>();
        queue.Enqueue((startX, startY));
        visited[startX, startY] = true;
        
        while (queue.Count > 0)
        {
            (int x, int y) = queue.Dequeue();
            region.Add((x, y));
            
            foreach ((int nx, int ny) in GetNeighbors(x, y))
            {
                if (!visited[nx, ny] && mapData[nx, ny] == biomeType)
                {
                    visited[nx, ny] = true;
                    queue.Enqueue((nx, ny));
                }
            }
        }
    }
    private void ExpandSmallBiome(List<(int x, int y)> region, TileId biomeType)
    {
        int targetSize = conf.MinBiomeSize * conf.MinBiomeSize;
        HashSet<(int, int)> regionSet = new HashSet<(int, int)>(region);
        
        while (region.Count < targetSize)
        {
            // Find border tiles
            HashSet<(int, int)> candidates = new HashSet<(int, int)>();
            foreach ((int x, int y) in region)
            {
                foreach ((int nx, int ny) in GetNeighbors(x, y))
                {
                    if (!regionSet.Contains((nx, ny)))
                    {
                        candidates.Add((nx, ny));
                    }
                }
            }
            
            if (candidates.Count == 0) break;
            
            // Expand to nearest candidate
            var expansion = candidates.OrderBy(_ => rng.Next()).First();
            mapData[expansion.Item1, expansion.Item2] = biomeType;
            region.Add(expansion);
            regionSet.Add(expansion);
        }
    }
    private int GetBiomeSize(int startX, int startY)
    {
        TileId biomeType = mapData[startX, startY];
        bool[,] visited = new bool[width, height];
        return FloodFill(startX, startY, biomeType, visited);
    }
    private int FloodFill(int x, int y, TileId biomeType, bool[,] visited)
    {
        if (x < 0 || x >= width || y < 0 || y >= height || visited[x, y] || mapData[x, y] != biomeType)
        {
            return 0;
        }

        visited[x, y] = true;
        int size = 1;

        size += FloodFill(x + 1, y, biomeType, visited);
        size += FloodFill(x - 1, y, biomeType, visited);
        size += FloodFill(x, y + 1, biomeType, visited);
        size += FloodFill(x, y - 1, biomeType, visited);

        // Add diagonal checks to create larger clusters
        size += FloodFill(x + 1, y + 1, biomeType, visited);
        size += FloodFill(x - 1, y - 1, biomeType, visited);
        size += FloodFill(x + 1, y - 1, biomeType, visited);
        size += FloodFill(x - 1, y + 1, biomeType, visited);

        return size;
    }
    private void ExpandBiome(int startX, int startY)
    {
        TileId biomeType = mapData[startX, startY];
        Queue<(int, int)> queue = new Queue<(int, int)>();
        queue.Enqueue((startX, startY));

        while (queue.Count > 0)
        {
            (int x, int y) = queue.Dequeue();
            if (x < 0 || x >= width || y < 0 || y >= height || mapData[x, y] == biomeType)
            {
                continue;
            }

            mapData[x, y] = biomeType;

            queue.Enqueue((x + 1, y));
            queue.Enqueue((x - 1, y));
            queue.Enqueue((x, y + 1));
            queue.Enqueue((x, y - 1));
        }
    }
    public double totalTemp { get; set; }
    public double totalHum { get; set; }
    private void SetAvarageTempatureHumidity()
    {
        for (int x = 0; x < conf.Width; x++)
        {
            for (int y = 0; y < conf.Height; y++)
            {
                totalTemp += tempatureNoise[x, y];
                totalHum += humidityNoise[x, y];
                avarageTempature = Math.Clamp(totalTemp / (x * y), 0, 1);
                avarageHumidity = totalHum / (x * y);
            }
        }
        //outputBuffer.Add($"Avarage tempature: {avarageTempature}");
        //outputBuffer.Add($"Avarage humidity: {avarageHumidity}");
    }
    private void AssignTempAndHumData()
    {
        for (int x = 0; x < conf.Width; x++)
        {
            for (int y = 0; y < conf.Height; y++)
            {
                double tempature = tempatureNoise[x, y];
                double humidity = humidityNoise[x, y];
                int tempZone = tempature switch
                {
                    < 0.0 => throw new ArgumentOutOfRangeException(nameof(tempature), "Temperature cannot be negative."),
                    < 0.1 => 1, // Very Cold
                    < 0.3 => 2, // Cold
                    < 0.5 => 3, // Cool
                    < 0.7 => 4, // Temperate
                    _ => 5,      // Warm
                };

                int humZone = humidity switch
                {
                    < 0.0 => throw new ArgumentOutOfRangeException(nameof(humidity), "Humidity cannot be negative."),
                    < 0.1 => 1, // Very Dry
                    < 0.3 => 2, // Dry
                    < 0.5 => 3, // Moderate
                    < 0.7 => 4, // Humid
                    _ => 5,      // Very Humid
                };
                temperatureData[x, y] = tempZone;
                humidityData[x, y] = humZone;
            }
        }
    }
    private void SmoothOutTempatureHumidity()
    {
        // Create temporary buffers to store smoothed data
        int[,] tempData = new int[conf.Width, conf.Height];
        int[,] humData = new int[conf.Width, conf.Height];

        // Number of smoothing iterations
        int smoothingIterations = 3;

        for (int iteration = 0; iteration < smoothingIterations; iteration++)
        {
            for (int x = 1; x < conf.Width - 1; x++)
            {
                for (int y = 1; y < conf.Height - 1; y++)
                {
                    // Smooth temperature
                    var tempNeighbors = new List<int>();
                    for (int ny = -1; ny <= 1; ny++)
                    {
                        for (int nx = -1; nx <= 1; nx++)
                        {
                            if (nx == 0 && ny == 0) continue;
                            tempNeighbors.Add(temperatureData[x + nx, y + ny]);
                        }
                    }
                    int tempMode = tempNeighbors
                        .GroupBy(zone => zone)
                        .OrderByDescending(g => g.Count())
                        .First()
                        .Key;
                    tempData[x, y] = tempMode;

                    // Smooth humidity
                    var humNeighbors = new List<int>();
                    for (int ny = -1; ny <= 1; ny++)
                    {
                        for (int nx = -1; nx <= 1; nx++)
                        {
                            if (nx == 0 && ny == 0) continue;
                            humNeighbors.Add(humidityData[x + nx, y + ny]);
                        }
                    }
                    int humMode = humNeighbors
                        .GroupBy(zone => zone)
                        .OrderByDescending(g => g.Count())
                        .First()
                        .Key;
                    humData[x, y] = humMode;
                }
            }

            // Update the main data with smoothed data
            for (int x = 1; x < conf.Width - 1; x++)
            {
                for (int y = 1; y < conf.Height - 1; y++)
                {
                    temperatureData[x, y] = tempData[x, y];
                    humidityData[x, y] = humData[x, y];
                }
            }
        }

        // Final pass to assure no straight edges by adjusting single outliers
        AssureNoStraightEdges(temperatureData);
        AssureNoStraightEdges(humidityData);
    }
    private void AssureNoStraightEdges(int[,] zoneData)
    {
        for (int a = 0; a < conf.BiomeBlend; a++)
        {
            for (int x = 1; x < conf.Width - 1; x++)
            {
                for (int y = 1; y < conf.Height - 1; y++)
                {
                    int currentZone = zoneData[x, y];
                    int[] surroundingZones = new int[8];
                    int index = 0;
                    for (int ny = -1; ny <= 1; ny++)
                    {
                        for (int nx = -1; nx <= 1; nx++)
                        {
                            if (nx == 0 && ny == 0) continue;
                            surroundingZones[index++] = zoneData[x + nx, y + ny];
                        }
                    }

                    var zoneGroups = surroundingZones.GroupBy(z => z)
                                                    .OrderByDescending(g => g.Count())
                                                    .ToList();

                    if (zoneGroups.First().Count() == 5)
                    {
                        // 80% chance to enforce the dominant zone on the current tile
                        if (rng.NextDouble() < 0.8)
                        {
                            zoneData[x, y] = zoneGroups.First().Key;
                        }

                        // Chance to fill some surrounding tiles that don't have the dominant zone
                        foreach (var zone in zoneGroups.Skip(1))
                        {
                            if (rng.NextDouble() < 0.5) // 50% chance to change each non-dominant zone
                            {
                                for (int i = 0; i < surroundingZones.Length; i++)
                                {
                                    if (surroundingZones[i] == zone.Key && rng.NextDouble() < 0.5)
                                    {
                                        int dx = (i % 3) - 1;
                                        int dy = (i / 3) - 1;
                                        zoneData[x + dx, y + dy] = zoneGroups.First().Key;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    #endregion
}
