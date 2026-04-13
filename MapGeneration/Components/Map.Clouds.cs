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
    private char GetCloudSymbol(CloudType type)
    {
        return type switch
        {
            CloudType.Cirrus => '1',
            CloudType.Altocumulus => '2',
            CloudType.Cumulus => '3',
            CloudType.Cumulonimbus => '4',
            CloudType.Nimbostratus => '5',
            CloudType.Stratus => '6',
            _ => ' '
        };
    }
    private void UpdateCloudProperties()
    {
        foreach (Cloud cloud in clouds)
        {
            cloud.Speed = weather.WindSpeed * 0.02;
            cloud.Direction = weather.WindDirection;
            cloud.Precipitation = weather.Humidity / 100.0 * weather.Intensity * weather.Pressure / 1013.25 * rng.NextDouble();
        }
    }
    #region cloud updating
    private static Dictionary<(int, int), (double, double)> cloudPositions {get; set;} = new();
    public int cloudFormations {get; set;}
    public void UpdateClouds()
    {
        MoveClouds();
        RemoveEdgeClouds();
        MergeNearbyClouds();
        // Increment the timer
        timeSinceLastCloudSpawn += deltaTime;
        int maxClouds = GetMaxCloudsForCurrentWeather();
        cloudFormations = GetCloudFormations();
        if ((timeSinceLastCloudSpawn > GetCloudCooldown()) && (cloudFormations < maxClouds))
        {
            timeSinceLastCloudSpawn = 0.0;
            (int x, int y) = GetStartingPositionBasedOnWindDirection();
            CloudType type = GetCloudTypeForCurrentWeather();
            GenerateCloudCluster(x, y, type);
        }
    }
    private void MoveClouds()
    {
        double slowFactor = 0.1;
        double windSpeed = weather.WindSpeed;
        double windDirection = weather.WindDirection;
        double radians = windDirection * (Math.PI / 180);
        double velocityX = windSpeed * Math.Cos(radians) * slowFactor;
        double velocityY = windSpeed * Math.Sin(radians) * slowFactor;

        CloudType[,] newCloudData = new CloudType[cloudDataWidth, cloudDataHeight];
        Dictionary<(int, int), (double, double)> newCloudPositions = new Dictionary<(int, int), (double, double)>();

        List<(int startX, int startY, int endX, int endY)> regions = GetCloudRegions();

        object lockCloudData = new();
        object lockCloudPositions = new();

        Parallel.ForEach(regions, region =>
        {
            (int startX, int startY, int endX, int endY) = region;
            Dictionary<(int, int), (double, double)> localCloudPositions = new Dictionary<(int, int), (double, double)>();
            List<(int x, int y, CloudType value)> localNewCloudData = new List<(int x, int y, CloudType value)>();

            // Process the assigned region
            for (int x = startX; x < endX; x++)
            {
                for (int y = startY; y < endY; y++)
                {
                    if (cloudData[x, y] != CloudType.None)
                    {
                        double newX = x + velocityX;
                        double newY = y + velocityY;

                        int intX = (int)Math.Round(newX);
                        int intY = (int)Math.Round(newY);

                        if (AreCoordsInBounds(intX, intY))
                        {
                            localNewCloudData.Add((intX, intY, cloudData[x, y]));
                            localCloudPositions[(intX, intY)] = (newX, newY);
                        }
                    }
                }
            }

            // Merge local results into shared data structures using locks
            lock (lockCloudData)
            {
                foreach ((int x, int y, CloudType value) in localNewCloudData)
                {
                    newCloudData[x, y] = value;
                }
            }

            lock (lockCloudPositions)
            {
                foreach (KeyValuePair<(int, int), (double, double)> kvp in localCloudPositions)
                {
                    newCloudPositions[kvp.Key] = kvp.Value;
                }
            }
        });

        cloudData = newCloudData;
        cloudPositions = newCloudPositions;

        // Continue with other cloud updates
        //ApplyJellyEffect();
        //FillCloudHoles();
        SmoothAndFluffClouds();
    }
    public Dictionary<(int x, int y), int> cloudSizes {get; set;} = new Dictionary<(int x, int y), int>();
    private void SmoothAndFluffClouds()
    {
        CloudType[,] tempCloudData = (CloudType[,])cloudData.Clone();
        List<(int startX, int startY, int endX, int endY)> regions = GetCloudRegions();
        object lockObj = new object();
        Random localRng = new Random(seed);

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
            HashSet<(int x, int y)> localAdjacentTiles = new HashSet<(int x, int y)>();

            // Collect all tiles adjacent to cloud positions in this region
            for (int x = startX; x < endX; x++)
            {
                for (int y = startY; y < endY; y++)
                {
                    if (cloudPositions.ContainsKey((x, y)))
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dy = -1; dy <= 1; dy++)
                            {
                                int newX = x + dx;
                                int newY = y + dy;

                                if (newX < 0 || newY < 0 || newX >= cloudDataWidth || newY >= cloudDataHeight)
                                    continue;

                                if (cloudData[newX, newY] == CloudType.None)
                                {
                                    localAdjacentTiles.Add((newX, newY));
                                }
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
                        if (dx == 0 && dy == 0)
                            continue;

                        int nx = x + dx;
                        int ny = y + dy;

                        if (nx < 0 || ny < 0 || nx >= cloudDataWidth || ny >= cloudDataHeight)
                            continue;

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
            List<(int x, int y)> localCloudPositions = new List<(int x, int y)>();

            for (int x = startX; x < endX; x++)
            {
                for (int y = startY; y < endY; y++)
                {
                    if (cloudPositions.ContainsKey((x, y)))
                    {
                        localCloudPositions.Add((x, y));
                    }
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

                        if (newX < 0 || newY < 0 || newX >= cloudDataWidth || newY >= cloudDataHeight)
                            continue;

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
                                    if (ndx == 0 && ndy == 0)
                                        continue;

                                    int nnx = newX + ndx;
                                    int nny = newY + ndy;

                                    if (nnx < 0 || nny < 0 || nnx >= cloudDataWidth || nny >= cloudDataHeight)
                                        continue;

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
                                    if (ndx == 0 && ndy == 0)
                                        continue;

                                    int nnx = x + ndx;
                                    int nny = y + ndy;

                                    if (nnx < 0 || nny < 0 || nnx >= cloudDataWidth || nny >= cloudDataHeight)
                                        continue;

                                    if (tempCloudData[nnx, nny] != CloudType.None)
                                    {
                                        cloudNeighbors++;
                                    }
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
                                if (dx == 0 && dy == 0)
                                    continue;

                                int nx = x + dx;
                                int ny = y + dy;

                                if (nx < 0 || ny < 0 || nx >= cloudDataWidth || ny >= cloudDataHeight)
                                    continue;

                                if (tempCloudData[nx, ny] != CloudType.None)
                                {
                                    neighbors++;
                                }
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
        Queue<(int x, int y)> queue = new Queue<(int x, int y)>();
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
        int minCloudSize = GetMaxAndMinSizesOfClouds().minCloudSize;
        int maxCloudSize = GetMaxAndMinSizesOfClouds().maxCloudSize;

        if (cloudSize < minCloudSize)
        {
            return 0.96; // Small clouds have higher chance to expand
        }
        else if (cloudSize > maxCloudSize)
        {
            return 0.11; // Large clouds have lower chance to expand
        }
        else
        {
            return 0.9 - (cloudSize - minCloudSize) * (0.8 / (maxCloudSize - minCloudSize));
        }
    }
    private double GetShrinkageProbability(int cloudSize)
    {
        int minCloudSize = GetMaxAndMinSizesOfClouds().minCloudSize;
        int maxCloudSize = GetMaxAndMinSizesOfClouds().maxCloudSize;

        if (cloudSize < minCloudSize)
        {
            return 0.1; // Small clouds have lower chance to shrink
        }
        else if (cloudSize > maxCloudSize)
        {
            return 0.36; // Large clouds have higher chance to shrink
        }
        else
        {
            return 0.1 + (cloudSize - minCloudSize) * (0.8 / (maxCloudSize - minCloudSize));
        }
    }
    private int GetCloudFormations()
    {
        int formations = 0;
        int width = cloudDataWidth;
        int height = cloudDataHeight;
        bool[,] visited = new bool[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!visited[x, y] && cloudData[x, y] != CloudType.None)
                {
                    formations++;
                    MarkCloudFormation(x, y, visited);
                }
            }
        }

        return formations;
    }
    private void MarkCloudFormation(int startX, int startY, bool[,] visited)
    {
        Queue<(int x, int y)> queue = new Queue<(int x, int y)>();
        queue.Enqueue((startX, startY));
        visited[startX, startY] = true;

        while (queue.Count > 0)
        {
            (int x, int y) = queue.Dequeue();

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0)
                        continue;

                    int nx = x + dx;
                    int ny = y + dy;

                    if (nx >= 0 && nx < cloudDataWidth &&
                        ny >= 0 && ny < cloudDataHeight &&
                        !visited[nx, ny] && cloudData[nx, ny] != CloudType.None)
                    {
                        visited[nx, ny] = true;
                        queue.Enqueue((nx, ny));
                    }
                }
            }
        }
    }
    private static int GetRepresentativeCloudSize(int x, int y, Dictionary<(int x, int y), int> cloudSizes)
    {
        if (cloudSizes.TryGetValue((x, y), out int size))
        {
            return size;
        }
        else
        {
            return 50; // Default value if not found
        }
    }
    private void MarkCloudPositions(int startX, int startY, CloudType[,] cloudData, Dictionary<(int x, int y), int> cloudSizes, int cloudSize)
    {
        Queue<(int x, int y)> queue = new();
        queue.Enqueue((startX, startY));
        HashSet<(int x, int y)> visited = new() { (startX, startY) };

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
    private int GetCloudTilesCount()
    {
        return cloudPositions.Count;
    }
    private void SmoothAndFluffCloudsPass(CloudType[,] tempCloudData)
    {
        List<(int startX, int startY, int endX, int endY)> regions = GetCloudRegions();
        object lockObj = new object();
        Random localRng = new Random(seed);

        Parallel.ForEach(regions, region =>
        {
            (int startX, int startY, int endX, int endY) = region;
            List<(int x, int y, CloudType neighborType)> localNewCloudPositions = new List<(int x, int y, CloudType neighborType)>();

            for (int x = startX; x < endX; x++)
            {
                for (int y = startY; y < endY; y++)
                {
                    if (cloudPositions.ContainsKey((x, y)))
                    {
                        // Check surrounding tiles instead of the cloud tiles themselves
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dy = -1; dy <= 1; dy++)
                            {
                                if (dx == 0 && dy == 0)
                                    continue;

                                int checkX = x + dx;
                                int checkY = y + dy;

                                if (checkX <= 0 || checkY <= 0 || checkX >= cloudDataWidth - 1 || checkY >= cloudDataHeight - 1)
                                    continue;

                                if (tempCloudData[checkX, checkY] == CloudType.None)
                                {
                                    int cloudNeighbors = 0;
                                    CloudType neighborType = CloudType.None;

                                    for (int ndx = -1; ndx <= 1; ndx++)
                                    {
                                        for (int ndy = -1; ndy <= 1; ndy++)
                                        {
                                            if (ndx == 0 && ndy == 0) continue;
                                            int neighborX = checkX + ndx;
                                            int neighborY = checkY + ndy;
                                            if (neighborX < 0 || neighborX >= cloudDataWidth || neighborY < 0 || neighborY >= cloudDataHeight)
                                                continue;
                                            if (tempCloudData[neighborX, neighborY] != CloudType.None)
                                            {
                                                cloudNeighbors++;
                                                neighborType = tempCloudData[neighborX, neighborY];
                                            }
                                        }
                                    }

                                    // Add new fluffy cloud pixels based on surrounding clouds
                                    if (cloudNeighbors >= 4 && cloudNeighbors <= 6 && localRng.NextDouble() > 0.7)
                                    {
                                        localNewCloudPositions.Add((checkX, checkY, neighborType));
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Update tempCloudData and cloudPositions
            lock (lockObj)
            {
                foreach ((int x, int y, CloudType neighborType) in localNewCloudPositions)
                {
                    tempCloudData[x, y] = neighborType;
                    cloudPositions[(x, y)] = (x, y);
                }
            }
        });
    }
    private void ApplyJellyEffect()
    {
        CloudType[,] newCloudData = new CloudType[cloudDataWidth, cloudDataHeight];
        Dictionary<(int, int), (double, double)> newCloudPositions = new Dictionary<(int, int), (double, double)>();

        List<(int startX, int startY, int endX, int endY)> regions = GetCloudRegions();

        object lockCloudData = new object();
        object lockCloudPositions = new object();

        Parallel.ForEach(regions, () => new Random(seed), (region, state, localRng) =>
        {
            (int startX, int startY, int endX, int endY) = region;
            Dictionary<(int, int), (double, double)> localPositions = new Dictionary<(int, int), (double, double)>();
            List<(int x, int y, CloudType value)> localCloudData = new List<(int x, int y, CloudType value)>();

            foreach (KeyValuePair<(int, int), (double, double)> kvp in cloudPositions)
            {
                (int intX, int intY) = kvp.Key;

                if (intX >= startX && intX < endX && intY >= startY && intY < endY)
                {
                    (double preciseX, double preciseY) = kvp.Value;

                    // Apply jelly effect only to edge tiles
                    if (IsCloudEdgeTile(intX, intY))
                    {
                        // Compute normal vector pointing outward from the cloud
                        double nx = 0;
                        double ny = 0;
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dy = -1; dy <= 1; dy++)
                            {
                                if (dx == 0 && dy == 0)
                                    continue;
                                int neighborX = intX + dx;
                                int neighborY = intY + dy;
                                if (neighborX >= 0 && neighborX < cloudDataWidth && neighborY >= 0 && neighborY < cloudDataHeight)
                                {
                                    if (cloudData[neighborX, neighborY] == CloudType.None) // Empty tile
                                    {
                                        nx += dx;
                                        ny += dy;
                                    }
                                }
                            }
                        }

                        double length = Math.Sqrt(nx * nx + ny * ny);
                        if (length > 0)
                        {
                            nx /= length;
                            ny /= length;

                            double jellyFactor = 0.22;
                            double displacement = (localRng.NextDouble() * 0.5 + 0.5) * jellyFactor; // Move outward

                            double newX = preciseX + nx * displacement;
                            double newY = preciseY + ny * displacement;

                            int intNewX = (int)Math.Round(newX);
                            int intNewY = (int)Math.Round(newY);

                            if (AreCoordsInBounds(intNewX, intNewY) && cloudData[intNewX, intNewY] == CloudType.None)
                            {
                                localCloudData.Add((intNewX, intNewY, cloudData[intX, intY]));
                                localPositions[(intNewX, intNewY)] = (newX, newY);
                            }
                            else
                            {
                                // Can't move, stay in place
                                localCloudData.Add((intX, intY, cloudData[intX, intY]));
                                localPositions[(intX, intY)] = (preciseX, preciseY);
                            }
                        }
                        else
                        {
                            // No outward direction, stay in place
                            localCloudData.Add((intX, intY, cloudData[intX, intY]));
                            localPositions[(intX, intY)] = (preciseX, preciseY);
                        }
                    }
                    else
                    {
                        localCloudData.Add((intX, intY, cloudData[intX, intY]));
                        localPositions[(intX, intY)] = (preciseX, preciseY);
                    }
                }
            }

            // Merge local results into shared data structures using locks
            lock (lockCloudData)
            {
                foreach ((int x, int y, CloudType value) in localCloudData)
                {
                    newCloudData[x, y] = value;
                }
            }

            lock (lockCloudPositions)
            {
                foreach (KeyValuePair<(int, int), (double, double)> kvp in localPositions)
                {
                    newCloudPositions[kvp.Key] = kvp.Value;
                }
            }

            return localRng;
        }, _ => { });

        cloudData = newCloudData;
        cloudPositions = newCloudPositions;
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
                    if (cloudData[nx, ny] == CloudType.None)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }
    private void FillCloudHoles()
    {
        CloudType[,] newCloudData = (CloudType[,])cloudData.Clone();
        HashSet<(int x, int y)> emptyAdjacentPositions = new HashSet<(int x, int y)>();

        // Collect all empty positions adjacent to cloud positions
        foreach ((int x, int y) in cloudPositions.Keys)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;

                    int nx = x + dx;
                    int ny = y + dy;

                    if (nx >= 0 && nx < cloudDataWidth && ny >= 0 && ny < cloudDataHeight)
                    {
                        if (cloudData[nx, ny] == CloudType.None)
                        {
                            emptyAdjacentPositions.Add((nx, ny));
                        }
                    }
                }
            }
        }

        foreach ((int x, int y) in emptyAdjacentPositions)
        {
            Dictionary<CloudType, int> surroundingTypes = new Dictionary<CloudType, int>();

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;

                    int nx = x + dx;
                    int ny = y + dy;

                    if (nx >= 0 && nx < cloudDataWidth && ny >= 0 && ny < cloudDataHeight)
                    {
                        CloudType neighborType = cloudData[nx, ny];
                        if (neighborType != CloudType.None)
                        {
                            if (!surroundingTypes.ContainsKey(neighborType))
                                surroundingTypes[neighborType] = 0;
                            surroundingTypes[neighborType]++;
                        }
                    }
                }
            }

            // Fill hole if surrounded by more than 6 cloud tiles of the same type
            if (surroundingTypes.Any())
            {
                KeyValuePair<CloudType, int> mostCommonType = surroundingTypes.OrderByDescending(kvp => kvp.Value).First();
                if (mostCommonType.Value >= 6)
                {
                    newCloudData[x, y] = mostCommonType.Key;
                }
            }
        }

        cloudData = newCloudData;
    }
    private int CountSurroundingClouds(int x, int y, HashSet<(int x, int y)> positions)
    {
        int count = 0;
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                if (positions.Contains((x + dx, y + dy)))
                {
                    count++;
                }
            }
        }
        return count;
    }
    private void RemoveEdgeClouds()
    {
        for (int x = 0; x < cloudDataWidth; x++)
        {
            for (int y = 0; y < cloudDataHeight; y++)
            {
                if (IsEdgeTile(x, y) && cloudData[x, y] != CloudType.None)
                {
                    cloudData[x, y] = CloudType.None;
                    cloudPositions.Remove((x, y));
                }
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
                if (ndx == 0 && ndy == 0)
                    continue;
                int nx = x + ndx;
                int ny = y + ndy;
                if (nx < 0 || ny < 0 || nx >= cloudDataWidth || ny >= cloudDataHeight)
                    continue;
                if (cloudData[nx, ny] != CloudType.None)
                    count++;
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
                if (ndx == 0 && ndy == 0)
                    continue;
                int nx = x + ndx;
                int ny = y + ndy;
                if (nx < 0 || ny < 0 || nx >= cloudDataWidth || ny >= cloudDataHeight)
                    continue;
                if (cloudData[nx, ny] == CloudType.None)
                    count++;
            }
        }
        return count;
    }
    private bool IsEdgeTile(int x, int y)
    {
        return x == 0 || y == 0 || x == cloudDataWidth - 1 || y == cloudDataHeight - 1;
    }
    private bool AreCoordsInBounds(int x, int y)
    {
        return x >= 0 && x < cloudDataWidth && y >= 0 && y < cloudDataHeight;
    }
    private void MergeNearbyClouds()
    {
        double mergeDistance = GetCloudTypeForCurrentWeather() switch
        {
            CloudType.Cirrus => 2.0,
            CloudType.Altocumulus => 6.0,
            CloudType.Cumulus => 5.0,
            CloudType.Cumulonimbus => 8.5,
            CloudType.Nimbostratus => 12.5,
            CloudType.Stratus => 2.5,
            _ => 5.0
        };
        Dictionary<(int, int), (double, double)> mergedClouds = new Dictionary<(int, int), (double, double)>();

        foreach (((int, int) pos, (double, double) precisePos) in cloudPositions)
        {
            bool merged = false;
            foreach (((int, int) otherPos, (double, double) otherPrecisePos) in mergedClouds)
            {
                if (Distance(pos.Item1, pos.Item2, otherPos.Item1, otherPos.Item2) < mergeDistance)
                {
                    // Merge cloud positions
                    double newX = (precisePos.Item1 + otherPrecisePos.Item1) / 2;
                    double newY = (precisePos.Item2 + otherPrecisePos.Item2) / 2;
                    mergedClouds[otherPos] = (newX, newY);
                    merged = true;
                    break;
                }
            }
            if (!merged)
            {
                mergedClouds[pos] = precisePos;
            }
        }

        cloudPositions = mergedClouds;
    }
    #endregion
    #region cloud regions
    private List<(int startX, int startY, int endX, int endY)> GetCloudRegions()
    {
        int regionsPerRow = 3;
        int regionWidth = cloudDataWidth / regionsPerRow;
        int regionHeight = cloudDataHeight / regionsPerRow;

        List<(int, int, int, int)> regions = new List<(int, int, int, int)>();

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
                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                {
                    yield return (nx, ny);
                }
            }
        }
    }
    private bool IsCloudSurrounded(int x, int y, int needed)
    {
        if (x < 0 || x >= cloudDataWidth || y < 0 || y >= cloudDataHeight)
            return false;

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
                        if (cloudData[nx, ny] == CloudType.None)
                        {
                            surroundingClouds++;
                        }
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
    public bool[,] cloudIsNight {get; set;}
    public bool[,] cloudIsDarkening {get; set;}
    private (int r, int g, int b) GetCloudColor(int x, int y)
    {
        (int mapX, int mapY) = CloudDataCordsToMapData(x, y);
        double intensity = GetDarkenedTileIntensity(mapX, mapY);

        if (intensity > 5)
        {
            cloudIsDarkening[mapX, mapY] = true;
        }
        if (intensity > 45)
        {
            cloudIsDarkening[mapX, mapY] = false;
            cloudIsNight[mapX, mapY] = true;
        }
        else if (intensity < 5 && intensity > 0)
        {
            cloudIsNight[mapX, mapY] = false;
        }

        int darkenedIntensity = cloudIsDarkening[mapX, mapY] ? (int)Math.Round(intensity) : 0;

        if (x < 0 || y < 0 || x >= cloudDataWidth || y >= cloudDataHeight)
        {
            return (255, 255, 255); // Default color for out-of-bounds
        }
        (int r, int g, int b) baseColor = GetCloudDepthColor(cloudData[x, y], cloudDepthData[x, y]);

        if (cloudIsNight[mapX, mapY])
        {
            baseColor.r = Math.Max(baseColor.r - 50 - darkenedIntensity, 0);
            baseColor.g = Math.Max(baseColor.g - 50 - darkenedIntensity, 0);
            baseColor.b = Math.Max(baseColor.b - 50 - darkenedIntensity, 0);
        }
        else if (cloudIsDarkening[mapX, mapY])
        {
            baseColor.r = Math.Max(baseColor.r - darkenedIntensity, 0);
            baseColor.g = Math.Max(baseColor.g - darkenedIntensity, 0);
            baseColor.b = Math.Max(baseColor.b - darkenedIntensity, 0);
        }

        return baseColor;
    }
    #endregion
    #region cloud state
    private void GiveCLoudsParameters()
    {
        foreach (Cloud cloud in clouds)
        {
            cloud.Speed = GetCloudSpeed(cloud.Type);
            cloud.Direction = GetCloudDirection(cloud.Type);
            cloud.Precipitation = GetCloudPrecipitation(cloud.Type);
        }
    }
    private double GetCloudSpeed(CloudType type)
    {
        return type switch
        {
            CloudType.Cirrus => 0.1 * GetAltitudeWindFactor(8000),
            CloudType.Altocumulus => 0.05 * GetAltitudeWindFactor(5000),
            CloudType.Cumulus => 0.1 * GetAltitudeWindFactor(2000),
            CloudType.Cumulonimbus => 0.2 * GetAltitudeWindFactor(1500),
            CloudType.Nimbostratus => 0.05 * GetAltitudeWindFactor(2500),
            CloudType.Stratus => 0.05 * GetAltitudeWindFactor(1000),
            _ => 0.1 * GetAltitudeWindFactor(2000)
        };
    }
    private double GetCloudDirection(CloudType type)
    {
        return weather.WindDirection;
    }
    private double GetCloudPrecipitation(CloudType type)
    {
        return type switch
        {
            CloudType.Cumulonimbus => 1.0 * weather.Humidity * weather.Temperature * rng.NextDouble() * 4,
            CloudType.Nimbostratus => 0.8 * weather.Humidity * weather.Temperature * rng.NextDouble() * 2,
            CloudType.Cumulus => 0.5 * weather.Humidity * weather.Temperature * rng.NextDouble(),
            CloudType.Altocumulus => 0.3 * weather.Humidity * weather.Temperature * rng.NextDouble(),
            CloudType.Cirrus => 0.1 * weather.Humidity * weather.Temperature * rng.NextDouble(),
            CloudType.Stratus => 0.2 * weather.Humidity * weather.Temperature * rng.NextDouble(),
            _ => 0.0
        };
    }
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
    public void RenderClouds(bool displayGUI = true)
    {
        int effectiveLeftPadding = displayGUI ? leftPadding : 0;
        int effectiveTopPadding = displayGUI ? topPadding : 2;
        CloudsDepth();

        // Remove clouds from previous positions that are no longer clouds
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                (int r, int g, int b) finalColor;
                TileId tile = mapData[x, y];
                if (darkenedPositionsIntensities.TryGetValue((x, y), out int darkBaseIntensity))
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
                (int cloudX, int cloudY) = MapDataCordsToCloudData(x, y);
                if (IsInCloudBounds(cloudX, cloudY))
                {
                    bool wasCloud = previousCloudData[cloudX, cloudY] != CloudType.None;
                    bool isCloud = cloudData[cloudX, cloudY] != CloudType.None;

                    if (wasCloud && !isCloud)
                    {
                        if (IsThereAnOverlayTile(x, y)) UpdateOverlayTile(x, y, displayGUI);
                        else
                        {
                            GUI.DrawPixel(effectiveLeftPadding + x * 2, y + effectiveTopPadding, finalColor.r, finalColor.g, finalColor.b, "  ");
                        }
                    }
                }
            }
        }

        // Render clouds at new positions
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {

                // Skip frame edges
                if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
                    continue;

                (int cloudX, int cloudY) = MapDataCordsToCloudData(x, y);
                if (IsInCloudBounds(cloudX, cloudY) && cloudData[cloudX, cloudY] != CloudType.None)
                {
                    _ = cloudDepthData[cloudX, cloudY];
                    //var cloudColor = GetCloudDepthColor(cloudType, depth);
                    (int r, int g, int b) cloudColor = GetCloudColor(cloudX, cloudY);
                    GUI.DrawPixel(effectiveLeftPadding + x * 2, y + effectiveTopPadding, cloudColor.r, cloudColor.g, cloudColor.b, "  ");
                }
            }
        }

        // Update previous cloud data
        Array.Copy(cloudData, previousCloudData, cloudData.Length);
    }
    public HashSet<(int x, int y)> previousShadowPositions {get; set;} = new HashSet<(int x, int y)>();
    public readonly Dictionary<(int x, int y), double> currentShadowPositions = new();
    public readonly int shadowRadius = 3;
    public static double shadowIntensityFactor {get; set;}
    public void DisplayCloudShadows(bool displayGUI = true)
    {
        int effectiveLeftPadding = displayGUI ? leftPadding : 0;
        int effectiveTopPadding = displayGUI ? topPadding : 2;
        if (conf.DisplayShadows)
        {
            currentShadowPositions.Clear();
            // Calculate shadow positions and intensities
            for (int x = 0; x < cloudDataWidth; x++)
            {
                for (int y = 0; y < cloudDataHeight; y++)
                {
                    if (cloudData[x, y] != CloudType.None)
                    {
                        (int mapX, int mapY) = CloudToMapCoordinates(x, y);
                        int shadowX = mapX + cloudShadowOffsetX;
                        int shadowY = mapY + cloudShadowOffsetY;

                        // Add shadow with intensity falloff
                        for (int dx = -shadowRadius; dx <= shadowRadius; dx++)
                        {
                            for (int dy = -shadowRadius; dy <= shadowRadius; dy++)
                            {
                                int smoothX = shadowX + dx;
                                int smoothY = shadowY + dy;

                                if (smoothX > 0 && smoothX < width - 1 && smoothY > 0 && smoothY < height - 1)
                                {
                                    double distance = Math.Sqrt(dx * dx + dy * dy);
                                    if (distance <= shadowRadius)
                                    {
                                        (int x, int y) pos = (x: smoothX, y: smoothY);
                                        double intensity = 1.0 - (distance / shadowRadius);

                                        if (intensity > 0)
                                        {
                                            if (!currentShadowPositions.ContainsKey(pos))
                                                currentShadowPositions[pos] = intensity;
                                            else
                                                currentShadowPositions[pos] = Math.Max(currentShadowPositions[pos], intensity);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Clear old shadows
            if (shadowIntensityFactor > 0)
            {
                foreach ((int x, int y) pos in previousShadowPositions)
                {
                    if (!currentShadowPositions.ContainsKey(pos) || (isCloudsRendering && IsTileUnderCloud(pos.x, pos.y)))
                    {
                        UpdateTile(pos.x, pos.y, displayGUI);
                        UpdateOverlayTile(pos.x, pos.y, displayGUI);
                    }
                }
            }

            // Draw new shadows with smooth intensity
            foreach (KeyValuePair<(int x, int y), double> pair in currentShadowPositions)
            {
                (int x, int y) pos = pair.Key;
                double intensity = pair.Value;

                if (intensity > 0 && pos.x > 0 && pos.x < width - 1 && pos.y > 0 && pos.y < height - 1)
                {
                    bool isUnderCloud = IsTileUnderCloud(pos.x, pos.y);

                    if (((isCloudsRendering && !isUnderCloud) || !isCloudsRendering) && !IsThereAWaveTile(pos.x, pos.y))
                    {
                        (int r, int g, int b) baseColor = IsTileDarkened(pos.x, pos.y) ? GetDarkenedColor(pos.x, pos.y) : GetColor(mapData[pos.x, pos.y], pos.x, pos.y);
                        int shadowFactor = (int)(shadowIntensityFactor * intensity);

                        int r = Math.Max(0, baseColor.r - shadowFactor);
                        int g = Math.Max(0, baseColor.g - shadowFactor);
                        int b = Math.Max(0, baseColor.b - shadowFactor);

                        if (shadowFactor > 0)
                        {
                            GUI.SetCursorPosition(effectiveLeftPadding + pos.x * 2, pos.y + effectiveTopPadding);
                            if (IsThereAnOverlayTile(pos.x, pos.y))
                            {
                                (int r, int g, int b) overlayColor = GetOverlayColor(overlayData[pos.x, pos.y]);

                                // Apply shadow intensity to overlay color as well
                                int or = Math.Max(0, overlayColor.r - shadowFactor);
                                int og = Math.Max(0, overlayColor.g - shadowFactor);
                                int ob = Math.Max(0, overlayColor.b - shadowFactor);

                                string background = GUI.SetBackgroundColor(r, g, b);
                                string foreground = GUI.SetForegroundColor(or, og, ob);
                                GUI.Write(background + foreground + GetSpeciesIcon(overlayData[pos.x, pos.y]) + GUI.ResetColor());
                            }
                            else
                            {
                                GUI.Write(GUI.SetBackgroundColor(r, g, b) + "  " + GUI.ResetColor());
                            }
                        }
                    }
                    else if (((isCloudsRendering && !isUnderCloud) || !isCloudsRendering) && IsThereAWaveTile(pos.x, pos.y))
                    {
                        (int r, int g, int b) baseColor = IsTileDarkened(pos.x, pos.y) ? GetDarkenedColor(pos.x, pos.y) : GetWaveColor(pos.x, pos.y);
                        int shadowFactor = (int)(shadowIntensityFactor * intensity);

                        int r = Math.Max(0, baseColor.r - shadowFactor);
                        int g = Math.Max(0, baseColor.g - shadowFactor);
                        int b = Math.Max(0, baseColor.b - shadowFactor);

                        if (shadowFactor > 20 && !IsTileUnderCloud(pos.x, pos.y))
                        {
                            GUI.SetCursorPosition(effectiveLeftPadding + pos.x * 2, pos.y + effectiveTopPadding);
                            if (IsThereAnOverlayTile(pos.x, pos.y))
                            {
                                (int r, int g, int b) overlayColor = GetOverlayColor(overlayData[pos.x, pos.y]);

                                // Apply shadow intensity to overlay color as well
                                int or = Math.Max(0, overlayColor.r - shadowFactor);
                                int og = Math.Max(0, overlayColor.g - shadowFactor);
                                int ob = Math.Max(0, overlayColor.b - shadowFactor);

                                string background = GUI.SetBackgroundColor(r, g, b);
                                string foreground = GUI.SetForegroundColor(or, og, ob);
                                GUI.Write(background + foreground + GetSpeciesIcon(overlayData[pos.x, pos.y]) + GUI.ResetColor());
                            }
                            else
                            {
                                GUI.Write(GUI.SetBackgroundColor(r, g, b) + "  " + GUI.ResetColor());
                            }
                        }
                    }
                }
            }

            // Update previous shadow positions, excluding positions under clouds
            previousShadowPositions = new HashSet<(int x, int y)>(
                currentShadowPositions.Keys.Where(pos => !isCloudsRendering || !IsTileUnderCloud(pos.x, pos.y))
            );
        }
    }
    private (int r, int g, int b) GetShadowColor(int x, int y)
    {
        (int r, int g, int b) baseColor = GetColor(mapData[x, y], x, y);
    
        // Convert map coordinates to cloud shadow coordinates (taking into account the offset)
        int shadowX = x + cloudShadowOffsetX;
        int shadowY = y + cloudShadowOffsetY;
        double shadowIntensity = 0.0;
    
        // Find current shadow positions with their intensities from the cloud data
        for (int dx = -3; dx <= 3; dx++)
        {
            for (int dy = -3; dy <= 3; dy++)
            {
                int nx = shadowX + dx;
                int ny = shadowY + dy;
    
                if (nx >= 0 && nx < cloudDataWidth && ny >= 0 && ny < cloudDataHeight)
                {
                    if (cloudData[nx, ny] != CloudType.None)
                    {
                        double distance = Math.Sqrt(dx * dx + dy * dy);
                        if (distance <= 3)
                        {
                            double intensity = 1.0 - (distance / 3);
                            shadowIntensity = Math.Max(shadowIntensity, intensity);
                        }
                    }
                }
            }
        }
    
        // Calculate shadow factor based on actual shadow intensity
        int shadowFactor = (int)(shadowIntensityFactor * shadowIntensity);
    
        int r = Math.Max(0, baseColor.r - shadowFactor);
        int g = Math.Max(0, baseColor.g - shadowFactor);
        int b = Math.Max(0, baseColor.b - shadowFactor);
    
        return (r, g, b);
    }
    public static double timeOfDay {get; set;}
    public static double season {get; set;}
    private void UpdateTime()
    {
        timeOfDay = weather.TimeOfDay;
    }
    private void UpdateSeason()
    {
        season = weather.Season;
    }
    private void UpdateCloudShadows()
    {
        // Calculate base shadow offset based on time of day
        double angle = ((timeOfDay - 6.0) / 24.0) * 2 * Math.PI; // Shift timeOfDay by 6 hours
        int baseOffsetX = (int)(-Math.Cos(angle) * 12); // Invert cosine for desired shadow offset
        int baseOffsetY = Math.Abs((int)(Math.Sin(angle) * 12)); // Ensure y offset is always positive

        // Apply seasonal variation
        double seasonalVariation = Math.Sin((season / 4.0) * 2 * Math.PI) * 2; // Adjust the multiplier as needed
        baseOffsetX += (int)seasonalVariation;

        // Ensure the shadow is always on a higher y-coordinate than the cloud itself
        baseOffsetY = Math.Max(baseOffsetY, 3);

        // Update global shadow offset variables
        cloudShadowOffsetX = baseOffsetX;
        cloudShadowOffsetY = baseOffsetY;

        double peakShadowIntensity = 30.0;
        // Adjust shadow intensity factor based on time of day
        shadowIntensityFactor = GetShadowIntensityFactor(timeOfDay, sunriseTime, sunsetTime, peakShadowIntensity);
    }
    private static double GetShadowIntensityFactor(double timeOfDay, double sunriseTime, double sunsetTime, double peakShadowIntensity)
    {
        double adjustedSunriseTime = sunriseTime + 2.0;
        double adjustedSunsetTime = sunsetTime - 1.0;

        if (timeOfDay < adjustedSunriseTime || timeOfDay > adjustedSunsetTime)
        {
            return 0.0;
        }

        double noonTime = (adjustedSunriseTime + adjustedSunsetTime) / 2.0;
        double morningDuration = noonTime - adjustedSunriseTime;
        double eveningDuration = adjustedSunsetTime - noonTime;

        if (timeOfDay <= noonTime)
        {
            // Morning: smoothly transition from 0 to peakShadowIntensity
            return peakShadowIntensity * (timeOfDay - adjustedSunriseTime) / morningDuration;
        }
        else
        {
            // Afternoon: smoothly transition from peakShadowIntensity to 0
            return peakShadowIntensity * (adjustedSunsetTime - timeOfDay) / eveningDuration;
        }
    }
    public bool IsThereACloudShadow(int x, int y)
    {
        if (currentShadowPositions.ContainsKey((x, y)))
        {
            return true;
        }
        return false;
    }
    private bool AreCloudCoordsInMapDataBounds(int x, int y)
    {
        return x >= 0 && y >= 0 && x < width && y < height;
    }
    private CloudType GetCloudType(char cloudChar)
    {
        return cloudChar switch
        {
            '1' => CloudType.Cirrus,
            '2' => CloudType.Altocumulus,
            '3' => CloudType.Cumulus,
            '4' => CloudType.Cumulonimbus,
            '5' => CloudType.Nimbostratus,
            '6' => CloudType.Stratus,
            _ => CloudType.Cumulus
        };
    }
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

        List<(int x, int y)> targetPoints = new List<(int x, int y)>();

        int maxPoints = rng.Next(minMaxPoints, maxMaxPoints + 1);
        // Max attepmpts to find a valid target point
        int maxAttempts = 100;
        int attempts = 0;
        for (int i = 0; i < maxPoints; i++)
        {
            (int x, int y) target = FindValidCloudTargetPoint((startX, startY), minRadius, maxRadius);
            if (target != (-1, -1))
            {
                targetPoints.Add(target);
            }
            else
            {
                i--;
            }

            attempts++;
            if (attempts >= maxAttempts)
            {
                break;
            }
        }

        GenerateCloudPath(type, (startX, startY), targetPoints);
        AddImperfectionsOnCloudEdges(cloudData, type);
        CreateFluffyCloudEdges(cloudData, type);
        SmoothCloudShape(cloudData, type);
    }
    private (int x, int y) FindValidCloudTargetPoint((int x, int y) startPoint, int minRadius, int maxRadius)
    {
        Random rng = new Random(seed);
        for (int attempts = 0; attempts < 100; attempts++)
        {
            int radius = rng.Next(minRadius, maxRadius + 1);
            double angle = rng.NextDouble() * 2 * Math.PI;
            int x = startPoint.x + (int)(radius * Math.Cos(angle));
            int y = startPoint.y + (int)(radius * Math.Sin(angle));

            if (IsValidCloudTargetPoint(x, y))
            {
                return (x, y);
            }
        }
        return (-1, -1);
    }
    private bool IsValidCloudTargetPoint(int x, int y)
    {
        return x >= 0 && y >= 0 && x < cloudDataWidth && y < cloudDataHeight;
    }
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
                            if (cloudData[x + dx, y + dy] != CloudType.None)
                            {
                                cloudDensity++;
                            }
                        }
                    }

                    if (cloudDensity < 4)
                    {
                        if (rng.NextDouble() > 0.5)
                        {
                            cloudData[x, y] = CloudType.None;
                        }
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

                if (nx >= 0 && nx < cloudDataWidth && ny >= 0 && ny < cloudDataHeight && i * i + j * j <= radius * radius)
                {
                    cloudData[nx, ny] = type;
                }
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
                            if (tempCloudData[x + dx, y + dy] == type)
                            {
                                cloudDensity++;
                            }
                        }
                    }

                    if (cloudDensity < 6)
                    {
                        if (rng.NextDouble() > 0.5)
                        {
                            tempCloudData[x, y] = CloudType.None;
                        }
                    }
                    else if (cloudDensity > 8)
                    {
                        if (rng.NextDouble() > 0.3)
                        {
                            tempCloudData[x, y] = type;
                        }
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
                            if (tempCloudData[x + dx, y + dy] == type)
                            {
                                cloudDensity++;
                            }
                        }
                    }

                    if (cloudDensity < 4)
                    {
                        tempCloudData[x, y] = CloudType.None;
                    }
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
                if (edgeFound)
                    break;
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
                if (cloudData[x, y] != CloudType.None)
                {
                    cloudDepthData[x, y] = CalculateCloudDepth(x, y, cloudData[x, y]);
                }
                else
                {
                    cloudDepthData[x, y] = 0;
                }
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
                        if (cloudData[x + dx, y + dy] != CloudType.None)
                            filledNeighbors++;
                    }
                }

                // Apply smoothing rules
                if (filledNeighbors < 2 || filledNeighbors > 6)
                {
                    tempCloudData[x, y] = CloudType.None;
                }
                else
                {
                    // Optionally, enhance fluffiness by adding more filled neighbors
                    if (filledNeighbors > 4 && cloudData[x, y] == CloudType.None)
                    {
                        tempCloudData[x, y] = cloudData[x, y];
                    }
                }
            }
        }

        // Update cloud data with smoothed data
        cloudData = tempCloudData;
    }
    private void SpawnCumulusCloud(int x, int y)
    {
        GenerateCloudCluster(x, y, CloudType.Cumulus);
    }
    private void SpawnCirrusCloud(int x, int y)
    {
        GenerateCloudCluster(x, y, CloudType.Cirrus);
    }
    private void SpawnCumulonimbusCloud(int x, int y)
    {
        GenerateCloudCluster(x, y, CloudType.Cumulonimbus);
    }
    private void SpawnNimbostratusCloud(int x, int y)
    {
        GenerateCloudCluster(x, y, CloudType.Nimbostratus);
    }
    private void SpawnAltocumulusCloud(int x, int y)
    {
        GenerateCloudCluster(x, y, CloudType.Altocumulus);
    }
    private void SpawnStratusCloud(int x, int y)
    {
        GenerateCloudCluster(x, y, CloudType.Stratus);
    }
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
            if (!IsInCloudBounds(cx, cy))
                return false;
        }
        return true;
    }
    private (int x, int y) MapDataCordsToCloudData(int x, int y)
    {
        return (x + cloudDataOffsetX, y + cloudDataOffsetY);
    }
    private (int x, int y) CloudDataCordsToMapData(int x, int y)
    {
        return (x - cloudDataOffsetX, y - cloudDataOffsetY);
    }
    private (int x, int y) CloudToMapCoordinates(int cloudX, int cloudY)
    {
        return (cloudX - cloudDataOffsetX, cloudY - cloudDataOffsetY);
    }
    private bool IsInCloudBounds(int x, int y)
    {
        return x >= 0 && x < cloudDataWidth && y >= 0 && y < cloudDataHeight;
    }
    #endregion
}
