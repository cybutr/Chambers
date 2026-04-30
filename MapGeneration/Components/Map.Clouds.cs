using System;
using System.Collections.Generic;
using System.Linq;
using Internal;
using static Internal.GUI;

public partial class Map
{
    public void InitializeClouds()
    {
        int maxC = weather.CurrentWeather switch
        {
            WeatherType.Clear => 20,
            WeatherType.Rain => 60,
            WeatherType.Snow => 55,
            WeatherType.Thunderstorm => 85,
            WeatherType.Fog => 30,
            WeatherType.Overcast => 40,
            WeatherType.Hail => 45,
            WeatherType.Sleet => 50,
            WeatherType.Drizzle => 35,
            WeatherType.BlowingSnow => 40,
            WeatherType.Sandstorm => 30,
            _ => 30
        };
        for (int i = 0; i < maxC; i++)
        {
            (int x, int y) = GetRandomCloudPoint();
            CloudType type = GetCloudTypeForCurrentWeather();
            GenerateCloudCluster(x, y, type);
        }
    }
    private CloudType GetRandomCloudType()
    {
        Array values = Enum.GetValues(typeof(CloudType));
        return (CloudType)values.GetValue(rng.Next(values.Length))!;
    }
    #region cloud updating
    public int cloudFormations {get; set;}
    public int cloudTileCount  {get; set;}
    public void UpdateClouds()
    {
        tick++;
        MoveClouds();
        RemoveEdgeClouds();
        timeSinceLastCloudSpawn += deltaTime;
        int maxClouds = GetMaxCloudsForCurrentWeather();
        (cloudFormations, cloudTileCount) = ComputeCloudStats();
        if ((timeSinceLastCloudSpawn > GetCloudCooldown()) && (cloudFormations < maxClouds))
        {
            timeSinceLastCloudSpawn = 0.0;
            (int x, int y) = GetStartingPositionBasedOnWindDirection();
            CloudType type = GetCloudTypeForCurrentWeather();
            GenerateCloudCluster(x, y, type);
        }
    }
    private const double CLOUD_SPEED_SCALE = 0.04;
    private void MoveClouds()
    {
        double radians = weather.WindDirection * (Math.PI / 180.0);
        double speed   = weather.WindSpeed * CLOUD_SPEED_SCALE * deltaTime;

        _cloudAccumX += Math.Cos(radians) * speed + (rng.NextDouble() - 0.5) * weather.WindSpeed * 0.002 * deltaTime;
        _cloudAccumY += Math.Sin(radians) * speed + (rng.NextDouble() - 0.5) * weather.WindSpeed * 0.002 * deltaTime;

        int shiftX = (int)_cloudAccumX;
        int shiftY = (int)_cloudAccumY;
        _cloudAccumX -= shiftX;
        _cloudAccumY -= shiftY;

        if (shiftX != 0 || shiftY != 0) ShiftCloudData(shiftX, shiftY);

        if (tick % (ulong)conf.CloudMorphInterval == 0) SmoothAndFluffClouds();
    }
    private void ShiftCloudData(int dx, int dy)
    {
        Array.Clear(_cloudDataSwap, 0, _cloudDataSwap.Length);
        Array.Clear(_cloudDepthSwap, 0, _cloudDepthSwap.Length);

        for (int x = 0; x < cloudDataWidth; x++)
        for (int y = 0; y < cloudDataHeight; y++)
        {
            int sx = x - dx;
            int sy = y - dy;
            if (sx < 0 || sx >= cloudDataWidth || sy < 0 || sy >= cloudDataHeight) continue;
            _cloudDataSwap[x, y]  = cloudData[sx, sy];
            _cloudDepthSwap[x, y] = cloudDepthData[sx, sy];
        }

        (cloudData, _cloudDataSwap)       = (_cloudDataSwap, cloudData);
        (cloudDepthData, _cloudDepthSwap) = (_cloudDepthSwap, cloudDepthData);
    }
    public Dictionary<(int x, int y), int> cloudSizes {get; set;} = new();
    private void SmoothAndFluffClouds()
    {
        CloudType[,] tempCloudData = (CloudType[,])cloudData.Clone();
        List<(int startX, int startY, int endX, int endY)> regions = GetCloudRegions();
        object lockObj = new();
        Random localRng = new(seed);

        // Precompute cloud sizes

        // Identify all cloud positions and assign them to clouds
        bool[,] visited = new bool[cloudDataWidth, cloudDataHeight];

        for (int x = 0; x < cloudDataWidth; x++)
        {
            for (int y = 0; y < cloudDataHeight; y++)
            {
                if (cloudData[x, y] != CloudType.None && !visited[x, y])
                {
                    int cloudSize = GetCloudSize(x, y, tempCloudData, visited);
                    MarkCloudPositions(x, y, tempCloudData, cloudSizes, cloudSize);
                }
            }
        }

        // First pass: Smooth edges by selectively adding cloud pixels
        Parallel.ForEach(regions, region =>
        {
            (int startX, int startY, int endX, int endY) = region;
            HashSet<(int x, int y)> localAdjacentTiles = [];

            // Collect all tiles adjacent to cloud positions in this region
            for (int x = startX; x < endX; x++)
            {
                for (int y = startY; y < endY; y++)
                {
                    if (cloudData[x, y] != CloudType.None)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dy = -1; dy <= 1; dy++)
                            {
                                int newX = x + dx;
                                int newY = y + dy;

                                if (newX < 0 || newY < 0 || newX >= cloudDataWidth || newY >= cloudDataHeight) continue;

                                if (cloudData[newX, newY] == CloudType.None) localAdjacentTiles.Add((newX, newY));
                            }
                        }
                    }
                }
            }

            // Process localAdjacentTiles
            foreach ((int x, int y) in localAdjacentTiles)
            {
                // Count cloud neighbors
                int neighborCount = 0;
                CloudType neighborType = CloudType.None;

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;

                        int nx = x + dx;
                        int ny = y + dy;

                        if (nx < 0 || ny < 0 || nx >= cloudDataWidth || ny >= cloudDataHeight) continue;

                        if (cloudData[nx, ny] != CloudType.None)
                        {
                            neighborCount++;
                            neighborType = cloudData[nx, ny];
                        }
                    }
                }

                // Add cloud pixel to smooth concave edges, adjusted by cloud size
                if (neighborCount >= 5 && neighborCount <= 8)
                {
                    int cloudSize = GetRepresentativeCloudSize(x, y, cloudSizes);

                    double expansionProbability = GetExpansionProbability(cloudSize);

                    if (localRng.NextDouble() < expansionProbability)
                    {
                        lock (lockObj)
                        {
                            tempCloudData[x, y] = neighborType;
                            cloudSizes[(x, y)] = cloudSize;
                        }
                    }
                }
            }
        });

        // Second pass: Add fluffiness, expand or shrink clouds
        Parallel.ForEach(regions, region =>
        {
            (int startX, int startY, int endX, int endY) = region;
            List<(int x, int y)> localCloudPositions = [];

            for (int x = startX; x < endX; x++)
            {
                for (int y = startY; y < endY; y++)
                {
                    if (cloudData[x, y] != CloudType.None) localCloudPositions.Add((x, y));
                }
            }

            foreach ((int x, int y) in localCloudPositions)
            {
                int cloudSize = GetRepresentativeCloudSize(x, y, cloudSizes);
                double expansionProbability = GetExpansionProbability(cloudSize);
                double shrinkageProbability = GetShrinkageProbability(cloudSize);

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int newX = x + dx;
                        int newY = y + dy;

                        if (newX < 0 || newY < 0 || newX >= cloudDataWidth || newY >= cloudDataHeight) continue;

                        // Expand clouds
                        if (tempCloudData[newX, newY] == CloudType.None)
                        {
                            // Count cloud neighbors
                            int cloudNeighbors = 0;
                            CloudType neighborType = CloudType.None;

                            for (int ndx = -1; ndx <= 1; ndx++)
                            {
                                for (int ndy = -1; ndy <= 1; ndy++)
                                {
                                    if (ndx == 0 && ndy == 0) continue;

                                    int nnx = newX + ndx;
                                    int nny = newY + ndy;

                                    if (nnx < 0 || nny < 0 || nnx >= cloudDataWidth || nny >= cloudDataHeight) continue;

                                    if (tempCloudData[nnx, nny] != CloudType.None)
                                    {
                                        cloudNeighbors++;
                                        neighborType = tempCloudData[nnx, nny];
                                    }
                                }
                            }

                            // Add new fluffy cloud pixels
                            if (cloudNeighbors >= 3)
                            {
                                if (localRng.NextDouble() < expansionProbability)
                                {
                                    lock (lockObj)
                                    {
                                        tempCloudData[newX, newY] = neighborType;
                                        cloudSizes[(newX, newY)] = cloudSize;
                                    }
                                }
                            }
                        }
                        // Shrink clouds at edges
                        else
                        {
                            // Count cloud neighbors
                            int cloudNeighbors = 0;

                            for (int ndx = -1; ndx <= 1; ndx++)
                            {
                                for (int ndy = -1; ndy <= 1; ndy++)
                                {
                                    if (ndx == 0 && ndy == 0) continue;

                                    int nnx = x + ndx;
                                    int nny = y + ndy;

                                    if (nnx < 0 || nny < 0 || nnx >= cloudDataWidth || nny >= cloudDataHeight) continue;

                                    if (tempCloudData[nnx, nny] != CloudType.None) cloudNeighbors++;
                                }
                            }

                            if (cloudNeighbors <= 4 && localRng.NextDouble() < shrinkageProbability)
                            {
                                lock (lockObj)
                                {
                                    tempCloudData[x, y] = CloudType.None;
                                    cloudSizes.Remove((x, y));
                                }
                            }
                        }
                    }
                }
            }
        });

        // Final pass: Smooth out isolated pixels and rough edges
        CloudType[,] finalCloudData = (CloudType[,])tempCloudData.Clone();

        Parallel.ForEach(regions, region =>
        {
            (int startX, int startY, int endX, int endY) = region;

            for (int x = startX; x < endX; x++)
            {
                for (int y = startY; y < endY; y++)
                {
                    if (tempCloudData[x, y] != CloudType.None)
                    {
                        int neighbors = 0;

                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dy = -1; dy <= 1; dy++)
                            {
                                if (dx == 0 && dy == 0) continue;

                                int nx = x + dx;
                                int ny = y + dy;

                                if (nx < 0 || ny < 0 || nx >= cloudDataWidth || ny >= cloudDataHeight) continue;

                                if (tempCloudData[nx, ny] != CloudType.None) neighbors++;
                            }
                        }

                        // Remove if too isolated
                        if (neighbors <= 1)
                        {
                            lock (lockObj)
                            {
                                finalCloudData[x, y] = CloudType.None;
                                cloudSizes.Remove((x, y));
                            }
                        }
                    }
                }
            }
        });

        cloudData = finalCloudData;
    }
    private int GetCloudSize(int startX, int startY, CloudType[,] cloudData, bool[,] visited)
    {
        Queue<(int x, int y)> queue = [];
        queue.Enqueue((startX, startY));
        visited[startX, startY] = true;
        int size = 0;

        while (queue.Count > 0)
        {
            (int x, int y) = queue.Dequeue();
            size++;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int nx = x + dx;
                    int ny = y + dy;

                    if (nx >= 0 && nx < cloudDataWidth && ny >= 0 && ny < cloudDataHeight && cloudData[nx, ny] != CloudType.None && !visited[nx, ny])
                    {
                        visited[nx, ny] = true;
                        queue.Enqueue((nx, ny));
                    }
                }
            }
        }

        return size;
    }
    private (int minCloudSize, int maxCloudSize) GetMaxAndMinSizesOfClouds()
    {
        int minCloudSize, maxCloudSize;
        switch (GetCloudTypeForCurrentWeather())
        {
            case CloudType.Cirrus:
                minCloudSize = 33;
                maxCloudSize = 44;
                break;
            case CloudType.Altocumulus:
                minCloudSize = 49;
                maxCloudSize = 68;
                break;
            case CloudType.Cumulus:
                minCloudSize = 67;
                maxCloudSize = 82;
                break;
            case CloudType.Cumulonimbus:
                minCloudSize = 123;
                maxCloudSize = 165;
                break;
            case CloudType.Nimbostratus:
                minCloudSize = 87;
                maxCloudSize = 127;
                break;
            case CloudType.Stratus:
                minCloudSize = 29;
                maxCloudSize = 44;
                break;
            default:
                minCloudSize = 60;
                maxCloudSize = 100;
                break;
        }

        return (minCloudSize, maxCloudSize);
    }
    private double GetExpansionProbability(int cloudSize)
    {
        var (minCloudSize, maxCloudSize) = GetMaxAndMinSizesOfClouds();
        if (cloudSize < minCloudSize) return 0.96;
        else if (cloudSize > maxCloudSize) return 0.11;
        else return 0.9 - (cloudSize - minCloudSize) * (0.8 / (maxCloudSize - minCloudSize));
    }
    private double GetShrinkageProbability(int cloudSize)
    {
        var (minCloudSize, maxCloudSize) = GetMaxAndMinSizesOfClouds();
        if (cloudSize < minCloudSize) return 0.1;
        else if (cloudSize > maxCloudSize) return 0.36;
        else return 0.1 + (cloudSize - minCloudSize) * (0.8 / (maxCloudSize - minCloudSize));
    }
    private (int formations, int tiles) ComputeCloudStats()
    {
        bool[,] visited = new bool[cloudDataWidth, cloudDataHeight];
        int formations = 0, tiles = 0;
        Queue<(int, int)> q = [];
        for (int x = 0; x < cloudDataWidth; x++)
        for (int y = 0; y < cloudDataHeight; y++)
        {
            if (cloudData[x, y] == CloudType.None || visited[x, y]) continue;
            formations++;
            q.Enqueue((x, y));
            visited[x, y] = true;
            while (q.Count > 0)
            {
                (int cx, int cy) = q.Dequeue();
                tiles++;
                for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = cx + dx, ny = cy + dy;
                    if (nx >= 0 && nx < cloudDataWidth && ny >= 0 && ny < cloudDataHeight
                        && !visited[nx, ny] && cloudData[nx, ny] != CloudType.None)
                    {
                        visited[nx, ny] = true;
                        q.Enqueue((nx, ny));
                    }
                }
            }
        }
        return (formations, tiles);
    }
    private static int GetRepresentativeCloudSize(int x, int y, Dictionary<(int x, int y), int> cloudSizes)
    {
        if (cloudSizes.TryGetValue((x, y), out int size)) return size;
        else return 50; // Default value if not found
    }
    private void MarkCloudPositions(int startX, int startY, CloudType[,] cloudData, Dictionary<(int x, int y), int> cloudSizes, int cloudSize)
    {
        Queue<(int x, int y)> queue = [];
        queue.Enqueue((startX, startY));
        HashSet<(int x, int y)> visited = [ (startX, startY) ];

        while (queue.Count > 0)
        {
            (int x, int y) = queue.Dequeue();
            cloudSizes[(x, y)] = cloudSize;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int nx = x + dx;
                    int ny = y + dy;

                    if (nx >= 0 && nx < cloudDataWidth && ny >= 0 && ny < cloudDataHeight && cloudData[nx, ny] != CloudType.None && !visited.Contains((nx, ny)))
                    {
                        visited.Add((nx, ny));
                        queue.Enqueue((nx, ny));
                    }
                }
            }
        }
    }
    private bool IsCloudEdgeTile(int x, int y)
    {
        if (cloudData[x, y] == CloudType.None) return false;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                int nx = x + dx;
                int ny = y + dy;

                if (nx >= 0 && nx < cloudDataWidth && ny >= 0 && ny < cloudDataHeight)
                {
                    if (cloudData[nx, ny] == CloudType.None) return true;
                }
            }
        }

        return false;
    }
    private void RemoveEdgeClouds()
    {
        for (int x = 0; x < cloudDataWidth; x++)
        {
            for (int y = 0; y < cloudDataHeight; y++)
            {
                if (IsEdgeTile(x, y) && cloudData[x, y] != CloudType.None)
                    cloudData[x, y] = CloudType.None;
            }
        }
    }
    private int CountCloudNeighbors(int x, int y, CloudType[,] cloudData)
    {
        int count = 0;
        for (int ndx = -1; ndx <= 1; ndx++)
        {
            for (int ndy = -1; ndy <= 1; ndy++)
            {
                if (ndx == 0 && ndy == 0) continue;
                int nx = x + ndx;
                int ny = y + ndy;
                if (nx < 0 || ny < 0 || nx >= cloudDataWidth || ny >= cloudDataHeight) continue;
                if (cloudData[nx, ny] != CloudType.None) count++;
            }
        }
        return count;
    }
    private int CountEmptyNeighbors(int x, int y, CloudType[,] cloudData)
    {
        int count = 0;
        for (int ndx = -1; ndx <= 1; ndx++)
        {
            for (int ndy = -1; ndy <= 1; ndy++)
            {
                if (ndx == 0 && ndy == 0) continue;
                int nx = x + ndx;
                int ny = y + ndy;
                if (nx < 0 || ny < 0 || nx >= cloudDataWidth || ny >= cloudDataHeight) continue;
                if (cloudData[nx, ny] == CloudType.None) count++;
            }
        }
        return count;
    }
    private bool IsEdgeTile(int x, int y) => x == 0 || y == 0 || x == cloudDataWidth - 1 || y == cloudDataHeight - 1;
    private bool AreCoordsInBounds(int x, int y) => x >= 0 && x < cloudDataWidth && y >= 0 && y < cloudDataHeight;
    #endregion
    #region cloud regions
    private List<(int startX, int startY, int endX, int endY)> GetCloudRegions()
    {
        int regionsPerRow = 3;
        int regionWidth = cloudDataWidth / regionsPerRow;
        int regionHeight = cloudDataHeight / regionsPerRow;

        List<(int, int, int, int)> regions = [];

        for (int i = 0; i < regionsPerRow; i++)
        {
            for (int j = 0; j < regionsPerRow; j++)
            {
                int startX = i * regionWidth;
                int startY = j * regionHeight;
                int endX = (i == regionsPerRow - 1) ? cloudDataWidth : (i + 1) * regionWidth;
                int endY = (j == regionsPerRow - 1) ? cloudDataHeight : (j + 1) * regionHeight;

                regions.Add((startX, startY, endX, endY));
            }
        }

        return regions;
    }
    private IEnumerable<(int, int)> GetNeighbors(int item1, int item2, int width, int height)
    {
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = item1 + dx;
                int ny = item2 + dy;
                if (nx >= 0 && nx < width && ny >= 0 && ny < height) yield return (nx, ny);
            }
        }
    }
    private bool IsCloudSurrounded(int x, int y, int needed)
    {
        if (x < 0 || x >= cloudDataWidth || y < 0 || y >= cloudDataHeight) return false;

        int surroundingClouds = 0;
        for (int d = 1; d <= 1; d++)
        {
            for (int dx = -d; dx <= d; dx++)
            {
                for (int dy = -d; dy <= d; dy++)
                {
                    int nx = x + dx;
                    int ny = y + dy;

                    if (IsInCloudBounds(nx, ny))
                    {
                        if (cloudData[nx, ny] == CloudType.None) surroundingClouds++;
                    }
                }
            }
        }
        return surroundingClouds >= needed;
    }
    private double Distance(double x1, double y1, double x2, double y2)
    {
        double dx = x2 - x1;
        double dy = y2 - y1;
        return Math.Sqrt(dx * dx + dy * dy);
    }
    public double timeSinceLastCloudSpawn {get; set;} = 0.0;
    private double GetCloudCooldown()
    {
        double cloudCooldownMultiplier = 1;
        double cloudCooldown = GetCloudTypeForCurrentWeather() switch
        {
            CloudType.Cirrus => 2.0 * cloudCooldownMultiplier,
            CloudType.Altocumulus => 3.0 * cloudCooldownMultiplier,
            CloudType.Cumulus => 3.5 * cloudCooldownMultiplier,
            CloudType.Cumulonimbus => 4.5 * cloudCooldownMultiplier,
            CloudType.Nimbostratus => 4.0 * cloudCooldownMultiplier,
            CloudType.Stratus => 2.0 * cloudCooldownMultiplier,
            _ => 3.0
        };
        return cloudCooldown;
    }
    private void SpawnNewCloudsBasedOnWeather()
    {
        (int x, int y) = GetStartingPositionBasedOnWindDirection();
        CloudType type = GetCloudTypeForCurrentWeather();
        SpawnCloud(x, y, type);
    }
    private int GetMaxCloudsForCurrentWeather()
    {
        int size = weather.CurrentWeather switch
        {
            WeatherType.Clear => 32,
            WeatherType.Rain => 30,
            WeatherType.Snow => 28,
            WeatherType.Thunderstorm => 22,
            WeatherType.Fog => 35,
            WeatherType.Overcast => 36,
            WeatherType.Hail => 25,
            WeatherType.Sleet => 28,
            WeatherType.Drizzle => 56,
            WeatherType.BlowingSnow => 28,
            WeatherType.Sandstorm => 22,
            _ => 30
        };
        return size;
    }
    public CloudType GetCloudTypeForCurrentWeather()
    {
        CloudType cloudType = weather.CurrentWeather switch
        {
            WeatherType.Clear => CloudType.Cumulus,
            WeatherType.Rain => CloudType.Nimbostratus,
            WeatherType.Snow => CloudType.Nimbostratus,
            WeatherType.Thunderstorm => CloudType.Cumulonimbus,
            WeatherType.Fog => CloudType.Stratus,
            WeatherType.Overcast => CloudType.Altocumulus,
            WeatherType.Hail => CloudType.Cumulonimbus,
            WeatherType.Sleet => CloudType.Nimbostratus,
            WeatherType.Drizzle => CloudType.Altocumulus,
            WeatherType.BlowingSnow => CloudType.Stratus,
            WeatherType.Sandstorm => CloudType.Cirrus,
            _ => CloudType.Cumulus
        };
        return cloudType;
    }
    private (int x, int y) GetStartingPositionBasedOnWindDirection()
    {
        int offsetC = 15;
        double windDirection = weather.WindDirection % (2 * Math.PI);
        int startX, startY;

        if (windDirection >= 0 && windDirection < Math.PI / 2)
        {
            startX = offsetC;
            startY = rng.Next(offsetC, cloudDataHeight - offsetC);
        }
        else if (windDirection >= Math.PI / 2 && windDirection < Math.PI)
        {
            startX = rng.Next(offsetC, cloudDataWidth - offsetC);
            startY = offsetC;
        }
        else if (windDirection >= Math.PI && windDirection < 3 * Math.PI / 2)
        {
            startX = cloudDataWidth - 1 - offsetC;
            startY = rng.Next(offsetC, cloudDataHeight - offsetC);
        }
        else
        {
            startX = rng.Next(offsetC, cloudDataWidth - offsetC);
            startY = cloudDataHeight - 1 - offsetC;
        }

        return (startX, startY);
    }
    private (int r, int g, int b) GetCloudColor(int x, int y)
    {
        if (x < 0 || y < 0 || x >= cloudDataWidth || y >= cloudDataHeight) return (255, 255, 255);
        (int mapX, int mapY) = CloudDataCordsToMapData(x, y);
        if (mapX < 0 || mapY < 0 || mapX >= width || mapY >= height) return (255, 255, 255);
        var baseColor = GetCloudDepthColor(cloudData[x, y], cloudDepthData[x, y]);
        int darkness = darknessData[mapX, mapY];
        if (darkness > 0) baseColor = ColorSpectrum.DarkenColor(baseColor, darkness);
        return baseColor;
    }
    #endregion
    #region cloud state
    private double GetCloudIntensity(CloudType type)
    {
        return type switch
        {
            CloudType.Cumulonimbus => 1.0,
            CloudType.Nimbostratus => 0.8,
            CloudType.Cumulus => 0.5,
            CloudType.Altocumulus => 0.3,
            CloudType.Cirrus => 0.1,
            CloudType.Stratus => 0.2,
            _ => 0.0
        };
    }
    private (int r, int g, int b) GetCloudColor(CloudType type)
    {
        return type switch
        {

            CloudType.Cirrus => ColorSpectrum.CIRRUS,
            CloudType.Altocumulus => ColorSpectrum.ALTOCUMULUS,
            CloudType.Cumulus => ColorSpectrum.CUMULUS,
            CloudType.Cumulonimbus => ColorSpectrum.CUMULONIMBUS,
            CloudType.Nimbostratus => ColorSpectrum.NIMBOSTRATUS,
            CloudType.Stratus => ColorSpectrum.STRATUS,
            _ => ColorSpectrum.CUMULUS
        };
    }
    #endregion
    #region cloud rendering
    public void UpdateCloudState() => CloudsDepth();
    public readonly int shadowRadius = 3;
    [System.Text.Json.Serialization.JsonIgnore] private double shadowIntensityFactor {get; set;}
    public void ComputeCloudShadows()
    {
        Array.Clear(shadowData, 0, shadowData.Length);

        if (!conf.DisplayShadows || shadowIntensityFactor <= 0) return;

        UpdateCloudShadows();

        for (int x = 0; x < cloudDataWidth; x++)
        for (int y = 0; y < cloudDataHeight; y++)
        {
            if (cloudData[x, y] == CloudType.None) continue;

            (int mapX, int mapY) = CloudDataCordsToMapData(x, y);
            int shadowX = mapX + cloudShadowOffsetX;
            int shadowY = mapY + cloudShadowOffsetY;

            for (int dx = -shadowRadius; dx <= shadowRadius; dx++)
            for (int dy = -shadowRadius; dy <= shadowRadius; dy++)
            {
                int sx = shadowX + dx;
                int sy = shadowY + dy;
                if (sx <= 0 || sx >= width - 1 || sy <= 0 || sy >= height - 1) continue;
                double distance = Math.Sqrt(dx * dx + dy * dy);
                if (distance > shadowRadius) continue;
                double intensity = 1.0 - (distance / shadowRadius);
                if (intensity > shadowData[sx, sy]) shadowData[sx, sy] = intensity;
            }
        }
    }
    private void UpdateCloudShadows()
    {
        // Calculate base shadow offset based on time of day
        double angle = ((dayNight.TimeOfDay - 6.0) / 24.0) * 2 * Math.PI; // Shift dayNight.TimeOfDay by 6 hours
        int baseOffsetX = (int)(-Math.Cos(angle) * 12); // Invert cosine for desired shadow offset
        int baseOffsetY = Math.Abs((int)(Math.Sin(angle) * 12)); // Ensure y offset is always positive

        double seasonalVariation = Math.Sin((dayNight.Season / 4.0) * 2 * Math.PI) * 2;
        baseOffsetX += (int)seasonalVariation;

        // Ensure the shadow is always on a higher y-coordinate than the cloud itself
        baseOffsetY = Math.Max(baseOffsetY, 3);

        // Update global shadow offset variables
        cloudShadowOffsetX = baseOffsetX;
        cloudShadowOffsetY = baseOffsetY;

        double peakShadowIntensity = 30.0;
        // Adjust shadow intensity factor based on time of day
        shadowIntensityFactor = GetShadowIntensityFactor(peakShadowIntensity);
    }
    private double GetShadowIntensityFactor(double peakShadowIntensity)
    {
        double adjustedSunriseTime = dayNight.SunriseTime + 2.0;
        double adjustedSunsetTime  = dayNight.SunsetTime  - 1.0;

        if (dayNight.TimeOfDay < adjustedSunriseTime || dayNight.TimeOfDay > adjustedSunsetTime) return 0.0;

        double noonTime = (adjustedSunriseTime + adjustedSunsetTime) / 2.0;
        double morningDuration = noonTime - adjustedSunriseTime;
        double eveningDuration = adjustedSunsetTime - noonTime;

        if (dayNight.TimeOfDay <= noonTime) return peakShadowIntensity * (dayNight.TimeOfDay - adjustedSunriseTime) / morningDuration;
        else return peakShadowIntensity * (adjustedSunsetTime - dayNight.TimeOfDay) / eveningDuration;
    }
    private bool AreCloudCoordsInMapDataBounds(int x, int y) => x >= 0 && y >= 0 && x < width && y < height;
    #endregion
    #region SpawnClouds
    public void SpawnCloud(int x, int y, CloudType type)
    {
        switch (type)
        {
            case CloudType.Cirrus:
                SpawnCirrusCloud(x, y);
                break;
            case CloudType.Altocumulus:
                SpawnAltocumulusCloud(x, y);
                break;
            case CloudType.Cumulus:
                SpawnCumulusCloud(x, y);
                break;
            case CloudType.Cumulonimbus:
                SpawnCumulonimbusCloud(x, y);
                break;
            case CloudType.Nimbostratus:
                SpawnNimbostratusCloud(x, y);
                break;
            case CloudType.Stratus:
                SpawnStratusCloud(x, y);
                break;
            default:
                SpawnCumulusCloud(x, y);
                break;
        }
    }
    private void SpawnRandomCloud(int x, int y)
    {
        CloudType cloudType = GetRandomCloudType();

        switch (cloudType)
        {
            case CloudType.Cirrus:
                SpawnCirrusCloud(x, y);
                break;
            case CloudType.Altocumulus:
                SpawnAltocumulusCloud(x, y);
                break;
            case CloudType.Cumulus:
                SpawnCumulusCloud(x, y);
                break;
            case CloudType.Cumulonimbus:
                SpawnCumulonimbusCloud(x, y);
                break;
            case CloudType.Nimbostratus:
                SpawnNimbostratusCloud(x, y);
                break;
            case CloudType.Stratus:
                SpawnStratusCloud(x, y);
                break;
            default:
                SpawnCumulusCloud(x, y);
                break;
        }
    }
    private void GenerateCloudCluster(int startX, int startY, CloudType type)
    {
        int minRadius = type switch
        {
            CloudType.Cirrus => 2,
            CloudType.Altocumulus => 3,
            CloudType.Cumulus => 6,
            CloudType.Cumulonimbus => 9,
            CloudType.Nimbostratus => 7,
            CloudType.Stratus => 2,
            _ => 8
        };

        int maxRadius = type switch
        {
            CloudType.Cirrus => 5,
            CloudType.Altocumulus => 7,
            CloudType.Cumulus => 10,
            CloudType.Cumulonimbus => 18,
            CloudType.Nimbostratus => 15,
            CloudType.Stratus => 4,
            _ => 20
        };

        int minMaxPoints = type switch
        {
            CloudType.Cirrus => 3,
            CloudType.Altocumulus => 3,
            CloudType.Cumulus => 5,
            CloudType.Cumulonimbus => 9,
            CloudType.Nimbostratus => 7,
            CloudType.Stratus => 3,
            _ => 10
        };
        int maxMaxPoints = type switch
        {
            CloudType.Cirrus => 4,
            CloudType.Altocumulus => 6,
            CloudType.Cumulus => 8,
            CloudType.Cumulonimbus => 16,
            CloudType.Nimbostratus => 13,
            CloudType.Stratus => 4,
            _ => 15
        };

        List<(int x, int y)> targetPoints = [];

        int maxPoints = rng.Next(minMaxPoints, maxMaxPoints + 1);
        // Max attepmpts to find a valid target point
        int maxAttempts = 100;
        int attempts = 0;
        for (int i = 0; i < maxPoints; i++)
        {
            (int x, int y) target = FindValidCloudTargetPoint((startX, startY), minRadius, maxRadius);
            if (target != (-1, -1)) targetPoints.Add(target);
            else i--;

            attempts++;
            if (attempts >= maxAttempts) break;
        }

        GenerateCloudPath(type, (startX, startY), targetPoints);
        AddImperfectionsOnCloudEdges(cloudData, type);
        CreateFluffyCloudEdges(cloudData, type);
        SmoothCloudShape(cloudData, type);
    }
    private (int x, int y) FindValidCloudTargetPoint((int x, int y) startPoint, int minRadius, int maxRadius)
    {
        Random rng = new(seed);
        for (int attempts = 0; attempts < 100; attempts++)
        {
            int radius = rng.Next(minRadius, maxRadius + 1);
            double angle = rng.NextDouble() * 2 * Math.PI;
            int x = startPoint.x + (int)(radius * Math.Cos(angle));
            int y = startPoint.y + (int)(radius * Math.Sin(angle));

            if (IsValidCloudTargetPoint(x, y)) return (x, y);
        }
        return (-1, -1);
    }
    private bool IsValidCloudTargetPoint(int x, int y) => x >= 0 && y >= 0 && x < cloudDataWidth && y < cloudDataHeight;
    private void GenerateCloudPath(CloudType type, (int x, int y) startPoint, List<(int x, int y)> targetPoints)
    {
        foreach ((int x, int y) target in targetPoints)
        {
            int dx = target.x - startPoint.x;
            int dy = target.y - startPoint.y;
            int steps = Math.Max(Math.Abs(dx), Math.Abs(dy));
            double stepX = dx / (double)steps;
            double stepY = dy / (double)steps;

            for (int i = 0; i <= steps; i++)
            {
                int x = startPoint.x + (int)(i * stepX);
                int y = startPoint.y + (int)(i * stepY);
                DrawCircle(type, x, y, type switch
                {
                    CloudType.Cirrus => rng.Next(2, 4),
                    CloudType.Altocumulus => rng.Next(3, 6),
                    CloudType.Cumulus => rng.Next(4, 7),
                    CloudType.Cumulonimbus => rng.Next(7, 11),
                    CloudType.Nimbostratus => rng.Next(6, 9),
                    CloudType.Stratus => rng.Next(2, 4),
                    _ => rng.Next(3, 6)
                });
            }
        }
    }
    private void AddImperfectionsOnCloudEdges(CloudType[,] cloudData, CloudType type)
    {
        for (int x = 1; x < cloudDataWidth - 1; x++)
        {
            for (int y = 1; y < cloudDataHeight - 1; y++)
            {
                if (cloudData[x, y] != CloudType.None)
                {
                    int cloudDensity = 0;
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            if (cloudData[x + dx, y + dy] != CloudType.None) cloudDensity++;
                        }
                    }

                    if (cloudDensity < 4)
                    {
                        if (rng.NextDouble() > 0.5) cloudData[x, y] = CloudType.None;
                    }
                }
            }
        }
    }
    private void DrawCircle(CloudType type, int x, int y, int radius)
    {
        for (int i = -radius; i <= radius; i++)
        {
            for (int j = -radius; j <= radius; j++)
            {
                int nx = x + i;
                int ny = y + j;

                if (nx >= 0 && nx < cloudDataWidth && ny >= 0 && ny < cloudDataHeight && i * i + j * j <= radius * radius) cloudData[nx, ny] = type;
            }
        }
    }
    private void CreateFluffyCloudEdges(CloudType[,] tempCloudData, CloudType type)
    {
        for (int x = 2; x < cloudDataWidth - 2; x++)
        {
            for (int y = 2; y < cloudDataHeight - 2; y++)
            {
                if (tempCloudData[x, y] == type)
                {
                    int cloudDensity = 0;
                    for (int dx = -2; dx <= 2; dx++)
                    {
                        for (int dy = -2; dy <= 2; dy++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            if (tempCloudData[x + dx, y + dy] == type) cloudDensity++;
                        }
                    }

                    if (cloudDensity < 6)
                    {
                        if (rng.NextDouble() > 0.5) tempCloudData[x, y] = CloudType.None;
                    }
                    else if (cloudDensity > 8)
                    {
                        if (rng.NextDouble() > 0.3) tempCloudData[x, y] = type;
                    }
                }
            }
        }
    }
    private void SmoothCloudShape(CloudType[,] tempCloudData, CloudType type)
    {
        for (int x = 1; x < cloudDataWidth - 1; x++)
        {
            for (int y = 1; y < cloudDataHeight - 1; y++)
            {
                if (tempCloudData[x, y] == type)
                {
                    int cloudDensity = 0;
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            if (tempCloudData[x + dx, y + dy] == type) cloudDensity++;
                        }
                    }

                    if (cloudDensity < 4) tempCloudData[x, y] = CloudType.None;
                }
            }
        }
    }
    private int CalculateCloudDepth(int x, int y, CloudType cloudType)
    {
        int maxDepth = cloudType switch
        {
            CloudType.Cirrus => 2,
            CloudType.Altocumulus => 3,
            CloudType.Cumulus => 4,
            CloudType.Cumulonimbus => 5,
            CloudType.Nimbostratus => 4,
            CloudType.Stratus => 3,
            _ => 3
        };

        int depth = maxDepth;

        // Check surrounding tiles to determine depth
        for (int d = 1; d <= maxDepth; d++)
        {
            bool edgeFound = false;
            for (int dx = -d; dx <= d; dx++)
            {
                for (int dy = -d; dy <= d; dy++)
                {
                    int nx = x + dx;
                    int ny = y + dy;

                    if (IsInCloudBounds(nx, ny))
                    {
                        if (cloudData[nx, ny] == CloudType.None)
                        {
                            edgeFound = true;
                            break;
                        }
                    }
                }
                if (edgeFound) break;
            }
            if (edgeFound)
            {
                depth = d;
                break;
            }
        }

        return depth;
    }
    private void CloudsDepth()
    {
        for (int x = 0; x < cloudDataWidth; x++)
        {
            for (int y = 0; y < cloudDataHeight; y++)
            {
                if (cloudData[x, y] != CloudType.None) cloudDepthData[x, y] = CalculateCloudDepth(x, y, cloudData[x, y]);
                else cloudDepthData[x, y] = 0;
            }
        }
    }
    private (int r, int g, int b) GetCloudDepthColor(CloudType cloudType, int depth)
    {
        // Define colors based on cloud type and depth
        return cloudType switch
        {
            CloudType.Cirrus => depth switch
            {
                1 => ColorSpectrum.CIRRUS_DEPTH_LIGHT,
                2 => ColorSpectrum.CIRRUS_DEPTH_MEDIUM,
                _ => ColorSpectrum.CIRRUS_DEPTH_DARK
            },
            CloudType.Altocumulus => depth switch
            {
                1 => ColorSpectrum.ALTOCUMULUS_DEPTH_LIGHT,
                2 => ColorSpectrum.ALTOCUMULUS_DEPTH_MEDIUM,
                _ => ColorSpectrum.ALTOCUMULUS_DEPTH_DARK
            },
            CloudType.Cumulus => depth switch
            {
                1 => ColorSpectrum.CUMULUS_DEPTH_LIGHT,
                2 => ColorSpectrum.CUMULUS_DEPTH_MEDIUM,
                _ => ColorSpectrum.CUMULUS_DEPTH_DARK
            },
            CloudType.Cumulonimbus => depth switch
            {
                1 => ColorSpectrum.CUMULONIMBUS_DEPTH_LIGHT,
                2 => ColorSpectrum.CUMULONIMBUS_DEPTH_MEDIUM,
                _ => ColorSpectrum.CUMULONIMBUS_DEPTH_DARK
            },
            CloudType.Nimbostratus => depth switch
            {
                1 => ColorSpectrum.NIMBOSTRATUS_DEPTH_LIGHT,
                2 => ColorSpectrum.NIMBOSTRATUS_DEPTH_MEDIUM,
                _ => ColorSpectrum.NIMBOSTRATUS_DEPTH_DARK
            },
            CloudType.Stratus => depth switch
            {
                1 => ColorSpectrum.STRATUS_DEPTH_LIGHT,
                2 => ColorSpectrum.STRATUS_DEPTH_MEDIUM,
                _ => ColorSpectrum.STRATUS_DEPTH_DARK
            },
            _ => (200, 200, 200)
        };
    }
    private void SmoothCloudEdges()
    {
        // Temporary copy of cloud data
        CloudType[,] tempCloudData = (CloudType[,])cloudData.Clone();

        for (int x = 1; x < cloudDataWidth - 1; x++)
        {
            for (int y = 1; y < cloudDataHeight - 1; y++)
            {
                if (cloudData[x, y] == CloudType.None) continue;

                int filledNeighbors = 0;

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        if (cloudData[x + dx, y + dy] != CloudType.None) filledNeighbors++;
                    }
                }

                // Apply smoothing rules
                if (filledNeighbors < 2 || filledNeighbors > 6) tempCloudData[x, y] = CloudType.None;
                else
                {
                    // Optionally, enhance fluffiness by adding more filled neighbors
                    if (filledNeighbors > 4 && cloudData[x, y] == CloudType.None) tempCloudData[x, y] = cloudData[x, y];
                }
            }
        }

        // Update cloud data with smoothed data
        cloudData = tempCloudData;
    }
    private void SpawnCumulusCloud(int x, int y) => GenerateCloudCluster(x, y, CloudType.Cumulus);
    private void SpawnCirrusCloud(int x, int y) => GenerateCloudCluster(x, y, CloudType.Cirrus);
    private void SpawnCumulonimbusCloud(int x, int y) => GenerateCloudCluster(x, y, CloudType.Cumulonimbus);
    private void SpawnNimbostratusCloud(int x, int y) => GenerateCloudCluster(x, y, CloudType.Nimbostratus);
    private void SpawnAltocumulusCloud(int x, int y) => GenerateCloudCluster(x, y, CloudType.Altocumulus);
    private void SpawnStratusCloud(int x, int y) => GenerateCloudCluster(x, y, CloudType.Stratus);
    private (int x, int y) GetRandomCloudPoint()
    {
        int x = rng.Next(0 + 15, cloudDataWidth - 15);
        int y = rng.Next(0 + 15, cloudDataHeight - 15);
        return (x, y);
    }
    private bool AreAllTilesInBounds(List<(int x, int y)> tiles)
    {
        foreach ((int cx, int cy) in tiles)
        {
            if (!IsInCloudBounds(cx, cy)) return false;
        }
        return true;
    }
    private (int x, int y) MapDataCordsToCloudData(int x, int y) => (x + cloudDataOffsetX, y + cloudDataOffsetY);
    private (int x, int y) CloudDataCordsToMapData(int x, int y) => (x - cloudDataOffsetX, y - cloudDataOffsetY);
    private bool IsInCloudBounds(int x, int y) => x >= 0 && x < cloudDataWidth && y >= 0 && y < cloudDataHeight;
    #endregion
}
