using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Internal;
public class Map
{
    private static bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    public static List<string> outputBuffer = new List<string>();
    public List<string> actualOutputBuffer = new List<string>();
    public static List<string> eventBuffer = new List<string>();
    public static int cloudShadowOffsetX;
    public static int cloudShadowOffsetY;            
    private static double sunriseTime;
    private static double sunsetTime;
    public static double time;
    public bool isCloudsRendering, isCloudsShadowsRendering;
    public bool shouldSimulationContinue = true;
    private HashSet<(int x, int y)> tilesToUpdate = new HashSet<(int x, int y)>();
    #region map parameters
    public int width;
    public int height;
    private int cloudDataWidth;
    private int cloudDataHeight;
    private int cloudDataOffsetX;
    private int cloudDataOffsetY;
    public char[,] mapData;
    public char[,] overlayData;
    public char[,] previousOverlayData;
    public char[,] previousMapData;
    private char[,] cloudData;
    private char[,] previousCloudData;
    private int[,] cloudDepthData;
    private double[,] precipitationData;
    private double[,] previousPrecipitationData;
    private Random rngMap;
    public Random rng;
    private double noiseScale;
    private double erosionFactor;
    private int minBiomeSize;
    private int minLakeSize;
    private int minRiverWidth;
    private int maxRiverWidth;
    private double riverFlowChance;
    private double plainsHeightThreshold;
    private double forestHeightThreshold;
    private double mountainHeightThreshold;
    private const double waterAnimationSpeed = 0.1;
    private double[,] noise;
    private double[,] tempatureNoise;
    private double[,] humidityNoise;
    private int[,] temperatureData;
    private int[,] humidityData;
    private double avarageTempature, avarageHumidity;
    private int topPadding;
    private int bottomPadding;
    private int leftPadding;
    private int rightPadding;
    private static int seed;
    private static Config conf = new Config(Console.WindowWidth / 2 - GUIConfig.LeftPadding - GUIConfig.RightPadding, Console.WindowHeight - GUIConfig.BottomPadding - GUIConfig.TopPadding, 
        10.0, 0.2, 12, 16, 7, 10, 0.4, 0.7, 0.4, 1.0, new Random().Next().ToString());

    public Map(Config config)
    {
        seed = config.Seed;
        rng = new Random(seed);
        conf = config;
        topPadding = GUIConfig.TopPadding;
        bottomPadding = GUIConfig.BottomPadding;
        leftPadding = GUIConfig.LeftPadding;
        rightPadding = GUIConfig.RightPadding;
        width = config.Width;
        height = config.Height;
        noiseScale = config.NoiseScale;
        erosionFactor = config.ErosionFactor;
        minBiomeSize = config.MinBiomeSize;
        minLakeSize = config.MinLakeSize;
        minRiverWidth = config.MinRiverWidth;
        maxRiverWidth = config.MaxRiverWidth;
        riverFlowChance = config.RiverFlowChance;
        plainsHeightThreshold = config.PlainsHeightThreshold;
        forestHeightThreshold = config.ForestHeightThreshold;
        mountainHeightThreshold = config.MountainHeightThreshold;
        cloudDataWidth = conf.Width * 3;
        cloudDataHeight = conf.Height * 3;
        cloudDataOffsetX = conf.Width;
        cloudDataOffsetY = conf.Height;
        mapData = new char[width, height];
        previousMapData = new char[width, height];
        overlayData = new char[width, height];
        previousOverlayData = new char[width, height];
        cloudData = new char[cloudDataWidth, cloudDataHeight];
        previousCloudData = new char[cloudDataWidth, cloudDataHeight];
        cloudDepthData = new int[cloudDataWidth, cloudDataHeight];
        precipitationData = new double[cloudDataWidth, cloudDataHeight];
        previousPrecipitationData = new double[cloudDataWidth, cloudDataHeight];
        temperatureData = new int[width, height];
        humidityData = new int[width, height];
        rngMap = new Random(seed); // Initialize with a seed
        noise = Perlin.GeneratePerlinNoise(width, height, noiseScale, rng.Next());
        tempatureNoise = Perlin.GeneratePerlinNoise(width, height, noiseScale * 25, rng.Next());
        humidityNoise = Perlin.GeneratePerlinNoise(width, height, noiseScale * 12, rng.Next());
        cloudIsNight = new bool[width, height];
        cloudIsDarkening = new bool[width, height];
    }
    #endregion
    bool debug = false;
    public void Generate()
    {
        SetAvarageTempatureHumidity();
        if (debug) Console.WriteLine(1);
        AssignTempAndHumData();
        if (debug) Console.WriteLine(2);
        SmoothOutTempatureHumidity();
        if (debug) Console.WriteLine(3);
        AssignBiomes(noise);
        if (debug) Console.WriteLine(4);
        EnsureMinimumBiomeSize();
        if (debug) Console.WriteLine(5);
        ReplaceBiome('M', 'P');
        if (debug) Console.WriteLine(6);
        CreateMountains();
        if (debug) Console.WriteLine(7);
        CreateRiver();
        if (debug) Console.WriteLine(8);
        CreateLakes();
        if (debug) Console.WriteLine(9);
        CreateComplexFrame();
        if (debug) Console.WriteLine(10);
        SingleTileCheckPF('P', 'F', 2);
        if (debug) Console.WriteLine(11);
        CreateBeaches();
        if (debug) Console.WriteLine(12);
        //CreateStreams();
        WaterDepth();
        if (debug) Console.WriteLine(13);
        FrameMap('@');
        if (debug) Console.WriteLine(14);
        InitializeSpecies(3, 6, new Crab(0, 0, 0, 0, mapData, seed));
        if (debug) Console.WriteLine(15);
        InitializeSpecies(2, 4, new Turtle(0, 0, 0, 0, mapData, overlayData, width, height, seed));
        if (debug) Console.WriteLine(16);
        InitializeCows(1, 2, 2, 4);
        if (debug) Console.WriteLine(17);
        InitializeSheeps(1, 2, 1, 3);
        if (debug) Console.WriteLine(18);
        InitializeWaves();
        if (debug) Console.WriteLine(19);
        InitializeWeather();
        if (debug) Console.WriteLine(20);
        InitializeClouds();
    }
    #region essential functions
    private void AssignBiomes(double[,] noise)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (noise[x, y] < forestHeightThreshold)
                {
                    mapData[x, y] = 'F'; // Forest
                }
                else if (noise[x, y] < plainsHeightThreshold)
                {
                    mapData[x, y] = 'P'; // Plains
                }
                else if (noise[x, y] < mountainHeightThreshold)
                {
                    mapData[x, y] = 'M'; // Mountain
                }
                else
                {
                    mapData[x, y] = ' '; // Empty
                }
            }
        }
    }
    private void EnsureMinimumBiomeSize()
    {
        // Implement logic to ensure biomes are at least minBiomeSize x minBiomeSize in size
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int biomeSize = GetBiomeSize(x, y);
                if (biomeSize < minBiomeSize * minBiomeSize)
                {
                    ExpandBiome(x, y);
                }
            }
        }
    }
    private int GetBiomeSize(int startX, int startY)
    {
        char biomeType = mapData[startX, startY];
        bool[,] visited = new bool[width, height];
        return FloodFill(startX, startY, biomeType, visited);
    }
    private int FloodFill(int x, int y, char biomeType, bool[,] visited)
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
        char biomeType = mapData[startX, startY];
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
    double totalTemp, totalHum;
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
        for (int a = 0; a < 5; a++)
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
    #region useful functions
    private (int, int) GetRandomPoint()
    {
        int x = rngMap.Next(0, width);
        int y = rngMap.Next(0, height);
        return (x, y);
    }
    private (int, int) GetRandomPointInRange(int startX, int startY, int minDistance, int maxDistance)
    {
        int endX, endY;
        do
        {
            endX = rngMap.Next(startX - maxDistance, startX + maxDistance + 1);
            endY = rngMap.Next(startY - maxDistance, startY + maxDistance + 1);
        } while (Math.Sqrt(Math.Pow(endX - startX, 2) + Math.Pow(endY - startY, 2)) < minDistance);
        return (endX, endY);
    }
    public (int, int) GetRandomPointInBiome(char biome)
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

        int randomIndex = rngMap.Next(biomePoints.Count);
        if (biomePoints.Count > 0)
        {
            return biomePoints[randomIndex];
        }
        return (-1, -1);
    }
    private bool IsInBiome(int x, int y, char biome)
    {
        return x >= 0 && x < width && y >= 0 && y < height && mapData[x, y] == biome;
    }
    private double GetDistance(int x1, int y1, int x2, int y2)
    {
        return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
    }
    private int CountSurroundingBiomes(int x, int y, char biome)
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
    private void ReplaceBiome(char oldBiome, char newBiome)
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
    private double[,] GeneratePerlinNoiseMap(int width, int height)
    {
        double[,] noiseMap = new double[width, height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int sampleX = x;
                int sampleY = y;
                double noiseValue = Perlin.GeneratePerlinNoise(width, height, noiseScale, rng.Next())[x, y];
                noiseValue = noiseMap[x, y];
                noiseMap[x, y] = noiseValue;
            }
        }
        return noiseMap;
    }
    private void SmoothMap(int smoothFactor)
    {
        for (int i = 0; i < smoothFactor; i++)
        {
            for (int y = 1; y < conf.Height - 1; y++)
            {
                for (int x = 1; x < conf.Width - 1; x++)
                {
                    double sum = 0;
                    int count = 0;
                    for (int ny = -1; ny <= 1; ny++)
                    {
                        for (int nx = -1; nx <= 1; nx++)
                        {
                            sum += mapData[x + nx, y + ny];
                            count++;
                        }
                    }
                    mapData[x, y] = (char)(sum / count);
                }
            }
        }
    }
    private double Heuristic(int x1, int y1, int x2, int y2)
    {
        return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
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
    private void SpreadTile(int startX, int startY, double spreadChance, int minSpread, int maxSpread)
    {
        char tile = mapData[startX, startY];
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
    public (int, int) GetMapCenter()
    {
        int centerX = width / 2;
        int centerY = height / 2;

        // Adjust for even dimensions
        if (width % 2 == 0) centerX -= 1;
        if (height % 2 == 0) centerY -= 1;

        return (centerX, centerY);
    }
    private void FillCircle(int x, int y, char tile, int minRadius, int maxRadius)
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
    {
        if (mapData[x, y] == 'O')
        {
            return true;
        }
        return false;
    }
    #endregion
    // Map Frame
    #region map frame
    private void FrameMapParams(int frameWidth, int frameHeight)
    {
        (int centerX, int centerY) = GetMapCenter();
        int startX = Math.Max(0, centerX - frameWidth / 2);
        int endX = Math.Min(width - 1, centerX + frameWidth / 2);
        int startY = Math.Max(0, centerY - frameHeight / 2);
        int endY = Math.Min(height - 1, centerY + frameHeight / 2);

        for (int y = startY; y <= endY; y++)
        {
            for (int x = startX; x <= endX; x++)
            {
                if (x == startX || x == endX || y == startY || y == endY)
                {
                    mapData[x, y] = 'O';
                }
            }
        }
    }
    public void FrameMap(char frameChar)
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

        List<(int x, int y)> startingPoints = new List<(int x, int y)>
        {
            GetRandomPointOnPath(GetPath(topLeft, bottomRight), 10, 55),
            GetRandomPointOnPath(GetPath(topRight, bottomLeft), 10, 55),
            GetRandomPointOnPath(GetPath(bottomRight, topLeft), 10, 55),
            GetRandomPointOnPath(GetPath(bottomLeft, topRight), 10, 55)
        };

        List<(int x, int y)> points = new List<(int x, int y)>(startingPoints);

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
                if (mapData[x, y] == '@')
                {
                    FillCircle(x, y, 'O', 2, 4);
                }
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

        Queue<(int, int)> queue = new Queue<(int, int)>();
        queue.Enqueue((startX, startY));
        bool[,] visited = new bool[width, height];
        visited[startX, startY] = true;

        while (queue.Count > 0)
        {
            (int x, int y) = queue.Dequeue();
            mapData[x, y] = 'O';

            foreach ((int nx, int ny) in GetNeighbors(x, y))
            {
                if (nx >= 0 && nx < width && ny >= 0 && ny < height && !visited[nx, ny])
                {
                    if (!IsOceanTile(nx, ny))
                    {
                        queue.Enqueue((nx, ny));
                    }
                    visited[nx, ny] = true;
                }
            }
        }
    }
    private (int x, int y) GetRandomPointOnPath(List<(int x, int y)> path, int minRange, int maxRange)
    {
        Random rng = new Random(seed);
        int index = rng.Next(minRange, Math.Min(maxRange, path.Count));
        return path[index];
    }
    private List<(int x, int y)> GenerateRandomPointsOnLineWithDistance((int x, int y) start, (int x, int y) end, int minRange, int maxRange, int minDistanceFromStart, int maxAdditionalPoints)
    {
        List<(int x, int y)> points = new List<(int x, int y)>();
        List<(int x, int y)> path = GetPath(start, end);
        Random rng = new Random(seed);
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
        List<(int x, int y)> remainingPoints = new List<(int x, int y)>(points);
        List<(int x, int y)> connectedPoints = new List<(int x, int y)> { remainingPoints[0] };
        remainingPoints.RemoveAt(0);

        while (remainingPoints.Count > 0)
        {
            (int x, int y) lastPoint = connectedPoints[connectedPoints.Count - 1];
            (int x, int y) nearestPoint = remainingPoints.OrderBy(p => GetDistance(lastPoint.x, lastPoint.y, p.x, p.y)).First();
            connectedPoints.Add(nearestPoint);
            remainingPoints.Remove(nearestPoint);
        }

        // Connect the points in order
        for (int i = 0; i < connectedPoints.Count - 1; i++)
        {
            ConnectPoints(connectedPoints[i], connectedPoints[i + 1]);
        }
        // Connect the last point to the first to close the loop
        ConnectPoints(connectedPoints[connectedPoints.Count - 1], connectedPoints[0]);
    }
    private List<(int x, int y)> GenerateRandomPointsOnLine((int x, int y) start, (int x, int y) end, int minRange, int maxRange, int minDistanceFromStart)
    {
        List<(int x, int y)> points = new List<(int x, int y)>();
        List<(int x, int y)> path = GetPath(start, end);
        Random rng = new Random(seed);

        for (int i = minRange; i < path.Count - minRange; i++)
        {
            if (rng.NextDouble() < 0.1) // Small chance to generate a point
            {
                (int x, int y) point = path[i];
                if (GetDistance(start.x, start.y, point.x, point.y) >= minDistanceFromStart && GetDistance(end.x, end.y, point.x, point.y) >= minDistanceFromStart)
                {
                    points.Add(point);
                }
            }
        }

        return points;
    }
    private char GetMostSurroundingBiome(int x, int y)
    {
        Dictionary<char, int> biomeCounts = new Dictionary<char, int>();
        foreach ((int nx, int ny) in GetNeighbors(x, y))
        {
            char biome = mapData[nx, ny];
            if (biomeCounts.ContainsKey(biome))
            {
                biomeCounts[biome]++;
            }
            else
            {
                biomeCounts[biome] = 1;
            }
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
                if (mapData[x, y] == 'P' || mapData[x, y] == 'F')
                {
                    int surroundingWater = CountSurroundingBiomes(x, y, 'O') + CountSurroundingBiomes(x, y, 'o');
                    int surroundingPlains = CountSurroundingBiomes(x, y, 'P');
                    int surroundingForest = CountSurroundingBiomes(x, y, 'F');

                    if (surroundingWater > surroundingPlains + surroundingForest)
                    {
                        mapData[x, y] = 'O';
                    }
                    else if (surroundingForest > surroundingPlains)
                    {
                        mapData[x, y] = 'F'; // Preserve forest
                    }
                    else if (surroundingPlains > surroundingForest)
                    {
                        mapData[x, y] = 'P'; // Preserve plains
                    }
                }
            }
        }

        // Second pass: Advanced smoothing to create rounded edges
        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                if (mapData[x, y] == 'P' || mapData[x, y] == 'F')
                {
                    int surroundingWater = CountSurroundingBiomes(x, y, 'O') + CountSurroundingBiomes(x, y, 'V');
                    int surroundingPlains = CountSurroundingBiomes(x, y, 'P');
                    int surroundingForest = CountSurroundingBiomes(x, y, 'F');

                    if (surroundingWater > surroundingPlains + surroundingForest)
                    {
                        mapData[x, y] = 'O';
                    }
                    else if (surroundingForest > surroundingPlains)
                    {
                        mapData[x, y] = 'F'; // Preserve forest
                    }
                    else if (surroundingPlains > surroundingForest)
                    {
                        mapData[x, y] = 'P'; // Preserve plains
                    }
                }
            }
        }

        // Third pass: Final smoothing to ensure consistency
        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                if (mapData[x, y] == 'P' || mapData[x, y] == 'F')
                {
                    int surroundingWater = CountSurroundingBiomes(x, y, 'O') + CountSurroundingBiomes(x, y, 'V');
                    int surroundingPlains = CountSurroundingBiomes(x, y, 'P');
                    int surroundingForest = CountSurroundingBiomes(x, y, 'F');

                    if (surroundingWater > surroundingPlains + surroundingForest)
                    {
                        mapData[x, y] = 'O';
                    }
                    else if (surroundingForest > surroundingPlains)
                    {
                        mapData[x, y] = 'F'; // Preserve forest
                    }
                    else if (surroundingPlains > surroundingForest)
                    {
                        mapData[x, y] = 'P'; // Preserve plains
                    }
                }
            }
        }
    }
    private List<(int x, int y)> GetPath((int x, int y) start, (int x, int y) end)
    {
        List<(int x, int y)> path = new List<(int x, int y)>();
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
        foreach ((int x, int y) point in path)
        {
            if (point.x >= 0 && point.x < width && point.y >= 0 && point.y < height)
            {
                mapData[point.x, point.y] = '@';
            }
        }
    }
    #endregion
    // Artifacts
    #region mountain functions
    private void CreateMountains()
    {
        if (rngMap.NextDouble() < 1)
        {
            GenerateMountainRanges();
            if (debug) Console.WriteLine("Generated mountain ranges");
            MountainDepth();
            if (debug) Console.WriteLine("Added mountain depth");
            ErodeMountainRanges();
            if (debug) Console.WriteLine("Eroded mountain ranges");
            GenerateSnowPeaks();
            if (debug) Console.WriteLine("Generated snow peaks");
            DeleteBadSnowPeaks();
            if (debug) Console.WriteLine("Deleted bad snow peaks");
        }
    }
    private void GenerateMountainRanges()
    {
        int maxMountains = 2; // Maximum number of mountain ranges to generate
        int maxAdditionalMountains = 4; // Maximum number of additional mountain ranges

        for (int m = 0; m < maxMountains; m++)
        {
            int startX, startY;
            int attempts = 0;
            int maxAttempts = 10000;
            do
            {
                (startX, startY) = GetRandomPoint();
                attempts++;
                if (attempts > maxAttempts)
                {
                    Console.WriteLine("Unable to find a valid start tile for mountain creation");
                    return;
                }
            } while (!IsInBiome(startX, startY, 'P') && !IsInBiome(startX, startY, 'F'));

            int endX, endY;
            attempts = 0;
            do
            {
                (endX, endY) = GetRandomPointInRange(startX, startY, 10, 80);
                attempts++;
                if (attempts > maxAttempts)
                {
                    Console.WriteLine("Unable to find a valid end tile for mountain creation");
                    return;
                }
            } while (!IsInBiome(endX, endY, 'P') && !IsInBiome(endX, endY, 'F'));

            CreateMountainRange(startX, startY, endX, endY);
            CreateAdditionalMountainRange(startX, startY, maxAdditionalMountains);
        }
    }
    private void CreateAdditionalMountainRange(int oldStartX, int oldStartY, int maxAttempts)
    {
        int currentAttempts = 0;
        while (currentAttempts < maxAttempts)
        {
            if (rngMap.NextDouble() < 0.5) // 50% chance to create an additional mountain range
            {
                int newStartX, newStartY;
                do
                {
                    (newStartX, newStartY) = GetRandomPointInBiome('M');
                } while (GetDistance(oldStartX, oldStartY, newStartX, newStartY) < 20);

                int endX, endY;
                (endX, endY) = GetRandomPointInRange(newStartX, newStartY, 10, 80);
                CreateMountainRange(newStartX, newStartY, endX, endY);

                // Update old starting point to the new one
                oldStartX = newStartX;
                oldStartY = newStartY;
            }
            currentAttempts++;
        }
    }
    private void CreateMountainRange(int startX, int startY, int endX, int endY)
    {
        int x = startX;
        int y = startY;
        int maxSteps = width * height; // Limit the number of steps to avoid infinite loop
        int steps = 0;

        // Define the mountain path
        while (steps < maxSteps)
        {
            int mountainWidth = rngMap.Next(conf.MinMountainWidth, conf.MaxMountainWidth + 1); // Mountain width between minRiverWidth and maxRiverWidth
            for (int i = -mountainWidth / 2; i <= mountainWidth / 2; i++)
            {
                if (x + i >= 0 && x + i < width)
                {
                    mapData[x + i, y] = 'M'; // Mark the tile as mountain
                }
                if (y + i >= 0 && y + i < height)
                {
                    mapData[x, y + i] = 'M'; // Mark the tile as mountain
                }
            }

            // Randomly choose the next direction, with a bias towards moving towards the end point
            int direction = rngMap.Next(100);
            if (direction < 30)
            {
                if (Math.Abs(endX - x) > Math.Abs(endY - y))
                {
                    x += endX > x ? 1 : -1; // Move towards endX
                }
                else
                {
                    y += endY > y ? 1 : -1; // Move towards endY
                }
            }
            else if (direction < 60)
            {
                if (Math.Abs(endX - x) > Math.Abs(endY - y))
                {
                    y += rngMap.Next(2) == 0 ? 1 : -1; // Move up or down
                }
                else
                {
                    x += rngMap.Next(2) == 0 ? 1 : -1; // Move left or right
                }
            }
            else
            {
                // Add some winding effect
                if (rngMap.Next(2) == 0)
                {
                    x += rngMap.Next(2) == 0 ? 1 : -1;
                }
                else
                {
                    y += rngMap.Next(2) == 0 ? 1 : -1;
                }
            }

            // Ensure the mountain range stays within bounds
            if (x < 0) x = 0;
            if (x >= width) x = width - 1;
            if (y < 0) y = 0;
            if (y >= height) y = height - 1;

            // Check if the mountain range has reached the end point
            if (x == endX && y == endY)
            {
                break;
            }

            steps++;
        }
    }
    private void MountainDepth()
    {
        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                if ((mapData[x, y] == 'M' || mapData[x, y] == 'm') && CountSurroundingBiomes(x, y, 'M') + CountSurroundingBiomes(x, y, 'm') == 8)
                {
                    mapData[x, y] = 'm'; // Turn surrounded mountains into darker mountains
                }
            }
        }
        int attempts = 0;
        int maxAttempts = 500; // Limit the number of attempts to avoid infinite loop
        for (int i = 0; i < 42 && attempts < maxAttempts; i++)
        {
            if (rngMap.NextDouble() < 0.6) // Chance to execute each iteration
            {
                (int, int) randomPoint = GetRandomPointInBiome('m');
                if (randomPoint != (0, 0)) // Ensure the point is valid
                {
                    mapData[randomPoint.Item1, randomPoint.Item2] = 'M';
                    SpreadTile(randomPoint.Item1, randomPoint.Item2, 0.5, 1, 5);
                }
            }
            attempts++;
        }
    }
    private void ErodeMountainRanges()
    {
        int erosionIterations = 1000; // Number of iterations for erosion
        double windFactor = 0.1; // Factor for wind erosion
        double waterFactor = 0.2; // Factor for water erosion
        double temperatureFactor = 0.05; // Factor for temperature erosion
        double erosionThreshold = 0.01; // Minimum erosion to consider a change
        int maxNoChangeIterations = 100; // Max iterations without significant change

        int noChangeCounter = 0;

        for (int iteration = 0; iteration < erosionIterations; iteration++)
        {
            bool significantChange = false;

            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    if (mapData[x, y] == 'M' || mapData[x, y] == 'm' || mapData[x, y] == 'S')
                    {
                        // Wind erosion
                        double windErosion = CalculateWindErosion(x, y, windFactor);
                        ApplyErosion(x, y, windErosion);

                        // Water erosion
                        double waterErosion = CalculateWaterErosion(x, y, waterFactor);
                        ApplyErosion(x, y, waterErosion);

                        // Temperature erosion
                        double temperatureErosion = CalculateTemperatureErosion(x, y, temperatureFactor);
                        ApplyErosion(x, y, temperatureErosion);

                        if (windErosion > erosionThreshold || waterErosion > erosionThreshold || temperatureErosion > erosionThreshold)
                        {
                            significantChange = true;
                        }
                    }
                }
            }

            if (!significantChange)
            {
                noChangeCounter++;
                if (noChangeCounter >= maxNoChangeIterations)
                {
                    break;
                }
            }
            else
            {
                noChangeCounter = 0;
            }
        }

        SmoothMountainEdges();
    }
    private double CalculateWindErosion(int x, int y, double windFactor)
    {
        // Simulate wind erosion based on neighboring tiles
        double erosion = 0.0;
        foreach ((int nx, int ny) in GetNeighbors(x, y))
        {
            if (mapData[nx, ny] == 'P' || mapData[nx, ny] == 'F')
            {
                erosion += windFactor;
            }
        }
        return erosion;
    }
    private double CalculateWaterErosion(int x, int y, double waterFactor)
    {
        // Simulate water erosion based on neighboring tiles
        double erosion = 0.0;
        foreach ((int nx, int ny) in GetNeighbors(x, y))
        {
            if (mapData[nx, ny] == 'O' || mapData[nx, ny] == 'L' || mapData[nx, ny] == 'R')
            {
                erosion += waterFactor;
            }
        }
        return erosion;
    }
    private double CalculateTemperatureErosion(int x, int y, double temperatureFactor)
    {
        // Simulate temperature erosion based on random temperature changes
        Random rng = new Random(seed);
        double temperatureChange = rng.NextDouble() * temperatureFactor;
        return temperatureChange;
    }
    private void ApplyErosion(int x, int y, double erosion)
    {
        // Apply erosion to the mountain tile
        if (erosion > 0.5)
        {
            mapData[x, y] = GetMostSurroundedBiome(x, y); // Turn heavily eroded mountain into plains
        }
        else if (erosion > 0.2)
        {
            mapData[x, y] = 'm'; // Turn moderately eroded mountain into darker mountain
        }
    }
    /*             private void SmoothMountainEdges()
                {
                    // Smooth the edges of the mountains to avoid checkerboard patterns
                    for (int x = 1; x < width - 1; x++)
                    {
                        for (int y = 1; y < height - 1; y++)
                        {
                            if (mapData[x, y] == 'M' || mapData[x, y] == 'N' || mapData[x, y] == 'S')
                            {
                                int mountainCount = 0;
                                foreach (var (nx, ny) in GetNeighbors(x, y))
                                {
                                    if (mapData[nx, ny] == 'M' || mapData[nx, ny] == 'N' || mapData[nx, ny] == 'S')
                                    {
                                        mountainCount++;
                                    }
                                }

                                if (mountainCount == 0)
                                {
                                    mapData[x, y] = 'P'; // Turn isolated mountains into plains
                                }
                            }
                        }
                    }
                }
    */
    private void SmoothMountainEdges()
    {
        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                if (mapData[x, y] == 'M' && CountSurroundingBiomes(x, y, 'M') < 5)
                {
                    mapData[x, y] = 'P'; // Turn mountain into plains
                }
            }
        }
    }
    public void GenerateSnowPeaks()
    {
        double snowPeakChance = 0.40; // Chance of turning a mountains into a snow peak
        bool[,] visited = new bool[width, height];

        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                if ((mapData[x, y] == 'm' || mapData[x, y] == 'S') && CountSurroundingBiomes(x, y, 'm') + CountSurroundingBiomes(x, y, 'S') >= 7 && !visited[x, y])
                {
                    if (rngMap.NextDouble() < snowPeakChance)
                    {
                        SpreadSnowPeaks(x, y, visited);
                    }
                }
            }
        }
    }
    public void DeleteBadSnowPeaks()
    {
        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                if (mapData[x, y] == 'S' && CountSurroundingBiomes(x, y, 'M') + CountSurroundingBiomes(x, y, 'm') < 8)
                {
                    mapData[x, y] = 'M'; // Turn snow peak back into mountain
                }
            }
        }
    }
    private void SpreadSnowPeaks(int startX, int startY, bool[,] visited)
    {
        Queue<(int, int)> queue = new Queue<(int, int)>();
        queue.Enqueue((startX, startY));
        visited[startX, startY] = true;

        while (queue.Count > 0)
        {
            (int x, int y) = queue.Dequeue();
            mapData[x, y] = 'S'; // Turn into snow peak

            // Spread to neighboring mountains
            foreach ((int nx, int ny) in GetNeighbors(x, y))
            {
                if (nx >= 0 && nx < width && ny >= 0 && ny < height && mapData[nx, ny] == 'M' && !visited[nx, ny])
                {
                    if (CountSurroundingBiomes(nx, ny, 'M') >= 8 && rngMap.NextDouble() < 0.8) // 80% chance to spread
                    {
                        queue.Enqueue((nx, ny));
                        visited[nx, ny] = true;
                    }
                }
            }
        }
    }
    #endregion
    #region river functions
    private void CreateRiver()
    {
        int maxRivers = 1; // Maximum number of rivers to generate
        for (int r = 0; r < maxRivers; r++)
        {
            // Choose a random starting point on any edge, ensuring it's at least 10 tiles away from corners
            int startX, startY;
            int startEdge; // 0 = top, 1 = bottom, 2 = left, 3 = right
            switch (rngMap.Next(2))
            {
                case 0:
                    startX = rngMap.Next(10, width - 10);
                    if (rngMap.Next(2) == 0)
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
                    startY = rngMap.Next(10, Math.Max(11, height - 10));
                    if (rngMap.Next(2) == 0)
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
                int riverWidth = rngMap.Next(minRiverWidth, maxRiverWidth + 1); // River width between minRiverWidth and maxRiverWidth
                for (int i = -riverWidth / 2; i <= riverWidth / 2; i++)
                {
                    if (x + i >= 0 && x + i < width)
                    {
                        mapData[x + i, y] = 'R'; // Mark the tile as river
                    }
                    if (y + i >= 0 && y + i < height)
                    {
                        mapData[x, y + i] = 'R'; // Mark the tile as river
                    }
                }

                // Randomly choose the next direction, with a bias towards moving forward
                int direction = rngMap.Next(100);
                if (direction < 30)
                {
                    if (startEdge == 2 || startEdge == 3)
                    {
                        y += rngMap.Next(2) == 0 ? 1 : -1; // Move up or down
                    }
                    else
                    {
                        x += rngMap.Next(2) == 0 ? 1 : -1; // Move left or right
                    }
                }
                else if (direction < 60)
                {
                    if (startEdge == 2 || startEdge == 3)
                    {
                        x += startEdge == 2 ? 1 : -1; // Move right if starting at left, left if starting at right
                    }
                    else
                    {
                        y += startEdge == 0 ? 1 : -1; // Move down if starting at top, up if starting at bottom
                    }
                }
                else
                {
                    // Add some winding effect
                    if (startEdge == 2 || startEdge == 3)
                    {
                        y += rngMap.Next(2) == 0 ? 1 : -1;
                    }
                    else
                    {
                        x += rngMap.Next(2) == 0 ? 1 : -1;
                    }
                }

                // Ensure the river flows within bounds
                if (x < 0) x = 0;
                if (x >= width) x = width - 1;
                if (y < 0) y = 0;
                if (y >= height) y = height - 1;

                // Check if the river has reached any edge that is not the starting edge
                if ((startEdge == 0 && y == height - 1) || (startEdge == 1 && y == 0) ||
                    (startEdge == 2 && x == width - 1) || (startEdge == 3 && x == 0) ||
                    (startEdge != 0 && startEdge != 1 && (y == 0 || y == height - 1)) ||
                    (startEdge != 2 && startEdge != 3 && (x == 0 || x == width - 1)))
                {
                    break;
                }
            }

            // Ensure the river reaches an edge
            while (true)
            {
                int riverWidth = rngMap.Next(minRiverWidth, maxRiverWidth + 1); // River width between minRiverWidth and maxRiverWidth
                for (int i = -riverWidth / 2; i <= riverWidth / 2; i++)
                {
                    if (x + i >= 0 && x + i < width)
                    {
                        mapData[x + i, y] = 'R'; // Mark the tile as river
                    }
                    if (y + i >= 0 && y + i < height)
                    {
                        mapData[x, y + i] = 'R'; // Mark the tile as river
                    }
                }

                // Move towards the nearest edge
                if (x > 0 && x < width - 1)
                {
                    x += x < width / 2 ? 1 : -1;
                }
                else if (y > 0 && y < height - 1)
                {
                    y += y < height / 2 ? 1 : -1;
                }

                // Ensure the river flows within bounds
                if (x < 0) x = 0;
                if (x >= width) x = width - 1;
                if (y < 0) y = 0;
                if (y >= height) y = height - 1;

                // Check if the river has reached any edge
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                {
                    // Ensure the river does not end on a similar y or x
                    if ((startEdge == 2 || startEdge == 3) && Math.Abs(y - startY) < height / 3)
                    {
                        y = (y + height / 3) % height;
                        if (Math.Abs(y - startY) < height / 3)
                        {
                            y = (y + height / 2) % height;
                        }
                    }
                    else if ((startEdge == 0 || startEdge == 1) && Math.Abs(x - startX) < width / 3)
                    {
                        x = (x + width / 3) % width;
                        if (Math.Abs(x - startX) < width / 3)
                        {
                            x = (x + width / 2) % width;
                        }
                    }
                    break;
                }
            }
        }

        // Smooth the river edges
        SmoothRiverEdges();
    }
    private void SmoothRiverEdges()
    {
        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                if (mapData[x, y] == 'R')
                {
                    int riverCount = 0;
                    if (mapData[x - 1, y] == 'R') riverCount++;
                    if (mapData[x + 1, y] == 'R') riverCount++;
                    if (mapData[x, y - 1] == 'R') riverCount++;
                    if (mapData[x, y + 1] == 'R') riverCount++;
                    if (mapData[x - 1, y - 1] == 'R') riverCount++;
                    if (mapData[x + 1, y - 1] == 'R') riverCount++;
                    if (mapData[x - 1, y + 1] == 'R') riverCount++;
                    if (mapData[x + 1, y + 1] == 'R') riverCount++;

                    if (riverCount < 3)
                    {
                        mapData[x, y] = GetMostSurroundedBiome(x, y); // Turn isolated river into the biome it's most surrounded by
                    }
                }
            }
        }
    }
    #endregion
    #region lake functions
    private void CreateLakes()
    {
        int maxLakes = rng.Next(1, 4); // Maximum number of lakes to generate
        int minRadius = 5;
        int maxRadius = 15;

        for (int i = 0; i < maxLakes; i++)
        {
            (int x, int y) startPoint = FindValidStartingPoint();
            if (startPoint == (-1, -1)) continue;

            int targetCount = new Random(seed).Next(1, 4);
            List<(int x, int y)> targetPoints = new List<(int x, int y)>();

            for (int j = 0; j < targetCount; j++)
            {
                (int x, int y) targetPoint = FindValidTargetPoint(startPoint, minRadius, maxRadius);
                if (targetPoint != (-1, -1))
                {
                    targetPoints.Add(targetPoint);
                }
            }

            GenerateLakePath(startPoint, targetPoints);
        }

        SmoothLakeEdges();
    }
    private (int x, int y) FindValidStartingPoint()
    {
        Random rng = new Random(seed);
        for (int attempts = 0; attempts < 100; attempts++)
        {
            int x = rng.Next(0, conf.Height);
            int y = rng.Next(0, conf.Height);

            if (IsValidStartingPoint(x, y))
            {
                return (x, y);
            }
        }
        return (-1, -1);
    }
    private bool IsValidStartingPoint(int x, int y)
    {
        if (mapData[x, y] != 'P' && mapData[x, y] != 'F') return false;

        for (int dx = -6; dx <= 6; dx++)
        {
            for (int dy = -6; dy <= 6; dy++)
            {
                int nx = x + dx;
                int ny = y + dy;
                if (nx >= 0 && ny >= 0 && nx < conf.Width && ny < conf.Height)
                {
                    if (mapData[nx, ny] == 'O' || mapData[nx, ny] == 'L' || mapData[nx, ny] == 'R' || mapData[nx, ny] == 'M' || mapData[nx, ny] == 'm' || mapData[nx, ny] == 'S')
                    {
                        return false;
                    }
                }
            }
        }
        return true;
    }
    private (int x, int y) FindValidTargetPoint((int x, int y) startPoint, int minRadius, int maxRadius)
    {
        Random rng = new Random(seed);
        for (int attempts = 0; attempts < 100; attempts++)
        {
            int radius = rng.Next(minRadius, maxRadius + 1);
            double angle = rng.NextDouble() * 2 * Math.PI;
            int x = startPoint.x + (int)(radius * Math.Cos(angle));
            int y = startPoint.y + (int)(radius * Math.Sin(angle));

            if (IsValidTargetPoint(x, y))
            {
                return (x, y);
            }
        }
        return (-1, -1);
    }
    private bool IsValidTargetPoint(int x, int y)
    {
        if (x < 0 || y < 0 || x >= conf.Width || y >= conf.Height) return false;
        if (mapData[x, y] == 'M' || mapData[x, y] == 'm' || mapData[x, y] == 'S') return false;

        for (int dx = -8; dx <= 8; dx++)
        {
            for (int dy = -8; dy <= 8; dy++)
            {
                int nx = x + dx;
                int ny = y + dy;
                if (nx >= 0 && ny >= 0 && nx < conf.Width && ny < conf.Height)
                {
                    if (mapData[nx, ny] == 'O' || mapData[nx, ny] == 'L' || mapData[nx, ny] == 'R')
                    {
                        return false;
                    }
                }
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
            double stepX = dx / (double)steps;
            double stepY = dy / (double)steps;

            for (int i = 0; i <= steps; i++)
            {
                int x = startPoint.x + (int)(i * stepX);
                int y = startPoint.y + (int)(i * stepY);
                FillCircle(x, y, 'L', 2, 5);
            }
        }
    }
    private void SmoothLakeEdges()
    {
        char lakeTile = 'L';
        int smoothingThreshold = 5; // Number of lake neighbors required to convert a tile to lake

        for (int x = 0; x < conf.Width; x++)
        {
            for (int y = 0; y < conf.Height; y++)
            {
                if (mapData[x, y] != lakeTile && CountLakeNeighbors(x, y) >= smoothingThreshold)
                {
                    mapData[x, y] = lakeTile;
                }
            }
        }
    }
    private bool IsLakeTile(int x, int y)
    {
        return mapData[x, y] == 'L';
    }
    private int CountLakeNeighbors(int x, int y)
    {
        int lakeCount = 0;
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue; // Skip the center tile
                int nx = x + dx;
                int ny = y + dy;
                if (nx >= 0 && ny >= 0 && nx < conf.Width && ny < conf.Height && IsLakeTile(nx, ny))
                {
                    lakeCount++;
                }
            }
        }
        return lakeCount;
    }
    #endregion
    #region stream functions
    private void CreateStreams()
    {
        int maxStreams = 1;
        for (int s = 0; s < maxStreams; s++)
        {
            (int x, int y) startPoint;
            int attempts = 0;
            do
            {
                startPoint = GetRandomPointInBiome('L');
                attempts++;
            } while (startPoint != (-1, -1) && attempts < 100);

            if (startPoint == (-1, -1))
            {
                outputBuffer.Add("No valid starting point found for stream.");
                continue;
            }

            (int x, int y) endPoint = GetStreamEndPoint(startPoint);
            if (endPoint == (-1, -1))
            {
                outputBuffer.Add("No valid ending point found for stream.");
                continue;
            }
            CreateStreamPath(startPoint, endPoint);
        }
    }
    private bool IsNearWater(int x, int y, int radius)
    {
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                int nx = x + dx;
                int ny = y + dy;
                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                {
                    if (mapData[nx, ny] == 'L' || mapData[nx, ny] == 'R' || mapData[nx, ny] == 'O' || mapData[nx, ny] == 's' || mapData[nx, ny] == 'r' || mapData[nx, ny] == 'o' || mapData[nx, ny] == 'l')
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }
    private (int x, int y) GetStreamEndPoint((int x, int y) startPoint)
    {
        (int x, int y) closestMountain = (-1, -1);
        double closestDistance = double.MaxValue;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (mapData[x, y] == 'm')
                {
                    double distance = GetDistance(startPoint.x, startPoint.y, x, y);
                    if (distance > 25 && distance < 50 && distance < closestDistance && !IsNearWater(x, y, 5))
                    {
                        closestMountain = (x, y);
                        closestDistance = distance;
                    }
                }
            }
        }
        return closestMountain;
    }
    private void CreateStreamPath((int x, int y) startPoint, (int x, int y) endPoint)
    {
        List<(int x, int y)> path = GenerateSwirlyPath(startPoint, endPoint);
        if (path.Count == 0 || path.Count < 5)
        {
            outputBuffer.Add("No valid path found for stream.");
            return;
        }
        foreach ((int x, int y) point in path)
        {
            if (point.x >= 0 && point.x < width && point.y >= 0 && point.y < height)
            {
                mapData[point.x, point.y] = 's';
            }
        }
    }
/*     private List<(int x, int y)> GenerateSwirlyPath((int x, int y) startPoint, (int x, int y) endPoint)
    {
        var path = new List<(int x, int y)>();
        var visited = new HashSet<(int x, int y)>();

        (int x, int y) current = startPoint;
        path.Add(current);
        visited.Add(current);

        var rng = new Random(seed);
        double angle = Math.Atan2(endPoint.y - startPoint.y, endPoint.x - startPoint.x);
        double angleOffset = rng.NextDouble() * Math.PI / 2 - Math.PI / 4;
        double angleChange = 0.3;

        int maxIterations = 1000; // Safeguard to prevent infinite loops
        int iterations = 0;

        while (current != endPoint && iterations < maxIterations)
        {
            double distance = GetDistance(current.x, current.y, endPoint.x, endPoint.y);
            if (distance < 1)
            {
                break;
            }

            double newX = current.x + Math.Cos(angle + angleOffset);
            double newY = current.y + Math.Sin(angle + angleOffset);
            (int x, int y) next = ((int)Math.Round(newX), (int)Math.Round(newY));

            if (!visited.Contains(next) && next.x >= 0 && next.x < width && next.y >= 0 && next.y < height)
            {
                path.Add(next);
                visited.Add(next);
                current = next;
            }
            else
            {
                angleOffset += rng.NextDouble() * angleChange - angleChange / 2;
            }

            iterations++;
        }

        // Ensure the path reaches the endpoint
        if (current != endPoint)
        {
            path.Add(endPoint);
            visited.Add(endPoint);
        }

        // Ensure all points have two sides touching another path point, except start and end points
        for (int i = 1; i < path.Count - 1; i++)
        {
            var (x, y) = path[i];
            int neighbors = 0;

            foreach ((int nx, int ny) in GetNeighbors(x, y))
            {
                if (path.Contains((nx, ny)))
                {
                    neighbors++;
                }
            }

            if (neighbors < 2)
            {
                path.RemoveAt(i);
                i--;
            }
        }

        // Ensure start and end points have exactly one neighbor
        if (path.Count > 1)
        {
            var (startX, startY) = path[0];
            var (endX, endY) = path[path.Count - 1];

            int startNeighbors = 0;
            foreach ((int nx, int ny) in GetNeighbors(startX, startY))
            {
                if (path.Contains((nx, ny)))
                {
                    startNeighbors++;
                }
            }

            if (startNeighbors != 1)
            {
                path.RemoveAt(0);
            }

            int endNeighbors = 0;
            foreach ((int nx, int ny) in GetNeighbors(endX, endY))
            {
                if (path.Contains((nx, ny)))
                {
                    endNeighbors++;
                }
            }

            if (endNeighbors != 1)
            {
                path.RemoveAt(path.Count - 1);
            }
        }

        return path;
    }
 */
    private List<(int x, int y)> GenerateSwirlyPath((int x, int y) startPoint, (int x, int y) endPoint)
    {
        var path = new List<(int x, int y)>();
        double angle = Math.Atan2(endPoint.y - startPoint.y, endPoint.x - startPoint.x);
        double distance = GetDistance(startPoint.x, startPoint.y, endPoint.x, endPoint.y);
        double angleOffset = (rngMap.NextDouble() - 0.5) * Math.PI / 4; // Random initial offset
        double angleChange = 0.1; // Change in angle per step

        for (int i = 0; i < distance; i++)
        {
            double x = startPoint.x + i * Math.Cos(angle + angleOffset);
            double y = startPoint.y + i * Math.Sin(angle + angleOffset);
            (int px, int py) = ((int)x, (int)y);
            path.Add((px, py));
            angleOffset += (rngMap.NextDouble() - 0.5) * angleChange; // Randomly adjust the angle
        }

        // Ensure each path point has 2 side neighbors
        for (int i = 1; i < path.Count - 1 && i < 1000; i++)
        {
            var (px, py) = path[i];
            if (CountSideNeighbors(path, px, py) < 2)
            {
                (int nx, int ny) = GetNextCornerPoint(px, py, path);
                if (!path.Contains((nx, ny)) && CountSideNeighbors(path, nx, ny) == 2)
                {
                    path.Add((nx, ny));
                }
            }
        }

        return path;
    }

    private int CountSideNeighbors(List<(int x, int y)> path, int x, int y)
    {
        int count = 0;
        if (path.Contains((x + 1, y))) count++;
        if (path.Contains((x - 1, y))) count++;
        if (path.Contains((x, y + 1))) count++;
        if (path.Contains((x, y - 1))) count++;
        return count;
    }

    private (int x, int y) GetNextCornerPoint(int x, int y, List<(int x, int y)> path)
    {
        List<(int x, int y)> possiblePoints = new List<(int x, int y)>
        {
            (x + 1, y + 1),
            (x - 1, y - 1),
            (x - 1, y + 1),
            (x + 1, y - 1)
        };

        // Shuffle the possible points to add randomness
        possiblePoints = possiblePoints.OrderBy(p => rngMap.Next()).ToList();

        foreach ((int nx, int ny) in possiblePoints)
        {
            if (!path.Contains((nx, ny)) && CountSideNeighbors(path, nx, ny) == 2)
            {
                return (nx, ny);
            }
        }

        // If no valid point is found, return the original point
        return (x, y);
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
        //double beachChance = avarageTempature * 0.5 * 0.01; // Chance of creating a beach
        double beachChance = Math.Clamp((avarageTempature - 0.4) * avarageTempatureFactor, 0.001, 0.03); // Adjusted formula for beach chance
        int minBeachSize = (int)Math.Clamp(25 + (avarageTempature - 0.5) * 20, 20, 50);
        int maxBeachSize = (int)Math.Clamp(35 + (avarageTempature - 0.5) * 20, 30, 68);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
            if (mapData[x, y] == 'O' && IsNextToLand(x, y) && !IsNextToMountain(x, y))
            {
                if (rngMap.NextDouble() < beachChance)
                {
                CreateSmoothBeach(x, y, minBeachSize, maxBeachSize);
                }
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
                if (nx >= 0 && ny >= 0 && nx < width && ny < height && (mapData[nx, ny] == 'P' || mapData[nx, ny] == 'F'))
                {
                    return true;
                }
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
                if (nx >= 0 && ny >= 0 && nx < width && ny < height && (mapData[nx, ny] == 'M' || mapData[nx, ny] == 'm' || mapData[nx, ny] == 'S'))
                {
                    return true;
                }
            }
        }
        return false;
    }
    private void CreateSmoothBeach(int startX, int startY, int minSize, int maxSize)
    {
        int beachSize = rngMap.Next(minSize, maxSize + 1);
        Queue<(int, int)> queue = new Queue<(int, int)>();
        queue.Enqueue((startX, startY));
        bool[,] visited = new bool[width, height];
        visited[startX, startY] = true;

        while (queue.Count > 0 && beachSize > 0)
        {
            (int x, int y) = queue.Dequeue();
            if (mapData[x, y] == 'P' || mapData[x, y] == 'F') // Replace only land tiles
            {
                mapData[x, y] = 'B'; // Assuming 'B' represents beach
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
        // Add dark spots to the beaches
        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                if (mapData[x, y] == 'B')
                {
                    if (rngMap.NextDouble() < 0.1) // 10% chance to place a dark spot
                    {
                        mapData[x, y] = 'b'; // Assuming 'D' represents a dark spot
                        SpreadTile(x, y, 0.5, 1, 3); // Spread the dark spot with a max of 3 tiles
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
                if (mapData[x, y] == 'B' && CountSurroundingBiomes(x, y, 'B') < 5)
                {
                    mapData[x, y] = GetMostSurroundingBiome(x, y);
                }
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
                if (mapData[x, y] == 'L' && CountSurroundingBiomes(x, y, 'L') + CountSurroundingBiomes(x, y, 'l') + CountSurroundingBiomes(x, y, 'O') + CountSurroundingBiomes(x, y, 'o') + CountSurroundingBiomes(x, y, 'R') + CountSurroundingBiomes(x, y, 'r') + CountSurroundingBiomes(x, y, 's') == 8)
                {
                    mapData[x, y] = 'l'; // Turn surrounded lake into deep lake
                }
                else if (mapData[x, y] == 'O' && CountSurroundingBiomes(x, y, 'O') + CountSurroundingBiomes(x, y, 'o') + CountSurroundingBiomes(x, y, 'L') + CountSurroundingBiomes(x, y, 'l') + CountSurroundingBiomes(x, y, 'R') + CountSurroundingBiomes(x, y, 'r') + CountSurroundingBiomes(x, y, 's') == 8)
                {
                    mapData[x, y] = 'o'; // Turn surrounded ocean into deep ocean
                }
                else if (mapData[x, y] == 'R' && CountSurroundingBiomes(x, y, 'R') + CountSurroundingBiomes(x, y, 'r') + CountSurroundingBiomes(x, y, 'O') + CountSurroundingBiomes(x, y, 'o') + CountSurroundingBiomes(x, y, 'L') + CountSurroundingBiomes(x, y, 'l') + CountSurroundingBiomes(x, y, 's') == 8)
                {
                    mapData[x, y] = 'r'; // Turn surrounded river into deep river
                }
                else if (mapData[x, y] == 's' && CountSurroundingBiomes(x, y, 's') + CountSurroundingBiomes(x, y, 'R') + CountSurroundingBiomes(x, y, 'r') + CountSurroundingBiomes(x, y, 'O') + CountSurroundingBiomes(x, y, 'o') + CountSurroundingBiomes(x, y, 'L') + CountSurroundingBiomes(x, y, 'l') == 8)
                {
                    mapData[x, y] = 'r'; // Turn surrounded stream into deep stream
                }
            }
        }
    }
    #region other functions
    public void DrawSkull()
    {
        int skullWidth = 15;
        int skullHeight = 15;
        int startX = (width - skullWidth) / 2;
        int startY = (height - skullHeight) / 2;

        string[] skullPattern = new string[]
        {
            "    @@@@@@@    ",
            "   @@@@@@@@@   ",
            "  @@ @@@@@ @@  ",
            " @@  @@ @@  @@ ",
            " @@   @ @   @@ ",
            " @@         @@ ",
            " @@  @@@@@  @@ ",
            "  @@ @@@@@ @@  ",
            "   @@@@@@@@@   ",
            "    @@@@@@@    ",
            "     @@@@@     ",
            "    @@ @@ @    ",
            "   @@  @  @@   ",
            "  @@   @   @@  ",
            "     @@@@@     "
        };

        for (int y = 0; y < skullHeight; y++)
        {
            for (int x = 0; x < skullWidth; x++)
            {
                if (skullPattern[y][x] != ' ')
                {
                    mapData[startX + x, startY + y] = 'X'; // Replace tile with '!'
                }
            }
        }
    }
    public void SkullEatsMap()
    {
        // Wait for about 5 seconds
        Thread.Sleep(5000);

        // Begin 'eating' the tiles from top to bottom with imperfections
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (rngMap.NextDouble() > 0.1) // Imperfections
                {
                    mapData[x, y] = '!'; // Replace tile with '!'
                }
            }
            // Delay after each row
            Thread.Sleep(150);
        }
        Console.SetCursorPosition(0, 9999);
    }
    public void CheckAndReplaceBiomes(char selectedBiome, int minSameBiome)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (mapData[x, y] == selectedBiome)
                {
                    int sameBiomeCount = CountSurroundingBiomes(x, y, selectedBiome);
                    if (sameBiomeCount < minSameBiome)
                    {
                        char mostSurroundedBiome = GetMostSurroundedBiome(x, y);
                        mapData[x, y] = mostSurroundedBiome;
                    }
                }
            }
        }
    }
    private char GetMostSurroundedBiome(int x, int y)
    {
        Dictionary<char, int> biomeCounts = new Dictionary<char, int>();
        foreach ((int nx, int ny) in GetNeighbors(x, y))
        {
            if (nx >= 0 && nx < width && ny >= 0 && ny < height) // Ensure the neighbor is within bounds
            {
                char neighborBiome = mapData[nx, ny];
                if (biomeCounts.ContainsKey(neighborBiome))
                {
                    biomeCounts[neighborBiome]++;
                }
                else
                {
                    biomeCounts[neighborBiome] = 1;
                }
            }
        }

        return biomeCounts.OrderByDescending(b => b.Value).First().Key;
    }
    public void SingleTileCheckPF(char selectedBiome1, char selectedBiome2, int minSameBiome)
    {
        CheckAndReplaceBiomes(selectedBiome1, minSameBiome);
        CheckAndReplaceBiomes(selectedBiome2, minSameBiome);
    }
    #endregion
    #region water system
    #region waves
    private class Wave
    {
        public List<(double x, double y)> Points { get; set; } = new List<(double x, double y)>();
        public double Direction { get; set; }
        public double Speed { get; set; }
        public double Curvature { get; set; }
        public int Length { get; set; }
        public double Intensity { get; set; } = 0.0;
        public HashSet<(int x, int y)> PreviousPoints { get; set; } = new HashSet<(int x, int y)>();
        public bool IsNight { get; set; }
        public bool IsDarkening { get; set; }
    }
    private List<Wave> waves = new List<Wave>();
    private readonly int MAX_WAVES = (int)Math.Round(conf.Width / 17.5);
    private const double WAVE_SPEED = 0.2;
    private void InitializeWaves()
    {
        waves.Clear();
        for (int i = 0; i < MAX_WAVES; i++)
        {
            AddNewWave();
        }
    }
    private static readonly object consoleLock = new object();
    private HashSet<(int x, int y)> wavePositions = new HashSet<(int x, int y)>();
    public void AnimateWater()
    {
        // Clear the wavePositions at the start
        wavePositions.Clear();

        foreach (Wave? wave in waves.ToList())
        {
            // Adjust wave intensity based on time of day
            if (!wave.IsNight && wave.Intensity < 1.0)
                wave.Intensity = Math.Min(wave.Intensity + 0.1, 1.0);
            else if (wave.IsNight && wave.Intensity < 1.0)
                wave.Intensity = Math.Max(wave.Intensity + 0.1, 0.0);
            // Store old positions to clear them
            HashSet<(int x, int y)> oldPoints = new HashSet<(int x, int y)>(wave.PreviousPoints);
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
                    if ((isCloudsRendering && !IsTileUnderCloud(point.x, point.y)) || !isCloudsRendering) UpdateWaterTile(point.x, point.y, false, false, wave.IsNight);
                    // Remove the point from wavePositions
                    wavePositions.Remove(point);
                }
            }

            // Draw new wave positions
            List<(int x, int y)> pointsToUpdate = wave.PreviousPoints.ToList();
            foreach ((int x, int y) in pointsToUpdate)
            {
                // Check if point is under cloud
                bool isUnderCloud = IsTileUnderCloud(x, y);
                if (!isCloudsRendering || (isCloudsRendering && !isUnderCloud))
                {
                    UpdateWaterTile(x, y, true, wave.IsNight, wave.IsDarkening, wave.Intensity);
                }
                // Add the point to wavePositions
                wavePositions.Add((x, y));
            }

            if (removeWave)
            {
                // Clear final positions before removing
                foreach ((int x, int y) in wave.PreviousPoints)
                {
                    UpdateWaterTile(x, y, false, false, wave.IsNight);
                    // Remove the point from wavePositions
                    wavePositions.Remove((x, y));
                }
                waves.Remove(wave);
                AddNewWave();
            }
            else
            {
                wave.Points = newPoints;
            }
        }
    }
    private void UpdateWaterTile(int x, int y, bool isWave, bool isNight, bool isDarkening, double intensity = 1.0)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return;
        char tile = mapData[x, y];

        (int r, int g, int b) baseColor;

        if (isNight)
        {
            baseColor = GetDarkenedTileColor(tile, x, y);
        }
        else
        {
            if (!isNight)
            {
                if (IsThereACloudShadow(x, y) && isCloudsShadowsRendering)
                {
                    // Get cloud shadow color with correct intensity
                    baseColor = GetShadowColor(x, y);
                }
                else
                {
                    baseColor = GetColor(tile, x, y);
                }
            }
            else // Night time
            {
                baseColor = GetColor(tile, x, y);
            }
        }

        (int r, int g, int b) finalColor;

        if (isWave && intensity > 0.0)
        {
            // Apply wave color intensity effect
            (int r, int g, int b) waveColor = GetColor('O', x, y);
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
            Console.SetCursorPosition(leftPadding + x * 2, y + topPadding);
            string background = SetBackgroundColor(finalColor.r, finalColor.g, finalColor.b);
            Console.Write(background + "  " + ResetColor());
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

        return cloudData[cloudX, cloudY] == '1' || cloudData[cloudX, cloudY] == '2' || cloudData[cloudX, cloudY] == '3' || cloudData[cloudX, cloudY] == '4' ||
                cloudData[cloudX, cloudY] == '5' || cloudData[cloudX, cloudY] == '6';
    }
    private void AddNewWave()
    {
        List<(int x, int y)> validPositions = new List<(int x, int y)>();

        // First scan the map for all valid positions
        for (int xx = 0; xx < width; xx++)
        {
            for (int yy = 0; yy < height; yy++)
            {
                if (IsDeepWater(xx, yy) && !IsNearLand(xx, yy, 3))
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
    {
        char tile = mapData[x, y];
        return tile == 'P' || tile == 'F' || tile == 'M' || tile == 'm' || tile == 'S' || tile == 'B' || tile == 'b';
    }
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
    private bool IsShallowWater(int x, int y)
    {
        char tile = mapData[x, y];
        return tile == 'O' || tile == 'L' || tile == 'R';
    }
    private bool IsWaterTile(int x, int y)
    {
        char tile = mapData[x, y];
        return tile == 'O' || tile == 'o' || tile == 'L' || tile == 'l' || tile == 'R' || tile == 'r';
    }
    private bool IsDeepWater(int x, int y)
    {
        char tile = mapData[x, y];
        return tile == 'o' || tile == 'l' || tile == 'r';
    }
    private bool IsThereAWaveTile(int x, int y)
    {
        return waves.Any(w => w.PreviousPoints.Contains((x, y)));
    }
    private (int r, int g, int b) GetWaveColor(int x, int y, double intensity = 1.0)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return (0, 0, 0);
        char tile = mapData[x, y];
        (int r, int g, int b) baseColor = GetColor(tile, x, y);
        (int r, int g, int b) finalColor;

        if (!IsShallowWater(x, y))
        {
            (int r, int g, int b) waveColor = GetColor('O', x, y);
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
    #region weather system
    #region essentials
    public enum WeatherType
    {
        Clear = 1,
        Rain = 2,
        Snow = 3,
        Thunderstorm = 4,
        Fog = 5,
        Overcast = 6,
        Hail = 7,
        Sleet = 8,
        Drizzle = 9,
        BlowingSnow = 10,
        Sandstorm = 11
    }
    public enum CloudType
    {
        Cumulus = 1,
        Stratus = 2,
        Cirrus = 3,
        Cumulonimbus = 4,
        Nimbostratus = 5,
        Altocumulus = 6
    }
    public class Weather
    {
        public WeatherType CurrentWeather { get; set; }
        public WeatherType NextWeather { get; set; }
        public double Intensity { get; set; }
        public double IntensityTarget { get; set; }
        public double IntensityChangeSpeed { get; set; }
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public double Pressure { get; set; }
        public double WindSpeed { get; set; }
        public double WindDirection { get; set; }
        public double TimeOfDay { get; set; }
        public double Season { get; set; }
    }
    public class Cloud
    {
        public int X { get; set; }
        public int Y { get; set; }
        public double Speed { get; set; }
        public double Direction { get; set; }
        public double Precipitation { get; set; }
        public CloudType Type { get; set; }

        public void Move()
        {
            X += (int)(Speed * Math.Cos(Direction));
            Y += (int)(Speed * Math.Sin(Direction));
        }
    }
    public class CloudLayer
    {
        public double BaseAltitude { get; set; }
        public CloudType Type { get; set; }
        public double Coverage { get; set; } // 0-1 cloud coverage
        public bool IsEnabled { get; set; }
    }
    public  Weather weather = new Weather();
    private List<Cloud> clouds = new List<Cloud>();
    private double deltaTime = 0.5;
    #endregion
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
    private static Dictionary<(int, int), (double, double)> cloudPositions = new();
    int cloudFormations;
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

        char[,] newCloudData = new char[cloudDataWidth, cloudDataHeight];
        Dictionary<(int, int), (double, double)> newCloudPositions = new Dictionary<(int, int), (double, double)>();

        List<(int startX, int startY, int endX, int endY)> regions = GetCloudRegions();

        object lockCloudData = new();
        object lockCloudPositions = new();

        Parallel.ForEach(regions, region =>
        {
            (int startX, int startY, int endX, int endY) = region;
            Dictionary<(int, int), (double, double)> localCloudPositions = new Dictionary<(int, int), (double, double)>();
            List<(int x, int y, char value)> localNewCloudData = new List<(int x, int y, char value)>();

            // Process the assigned region
            for (int x = startX; x < endX; x++)
            {
                for (int y = startY; y < endY; y++)
                {
                    if (cloudData[x, y] != '\0')
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
                foreach ((int x, int y, char value) in localNewCloudData)
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
    Dictionary<(int x, int y), int> cloudSizes = new Dictionary<(int x, int y), int>();
    private void SmoothAndFluffClouds()
    {
        char[,] tempCloudData = (char[,])cloudData.Clone();
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
                if (cloudData[x, y] != '\0' && !visited[x, y])
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

                                if (cloudData[newX, newY] == '\0')
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
                char neighborType = '\0';

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

                        if (cloudData[nx, ny] != '\0')
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
                        if (tempCloudData[newX, newY] == '\0')
                        {
                            // Count cloud neighbors
                            int cloudNeighbors = 0;
                            char neighborType = '\0';

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

                                    if (tempCloudData[nnx, nny] != '\0')
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

                                    if (tempCloudData[nnx, nny] != '\0')
                                    {
                                        cloudNeighbors++;
                                    }
                                }
                            }

                            if (cloudNeighbors <= 4 && localRng.NextDouble() < shrinkageProbability)
                            {
                                lock (lockObj)
                                {
                                    tempCloudData[x, y] = '\0';
                                    cloudSizes.Remove((x, y));
                                }
                            }
                        }
                    }
                }
            }
        });

        // Final pass: Smooth out isolated pixels and rough edges
        char[,] finalCloudData = (char[,])tempCloudData.Clone();

        Parallel.ForEach(regions, region =>
        {
            (int startX, int startY, int endX, int endY) = region;

            for (int x = startX; x < endX; x++)
            {
                for (int y = startY; y < endY; y++)
                {
                    if (tempCloudData[x, y] != '\0')
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

                                if (tempCloudData[nx, ny] != '\0')
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
                                finalCloudData[x, y] = '\0';
                                cloudSizes.Remove((x, y));
                            }
                        }
                    }
                }
            }
        });

        cloudData = finalCloudData;
    }
    private int GetCloudSize(int startX, int startY, char[,] cloudData, bool[,] visited)
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

                    if (nx >= 0 && nx < cloudDataWidth && ny >= 0 && ny < cloudDataHeight && cloudData[nx, ny] != '\0' && !visited[nx, ny])
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
                if (!visited[x, y] && cloudData[x, y] != '\0')
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
                        !visited[nx, ny] && cloudData[nx, ny] != '\0')
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
    private void MarkCloudPositions(int startX, int startY, char[,] cloudData, Dictionary<(int x, int y), int> cloudSizes, int cloudSize)
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

                    if (nx >= 0 && nx < cloudDataWidth && ny >= 0 && ny < cloudDataHeight && cloudData[nx, ny] != '\0' && !visited.Contains((nx, ny)))
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
    private void SmoothAndFluffCloudsPass(char[,] tempCloudData)
    {
        List<(int startX, int startY, int endX, int endY)> regions = GetCloudRegions();
        object lockObj = new object();
        Random localRng = new Random(seed);

        Parallel.ForEach(regions, region =>
        {
            (int startX, int startY, int endX, int endY) = region;
            List<(int x, int y, char neighborType)> localNewCloudPositions = new List<(int x, int y, char neighborType)>();

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

                                if (tempCloudData[checkX, checkY] == '\0')
                                {
                                    int cloudNeighbors = 0;
                                    char neighborType = '\0';

                                    for (int ndx = -1; ndx <= 1; ndx++)
                                    {
                                        for (int ndy = -1; ndy <= 1; ndy++)
                                        {
                                            if (ndx == 0 && ndy == 0) continue;
                                            int neighborX = checkX + ndx;
                                            int neighborY = checkY + ndy;
                                            if (neighborX < 0 || neighborX >= cloudDataWidth || neighborY < 0 || neighborY >= cloudDataHeight)
                                                continue;
                                            if (tempCloudData[neighborX, neighborY] != '\0')
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
                foreach ((int x, int y, char neighborType) in localNewCloudPositions)
                {
                    tempCloudData[x, y] = neighborType;
                    cloudPositions[(x, y)] = (x, y);
                }
            }
        });
    }
    private void ApplyJellyEffect()
    {
        char[,] newCloudData = new char[cloudDataWidth, cloudDataHeight];
        Dictionary<(int, int), (double, double)> newCloudPositions = new Dictionary<(int, int), (double, double)>();

        List<(int startX, int startY, int endX, int endY)> regions = GetCloudRegions();

        object lockCloudData = new object();
        object lockCloudPositions = new object();

        Parallel.ForEach(regions, () => new Random(seed), (region, state, localRng) =>
        {
            (int startX, int startY, int endX, int endY) = region;
            Dictionary<(int, int), (double, double)> localPositions = new Dictionary<(int, int), (double, double)>();
            List<(int x, int y, char value)> localCloudData = new List<(int x, int y, char value)>();

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
                                    if (cloudData[neighborX, neighborY] == '\0') // Empty tile
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

                            if (AreCoordsInBounds(intNewX, intNewY) && cloudData[intNewX, intNewY] == '\0')
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
                foreach ((int x, int y, char value) in localCloudData)
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
        if (cloudData[x, y] == '\0') return false;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                int nx = x + dx;
                int ny = y + dy;

                if (nx >= 0 && nx < cloudDataWidth && ny >= 0 && ny < cloudDataHeight)
                {
                    if (cloudData[nx, ny] == '\0')
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
        char[,] newCloudData = (char[,])cloudData.Clone();
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
                        if (cloudData[nx, ny] == '\0')
                        {
                            emptyAdjacentPositions.Add((nx, ny));
                        }
                    }
                }
            }
        }

        foreach ((int x, int y) in emptyAdjacentPositions)
        {
            Dictionary<char, int> surroundingTypes = new Dictionary<char, int>();

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;

                    int nx = x + dx;
                    int ny = y + dy;

                    if (nx >= 0 && nx < cloudDataWidth && ny >= 0 && ny < cloudDataHeight)
                    {
                        char neighborType = cloudData[nx, ny];
                        if (neighborType != '\0')
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
                KeyValuePair<char, int> mostCommonType = surroundingTypes.OrderByDescending(kvp => kvp.Value).First();
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
                if (IsEdgeTile(x, y) && cloudData[x, y] != '\0')
                {
                    cloudData[x, y] = '\0';
                    cloudPositions.Remove((x, y));
                }
            }
        }
    }
    private int CountCloudNeighbors(int x, int y, char[,] cloudData)
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
                if (cloudData[nx, ny] != '\0')
                    count++;
            }
        }
        return count;
    }
    private int CountEmptyNeighbors(int x, int y, char[,] cloudData)
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
                if (cloudData[nx, ny] == '\0')
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
                        if (cloudData[nx, ny] == '\0')
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
    double timeSinceLastCloudSpawn = 0.0;
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
    private bool[,] cloudIsNight;
    private bool[,] cloudIsDarkening;
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
        (int r, int g, int b) baseColor = GetCloudDepthColor(GetCloudType(cloudData[x, y]), cloudDepthData[x, y]);

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
    #region weather state
    public void InitializeWeather()
    {
        int weatherRng = rngMap.Next(0, 2);
        weather.CurrentWeather = weatherRng switch
        {
            0 => WeatherType.Clear,
            1 => WeatherType.Overcast,
            _ => WeatherType.Clear
        };
        weather.NextWeather = GetSeasonWeatherType();
        weather.Intensity = 0.0;
        weather.IntensityTarget = 0.0;
        weather.IntensityChangeSpeed = 0.1;
        weather.TimeOfDay = 12.0;
        weather.Season = rngMap.NextDouble() * 4.0;
        weather.Temperature = GetTemperature(weather.Season, weather.TimeOfDay, weather.CurrentWeather, avarageTempature);
        weather.Humidity = GetHumidity(weather.CurrentWeather, weather.Temperature, weather.TimeOfDay, weather.Season, avarageHumidity, seed);
        weather.Pressure = GetPressure();
        weather.WindSpeed = rng.NextDouble() * 10.0 + 5.0;
        weather.WindDirection = rng.Next(360);
    }
    public void UpdateWeather()
    {
        // Update time of day and season
        weather.TimeOfDay += deltaTime * 24.0 / 720.0; 
        time = weather.TimeOfDay;
        if (weather.TimeOfDay >= 24.0) // One day is 12 minutes
        {
            weather.TimeOfDay -= 24.0;
            dayCount++;
        }

        int daysPerSeason = 5; // Customize the number of days per season
        weather.Season += deltaTime / (daysPerSeason * 720.0); // Season changes every 5 days
        if (weather.Season >= 4.0)
            weather.Season -= 4.0;

        // Smoothly transition intensity
        weather.Intensity += (weather.IntensityTarget - weather.Intensity) * weather.IntensityChangeSpeed * deltaTime;

        // Change weather if necessary
        if (ShouldChangeWeather())
        {
            weather.CurrentWeather = weather.NextWeather;
            weather.NextWeather = GetSeasonWeatherType();
            weather.IntensityTarget = rng.NextDouble();
            InitializeMinTimeBetweenChanges();
        }

        // Update temperature based on season and time of day
        weather.Temperature = GetTemperature(weather.Season, weather.TimeOfDay, weather.CurrentWeather, avarageTempature);

        // Update humidity based on weather type and temperature
        weather.Humidity = GetHumidity(weather.CurrentWeather, weather.Temperature, weather.TimeOfDay, weather.Season, avarageHumidity, seed);

        // Update pressure based on weather conditions
        weather.Pressure = GetPressure();

        // Update wind speed and direction dynamically
        UpdateSunTimes(weather.Season);
        UpdateWind();
        UpdateCloudShadows();
        UpdateTime();
        UpdateSeason();
    }
    public static void UpdateSunTimes(double season)
    {
        // Define the exact season values for solstices
        double summerSolstice = 1.8;
        double winterSolstice = 3.9;

        // Calculate the progression between solstices
        double progress;
        double sunriseOffset, sunsetOffset;

        if (season <= summerSolstice)
        {
            // From Spring to Summer Solstice
            progress = (season - 0.0) / (summerSolstice - 0.0);
        }
        else
        {
            // From Summer Solstice to Winter Solstice
            progress = (season - summerSolstice) / (winterSolstice - summerSolstice);
        }

        // Use a cosine function to ensure alignment at solstices
        sunriseOffset = Math.Cos(progress * Math.PI) * (winterSolsticeSunrise - equinoxSunrise);
        sunsetOffset = Math.Cos(progress * Math.PI) * (winterSolsticeSunset - equinoxSunset);

        // Update sunrise and sunset times
        sunriseTime = Math.Round(equinoxSunrise + sunriseOffset, 2);
        sunsetTime = Math.Round(equinoxSunset + sunsetOffset, 2);

        // Ensure exact times at solstices
        if (Math.Abs(season - summerSolstice) < 0.01)
        {
            sunriseTime = summerSolsticeSunrise;
            sunsetTime = summerSolsticeSunset;
        }
        else if (Math.Abs(season - winterSolstice) < 0.01)
        {
            sunriseTime = winterSolsticeSunrise;
            sunsetTime = winterSolsticeSunset;
        }
    }
    private double timeSinceLastWeatherChange = 0.0;

    // Average times for sunrise and sunset at equinoxes and solstices (in hours)
    private static readonly double equinoxSunrise = 6.0;
    private static readonly double equinoxSunset = 18.0;
    private static readonly double summerSolsticeSunrise = 5.0;
    private static readonly double summerSolsticeSunset = 21.0;
    private static readonly double winterSolsticeSunrise = 7.0;
    private static readonly double winterSolsticeSunset = 17.0;
    private int dayCount;
    double minTimeBetweenChanges;
    private WeatherType GetSeasonWeatherType()
    {
        double temperature = weather.Temperature;
        double humidity = weather.Humidity;
        double pressure = weather.Pressure;
        double windSpeed = weather.WindSpeed;
        double windDirection = weather.WindDirection;
        double timeOfDay = weather.TimeOfDay;
        double season = weather.Season;

        WeatherType weatherType = WeatherType.Clear;

        // Initialize weather probabilities
        Dictionary<WeatherType, double> weatherProbabilities = new Dictionary<WeatherType, double>()
        {
            { WeatherType.Clear, 0.3 },
            { WeatherType.Rain, 0.1 },
            { WeatherType.Snow, 0.1 },
            { WeatherType.Thunderstorm, 0.05 },
            { WeatherType.Fog, 0.05 },
            { WeatherType.Overcast, 0.1 },
            { WeatherType.Hail, 0.05 },
            { WeatherType.Sleet, 0.05 },
            { WeatherType.Drizzle, 0.1 },
            { WeatherType.BlowingSnow, 0.05 },
            { WeatherType.Sandstorm, 0.05 }
        };

        // Adjust probabilities based on temperature
        if (temperature < 0)
        {
            weatherProbabilities[WeatherType.Snow] += 0.3;
            weatherProbabilities[WeatherType.BlowingSnow] += 0.1;
            weatherProbabilities[WeatherType.Rain] -= 0.1;
            weatherProbabilities[WeatherType.Thunderstorm] -= 0.05;
        }
        else if (temperature > 25)
        {
            weatherProbabilities[WeatherType.Thunderstorm] += 0.1;
            weatherProbabilities[WeatherType.Sandstorm] += 0.1;
        }
        else if (temperature > 15)
        {
            weatherProbabilities[WeatherType.Rain] += 0.1;
            weatherProbabilities[WeatherType.Thunderstorm] += 0.05;
        }

        // Adjust probabilities based on humidity
        if (humidity > 80)
        {
            weatherProbabilities[WeatherType.Rain] += 0.2;
            weatherProbabilities[WeatherType.Drizzle] += 0.1;
            weatherProbabilities[WeatherType.Fog] += 0.1;
            weatherProbabilities[WeatherType.Overcast] += 0.1;
        }
        else if (humidity < 30)
        {
            weatherProbabilities[WeatherType.Clear] += 0.1;
            weatherProbabilities[WeatherType.Sandstorm] += 0.1;
        }

        // Adjust probabilities based on pressure
        if (pressure < 1000)
        {
            weatherProbabilities[WeatherType.Rain] += 0.1;
            weatherProbabilities[WeatherType.Thunderstorm] += 0.1;
            weatherProbabilities[WeatherType.Hail] += 0.05;
            weatherProbabilities[WeatherType.Overcast] += 0.1;
        }
        else if (pressure > 1020)
        {
            weatherProbabilities[WeatherType.Clear] += 0.2;
        }

        // Adjust probabilities based on season
        if (season >= 0.0 && season < 1.0) // Spring
        {
            weatherProbabilities[WeatherType.Rain] += 0.1;
            weatherProbabilities[WeatherType.Drizzle] += 0.1;
        }
        else if (season >= 1.0 && season < 2.0) // Summer
        {
            weatherProbabilities[WeatherType.Thunderstorm] += 0.1;
            weatherProbabilities[WeatherType.Clear] += 0.1;
        }
        else if (season >= 2.0 && season < 3.0) // Autumn
        {
            weatherProbabilities[WeatherType.Overcast] += 0.1;
            weatherProbabilities[WeatherType.Fog] += 0.1;
        }
        else // Winter
        {
            weatherProbabilities[WeatherType.Snow] += 0.2;
            weatherProbabilities[WeatherType.BlowingSnow] += 0.1;
            weatherProbabilities[WeatherType.Sleet] += 0.1;
        }

        // Adjust probabilities based on current weather
        switch (weather.CurrentWeather)
        {
            case WeatherType.Rain:
                weatherProbabilities[WeatherType.Thunderstorm] += 0.2;
                weatherProbabilities[WeatherType.Rain] += 0.1;
                weatherProbabilities[WeatherType.Drizzle] += 0.05;
                break;
            case WeatherType.Thunderstorm:
                weatherProbabilities[WeatherType.Rain] += 0.1;
                weatherProbabilities[WeatherType.Clear] += 0.05;
                break;
            case WeatherType.Clear:
                weatherProbabilities[WeatherType.Clear] += 0.1;
                weatherProbabilities[WeatherType.Rain] += 0.05;
                break;
            case WeatherType.Overcast:
                weatherProbabilities[WeatherType.Rain] += 0.1;
                weatherProbabilities[WeatherType.Clear] += 0.05;
                break;
            case WeatherType.Snow:
                weatherProbabilities[WeatherType.Snow] += 0.2;
                weatherProbabilities[WeatherType.BlowingSnow] += 0.1;
                break;
            case WeatherType.BlowingSnow:
                weatherProbabilities[WeatherType.Snow] += 0.1;
                weatherProbabilities[WeatherType.BlowingSnow] += 0.1;
                break;
            case WeatherType.Fog:
                weatherProbabilities[WeatherType.Fog] += 0.1;
                weatherProbabilities[WeatherType.Clear] += 0.05;
                break;
            // Add more cases as needed
        }

        // Ensure probabilities are not negative
        foreach (WeatherType key in weatherProbabilities.Keys.ToList())
        {
            if (weatherProbabilities[key] < 0)
                weatherProbabilities[key] = 0;
        }

        // Normalize probabilities
        double totalProbability = weatherProbabilities.Values.Sum();
        if (totalProbability == 0)
        {
            // If totalProbability is zero after adjustments, assign default probabilities
            weatherProbabilities = new Dictionary<WeatherType, double>()
            {
                { WeatherType.Clear, 0.3 },
                { WeatherType.Rain, 0.1 },
                { WeatherType.Snow, 0.1 },
                { WeatherType.Thunderstorm, 0.05 },
                { WeatherType.Fog, 0.05 },
                { WeatherType.Overcast, 0.1 },
                { WeatherType.Hail, 0.05 },
                { WeatherType.Sleet, 0.05 },
                { WeatherType.Drizzle, 0.1 },
                { WeatherType.BlowingSnow, 0.05 },
                { WeatherType.Sandstorm, 0.05 }
            };
            totalProbability = weatherProbabilities.Values.Sum();
        }

        // Generate random number to select weather type
        double rand = rng.NextDouble() * totalProbability;
        double cumulative = 0.0;

        foreach (KeyValuePair<WeatherType, double> pair in weatherProbabilities)
        {
            cumulative += pair.Value;
            if (rand <= cumulative)
            {
                weatherType = pair.Key;
                break;
            }
        }

        return weatherType;
    }
    private void InitializeMinTimeBetweenChanges()
    {
        minTimeBetweenChanges = weather.CurrentWeather switch
        {
            WeatherType.Clear => 95.0,
            WeatherType.Rain => 125.0,
            WeatherType.Snow => 255.0,
            WeatherType.Thunderstorm => 260.0,
            WeatherType.Fog => 140.0,
            WeatherType.Overcast => 90.0,
            WeatherType.Hail => 90.0,
            WeatherType.Sleet => 120.0,
            WeatherType.Drizzle => 105.0,
            WeatherType.BlowingSnow => 95.0,
            WeatherType.Sandstorm => 65.0,
            _ => 115.0
        };
    }
    private bool ShouldChangeWeather()
    {
        // Increment the timer
        timeSinceLastWeatherChange += deltaTime;

        if (timeSinceLastWeatherChange < minTimeBetweenChanges)
        {
            return false;
        }

        // Probability-based weather change
        double changeProbability = 0.01 * deltaTime * 10;
        if (rng.NextDouble() < changeProbability)
        {
            timeSinceLastWeatherChange = 0.0;
            return true;
        }

        return false;
    }
    private WeatherType GetRandomWeatherType()
    {
        Array values = Enum.GetValues(typeof(WeatherType));
        object? value = values.GetValue(rng.Next(values.Length));
        return value != null ? (WeatherType)value : WeatherType.Clear;
    }
    static double weatherFactor = 40;
    private static double GetHumidity(WeatherType weatherType, double tempature, double timeOfDay, double season, double avarageHumidity, int seed)
    {
        double seasonFactor = Math.Cos(season / 2.0 * Math.PI);
        double timeFactor = Math.Cos(timeOfDay / 24.0 * 2 * Math.PI);
        double tempatureFactor = Math.Cos(tempature / 40.0 * Math.PI);
        Random rng = new Random(seed);
        double humidity;
        double weather = weatherType switch
        {
            WeatherType.Clear => 50.0,
            WeatherType.Rain => 60.0,
            WeatherType.Snow => 50.0,
            WeatherType.Thunderstorm => 70.0,
            WeatherType.Fog => 75.0,
            WeatherType.Overcast => 70.0,
            WeatherType.Hail => 60.0,
            WeatherType.Sleet => 50.0,
            WeatherType.Drizzle => 55.0,
            WeatherType.BlowingSnow => 40.0,
            WeatherType.Sandstorm => 40.0,
            _ => 50.0
        };
        weather += (avarageHumidity - 0.5) * 10;
        double weatherTimer = weatherType switch
        {
            WeatherType.Clear => rng.NextDouble() * 2.0,
            WeatherType.Rain => rng.NextDouble() * 3.5,
            WeatherType.Snow => rng.NextDouble() * 3.0,
            WeatherType.Thunderstorm => rng.NextDouble() * 4.0,
            WeatherType.Fog => rng.NextDouble() * 4.5,
            WeatherType.Overcast => rng.NextDouble() * 3.0,
            WeatherType.Hail => rng.NextDouble() * 3.0,
            WeatherType.Sleet => rng.NextDouble() * 3.0,
            WeatherType.Drizzle => rng.NextDouble() * 3.0,
            WeatherType.BlowingSnow => rng.NextDouble() * 3.0,
            WeatherType.Sandstorm => rng.NextDouble() * 3.0,
            _ => 3.0
        };
        if (weatherFactor < weather)
        {
            weatherFactor += weatherTimer;
        }
        else if (weatherFactor > weather + 1)
        {
            weatherFactor -= weatherTimer;
        }

        humidity = weatherFactor + seasonFactor + timeFactor + tempatureFactor;
    
        // Ensure humidity stays within realistic bounds (0% - 100%)
        humidity = Math.Clamp(humidity, 0.0, 100.0);
    
        return humidity;
    }
    private double GetPressure()
    {
        // Base atmospheric pressure in hPa
        double basePressure = 1013.25;
        double pressure;

        // Seasonal adjustments with smooth transitions
        double[] seasonAdjustments = new double[] { 1.02, 0.98, 1.01, 1.03, 1.02 }; // Spring, Summer, Autumn, Winter, Spring
        int currentSeasonIndex = (int)Math.Floor(weather.Season) % 4;
        int nextSeasonIndex = (currentSeasonIndex + 1) % 4;
        double seasonFraction = weather.Season - Math.Floor(weather.Season);

        double seasonAdjustment = seasonAdjustments[currentSeasonIndex] * (1 - seasonFraction) +
                                seasonAdjustments[nextSeasonIndex] * seasonFraction;

        // Time of day adjustments (higher pressure at night)
        double timeAdjustment = 1.0 + 0.005 * Math.Cos(weather.TimeOfDay / 24.0 * 2 * Math.PI);

        // Weather type adjustments
        double weatherAdjustment = weather.CurrentWeather switch
        {
            WeatherType.Clear => 1.01,
            WeatherType.Rain => 0.99,
            WeatherType.Snow => 0.98,
            WeatherType.Thunderstorm => 0.95,
            WeatherType.Fog => 1.00,
            WeatherType.Overcast => 0.97,
            WeatherType.Hail => 0.96,
            WeatherType.Sleet => 0.95,
            WeatherType.Drizzle => 0.98,
            WeatherType.BlowingSnow => 0.94,
            WeatherType.Sandstorm => 0.93,
            _ => 1.0
        };

        // Calculate dynamic pressure
        pressure = basePressure * seasonAdjustment * timeAdjustment * weatherAdjustment;

        // Introduce minor random fluctuations for realism
        pressure += (rng.NextDouble() - 0.5) * 0.5; // ±0.25 hPa
        return pressure;
    }
    private static double GetTemperature(double season, double timeOfDay, WeatherType weatherType, double avarageTempature)
    {
        // Normalize the season value between 0 and 4
        season %= 4.0;

        // Define temperatures at key points for each season (in degrees Celsius)
        // Index 0: Start of Spring, 1: Start of Summer, 2: Start of Autumn, 3: Start of Winter, 4: Wrap back to Spring
        // Determine temperature zones
        int tempZone = avarageTempature switch
        {
            < 0.0 => throw new ArgumentOutOfRangeException(nameof(avarageTempature), "Temperature cannot be negative."),
            < 0.1 => 1, // Very Cold
            < 0.3 => 2, // Cold
            < 0.5 => 3, // Cool
            < 0.7 => 4, // Temperate
            _ => 5,      // Warm
        };

        // Define temperatures at key points for each season (in degrees Celsius)
        double[] seasonTemperatures = tempZone switch
        {
            1 => new double[] { -10.0, 0.0, -5.0, -20.0, -10.0 }, // Very Cold
            2 => new double[] { 0.0, 10.0, 5.0, -5.0, 0.0 }, // Cold
            3 => new double[] { 10.0, 20.0, 15.0, 5.0, 10.0 }, // Cool
            4 => new double[] { 15.0, 25.0, 20.0, 10.0, 15.0 }, // Temperate
            5 => new double[] { 20.0, 30.0, 25.0, 15.0, 20.0 }, // Warm
            _ => new double[] { 10.0, 25.0, 15.0, 0.0, 10.0 } // Default
        };

        // Get the current season index and the fraction within that season
        int seasonIndex = (int)Math.Floor(season);
        double seasonProgress = season - seasonIndex;

        // Get temperatures at the start and end of the current season
        double tempStart = seasonTemperatures[seasonIndex];
        double tempEnd = seasonTemperatures[seasonIndex + 1];

        // Smoothly interpolate the base temperature between seasons
        double baseTemp = tempStart + (tempEnd - tempStart) * seasonProgress;

        // Adjust temperature based on time of day (warmer during the day, cooler at night)
        // Shift the time to peak at 14 hours
        double dayTemperatureVariation = Math.Sin((timeOfDay - 8.0) / 24.0 * 2 * Math.PI) * 8.0;
        baseTemp += dayTemperatureVariation;

        // Adjust temperature based on current weather conditions
        double weatherAdjustment = weatherType switch
        {
            WeatherType.Thunderstorm => -2.0,
            WeatherType.Rain => -1.0,
            WeatherType.Snow => -3.0,
            WeatherType.Sleet => -1.5,
            WeatherType.Overcast => -0.5,
            WeatherType.Clear => 1.0,
            WeatherType.Fog => -0.2,
            WeatherType.Hail => -1.5,
            WeatherType.Drizzle => -0.5,
            WeatherType.BlowingSnow => -2.0,
            WeatherType.Sandstorm => 1.5,
            _ => 0.0
        };
        baseTemp += weatherAdjustment;
        
        // Integrate the average temperature to make a realistic temperature based around it
        int avarageTempatureFactor = avarageTempature switch
        {
            < 0.0 => throw new ArgumentOutOfRangeException(nameof(avarageTempature), "Temperature cannot be negative."),
            < 0.1 => 45, // Very Cold
            < 0.3 => 35, // Cold
            < 0.5 => 30, // Cool
            < 0.7 => 35, // Temperate
            _ => 35,      // Warm
        };
        baseTemp += (avarageTempature - 0.5) * avarageTempatureFactor; // Adjust the factor as needed to influence the temperature
        
        return baseTemp;
    }
    private void UpdateWind()
    {
        windChangeTimer += deltaTime;

        if (windChangeTimer >= minWindChangeInterval && !isTurning)
        {
            // Decide whether to change wind direction
            double changeChance = deltaTime / (maxWindChangeInterval - minWindChangeInterval);
            if (rng.NextDouble() < changeChance)
            {
                // Start turning
                isTurning = true;
                windChangeTimer = 0.0;
                // Choose a random angle to turn, limited to -20 to 20 degrees
                int turn = rng.Next(-20, 21); // Random integer between -20 and 20 inclusive
                windTargetDirection = (weather.WindDirection + turn + 360) % 360;
            }
        }

        if (isTurning)
        {
            // Calculate the smallest difference
            double difference = windTargetDirection - weather.WindDirection;
            difference = (difference + 180) % 360 - 180;

            // Determine the direction to turn
            double turnDirection = difference > 0 ? 1 : -1;

            // Apply a smooth turn by 1 degree per update
            double turnAmount = 1.0;

            if (Math.Abs(difference) <= turnAmount)
            {
                weather.WindDirection = windTargetDirection;
                isTurning = false;
                windChangeTimer = 0.0;
            }
            else
            {
                weather.WindDirection += turnDirection * turnAmount;
                // Ensure degrees are integers
                weather.WindDirection = Math.Round(weather.WindDirection);
                weather.WindDirection = (weather.WindDirection + 360) % 360;
            }
        }

        // Calculate base wind speed
        double baseWindSpeed = GetBaseWindSpeed();

        // Adjust wind speed based on time of day, season, and weather
        double timeOfDayFactor = GetTimeOfDayWindFactor();
        double seasonFactor = GetSeasonWindFactor();
        double weatherFactor = GetWeatherWindFactor();

        weather.WindSpeed = baseWindSpeed * timeOfDayFactor * seasonFactor * weatherFactor;

        // Calculate pressure gradient influence
        double pressureGradient = GetPressureGradient();
        double gradientFactor = 0.05; // Adjust for realism

        // Calculate temperature influence on wind
        double temperatureGradient = GetTemperatureGradient();
        double temperatureFactor = 0.03; // Adjust for realism

        // Update wind speed based on pressure and temperature gradients
        double windSpeedChange = (pressureGradient * gradientFactor) + (temperatureGradient * temperatureFactor);
        weather.WindSpeed += windSpeedChange * deltaTime;

        // Clamp wind speed to realistic bounds
        weather.WindSpeed = Math.Clamp(weather.WindSpeed, 0.0, 40.0);
    }
    private double GetBaseWindSpeed()
    {
        return 10.0; // Base wind speed
    }
    private double GetBaseWindDirection()
    {
        return Math.PI / 2; // Base wind direction (East)
    }
    private double GetTimeOfDayWindFactor()
    {
        // Assume stronger winds during midday due to thermal currents
        double time = weather.TimeOfDay;
        double factor = 1.0 + 0.5 * Math.Sin((time / 24.0) * 2 * Math.PI);
        return factor; // Varies between 0.5 and 1.5
    }
    private double GetSeasonWindFactor()
    {
        double season = weather.Season;
        // Seasons are represented as 0.0 to 4.0 (0 to less than 1 is Spring, etc.)
        if (season >= 0.0 && season < 1.0) // Spring
            return 1.0;
        else if (season >= 1.0 && season < 2.0) // Summer
            return 1.2;
        else if (season >= 2.0 && season < 3.0) // Autumn
            return 0.9;
        else // Winter
            return 0.8;
    }
    private double GetWeatherWindFactor()
    {
        switch (weather.CurrentWeather)
        {
            case WeatherType.Thunderstorm:
                return 1.5;
            case WeatherType.Rain:
                return 1.2;
            case WeatherType.Snow:
                return 1.1;
            case WeatherType.Fog:
                return 0.7;
            case WeatherType.Clear:
                return 1.0;
            default:
                return 1.0;
        }
    }
    private double GetWeatherWindDirectionChange()
    {
        switch (weather.CurrentWeather)
        {
            case WeatherType.Thunderstorm:
                return (rng.NextDouble() - 0.5) * (Math.PI / 4); // Random change up to ±22.5 degrees
            case WeatherType.Sandstorm:
                return Math.PI; // Winds blow from the opposite direction during sandstorms
            default:
                return 0.0;
        }
    }
    private double minWindChangeInterval = 30.0; // minimum interval in seconds
    private double maxWindChangeInterval = 90.0; // maximum interval in seconds
    private double windChangeTimer;
    private double windTargetDirection;
    private bool isTurning = false;

    // Helper method to calculate pressure gradient
    private double GetPressureGradient()
    {
        // Example: Simple gradient based on neighboring pressure
        double gradient = 0.0;
        // Implement actual pressure gradient calculation based on map data
        return gradient;
    }
    private double GetTemperatureGradient()
    {
        // Example: Simple gradient based on temperature differences
        double gradient = 0.0;
        // Implement actual temperature gradient calculation based on map data
        return gradient;
    }
    private double CalculateWindDirectionChange(double pressureGradient, double temperatureGradient)
    {
        // Example: Change direction based on pressure and temperature gradients
        double directionChange = 0.0;
        // Implement actual logic to adjust wind direction
        return directionChange;
    }
    private double GetAltitudeWindFactor(double altitude)
    {
        return 1.0 + (altitude / 10000.0) * 0.5;
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
    public void RenderClouds()
    {
        CloudsDepth();

        // Remove clouds from previous positions that are no longer clouds
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                (int r, int g, int b) finalColor;
                char tile = mapData[x, y];
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
                    bool wasCloud = previousCloudData[cloudX, cloudY] != '\0';
                    bool isCloud = cloudData[cloudX, cloudY] != '\0';

                    if (wasCloud && !isCloud)
                    {
                        if (IsThereAnOverlayTile(x, y)) UpdateOverlayTile(x, y);
                        else
                        {
                            Console.SetCursorPosition(leftPadding + x * 2, y + topPadding);
                            Console.Write(SetBackgroundColor(finalColor.r, finalColor.g, finalColor.b) + "  " + ResetColor());
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
                if (IsInCloudBounds(cloudX, cloudY) && cloudData[cloudX, cloudY] != '\0')
                {
                    int depth = cloudDepthData[cloudX, cloudY];
                    CloudType cloudType = GetCloudType(cloudData[cloudX, cloudY]);
                    //var cloudColor = GetCloudDepthColor(cloudType, depth);
                    (int r, int g, int b) cloudColor = GetCloudColor(cloudX, cloudY);
                    Console.SetCursorPosition(leftPadding + x * 2, y + topPadding);
                    Console.Write(SetBackgroundColor(cloudColor.r, cloudColor.g, cloudColor.b) + "  " + ResetColor());
                }
            }
        }

        // Update previous cloud data
        Array.Copy(cloudData, previousCloudData, cloudData.Length);
    }
    private HashSet<(int x, int y)> previousShadowPositions = new HashSet<(int x, int y)>();
    private readonly Dictionary<(int x, int y), double> currentShadowPositions = new();
    private readonly int shadowRadius = 3;
    private static double shadowIntensityFactor;
    public void DisplayCloudShadows()
    {
        currentShadowPositions.Clear();
        // Calculate shadow positions and intensities
        for (int x = 0; x < cloudDataWidth; x++)
        {
            for (int y = 0; y < cloudDataHeight; y++)
            {
                if (cloudData[x, y] != '\0')
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
                    UpdateTile(pos.x, pos.y);
                    UpdateOverlayTile(pos.x, pos.y);
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
                        Console.SetCursorPosition(leftPadding + pos.x * 2, pos.y + topPadding);
                        if (IsThereAnOverlayTile(pos.x, pos.y))
                        {
                            (int r, int g, int b) overlayColor = GetOverlayColor(overlayData[pos.x, pos.y]);

                            // Apply shadow intensity to overlay color as well
                            int or = Math.Max(0, overlayColor.r - shadowFactor);
                            int og = Math.Max(0, overlayColor.g - shadowFactor);
                            int ob = Math.Max(0, overlayColor.b - shadowFactor);

                            string background = SetBackgroundColor(r, g, b);
                            string foreground = SetForegroundColor(or, og, ob);
                            Console.Write(background + foreground + GetSpeciesIcon(overlayData[pos.x, pos.y]) + ResetColor());
                        }
                        else
                        {
                            Console.Write(SetBackgroundColor(r, g, b) + "  " + ResetColor());
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

                    if (shadowFactor > 0)
                    {
                        Console.SetCursorPosition(leftPadding + pos.x * 2, pos.y + topPadding);
                        if (IsThereAnOverlayTile(pos.x, pos.y))
                        {
                            (int r, int g, int b) overlayColor = GetOverlayColor(overlayData[pos.x, pos.y]);

                            // Apply shadow intensity to overlay color as well
                            int or = Math.Max(0, overlayColor.r - shadowFactor);
                            int og = Math.Max(0, overlayColor.g - shadowFactor);
                            int ob = Math.Max(0, overlayColor.b - shadowFactor);

                            string background = SetBackgroundColor(r, g, b);
                            string foreground = SetForegroundColor(or, og, ob);
                            Console.Write(background + foreground + GetSpeciesIcon(overlayData[pos.x, pos.y]) + ResetColor());
                        }
                        else
                        {
                            Console.Write(SetBackgroundColor(r, g, b) + "  " + ResetColor());
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
                    if (cloudData[nx, ny] != '\0')
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
    private static double timeOfDay;
    private static double season;
    private void UpdateTime()
    {
        timeOfDay = weather.TimeOfDay;
    }
    private void UpdateSeason()
    {
        season = weather.Season;
    }
    private static void UpdateCloudShadows()
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

        int maxPoints = rngMap.Next(minMaxPoints, maxMaxPoints + 1);
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
                    CloudType.Cirrus => rngMap.Next(2, 4),
                    CloudType.Altocumulus => rngMap.Next(3, 6),
                    CloudType.Cumulus => rngMap.Next(4, 7),
                    CloudType.Cumulonimbus => rngMap.Next(7, 11),
                    CloudType.Nimbostratus => rngMap.Next(6, 9),
                    CloudType.Stratus => rngMap.Next(2, 4),
                    _ => rngMap.Next(3, 6)
                });
            }
        }
    }
    private void AddImperfectionsOnCloudEdges(char[,] cloudData, CloudType type)
    {
        for (int x = 1; x < cloudDataWidth - 1; x++)
        {
            for (int y = 1; y < cloudDataHeight - 1; y++)
            {
                if (cloudData[x, y] != '\0')
                {
                    int cloudDensity = 0;
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            if (cloudData[x + dx, y + dy] != '\0')
                            {
                                cloudDensity++;
                            }
                        }
                    }

                    if (cloudDensity < 4)
                    {
                        if (rngMap.NextDouble() > 0.5)
                        {
                            cloudData[x, y] = '\0';
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
                    cloudData[nx, ny] = GetCloudSymbol(type);
                }
            }
        }
    }
    private void CreateFluffyCloudEdges(char[,] tempCloudData, CloudType type)
    {
        char cloudSymbol = GetCloudSymbol(type);
        for (int x = 2; x < cloudDataWidth - 2; x++)
        {
            for (int y = 2; y < cloudDataHeight - 2; y++)
            {
                if (tempCloudData[x, y] == cloudSymbol)
                {
                    int cloudDensity = 0;
                    for (int dx = -2; dx <= 2; dx++)
                    {
                        for (int dy = -2; dy <= 2; dy++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            if (tempCloudData[x + dx, y + dy] == cloudSymbol)
                            {
                                cloudDensity++;
                            }
                        }
                    }

                    if (cloudDensity < 6)
                    {
                        if (rng.NextDouble() > 0.5)
                        {
                            tempCloudData[x, y] = '\0';
                        }
                    }
                    else if (cloudDensity > 8)
                    {
                        if (rng.NextDouble() > 0.3)
                        {
                            tempCloudData[x, y] = cloudSymbol;
                        }
                    }
                }
            }
        }
    }
    private void SmoothCloudShape(char[,] tempCloudData, CloudType type)
    {
        char cloudSymbol = GetCloudSymbol(type);
        for (int x = 1; x < cloudDataWidth - 1; x++)
        {
            for (int y = 1; y < cloudDataHeight - 1; y++)
            {
                if (tempCloudData[x, y] == cloudSymbol)
                {
                    int cloudDensity = 0;
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            if (tempCloudData[x + dx, y + dy] == cloudSymbol)
                            {
                                cloudDensity++;
                            }
                        }
                    }

                    if (cloudDensity < 4)
                    {
                        tempCloudData[x, y] = '\0';
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
                        if (cloudData[nx, ny] == '\0')
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
                if (cloudData[x, y] != '\0')
                {
                    CloudType cloudType = GetCloudType(cloudData[x, y]);
                    cloudDepthData[x, y] = CalculateCloudDepth(x, y, cloudType);
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
        char[,] tempCloudData = (char[,])cloudData.Clone();

        for (int x = 1; x < cloudDataWidth - 1; x++)
        {
            for (int y = 1; y < cloudDataHeight - 1; y++)
            {
                if (cloudData[x, y] == '\0') continue;

                int filledNeighbors = 0;

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        if (cloudData[x + dx, y + dy] != '\0')
                            filledNeighbors++;
                    }
                }

                // Apply smoothing rules
                if (filledNeighbors < 2 || filledNeighbors > 6)
                {
                    tempCloudData[x, y] = '\0';
                }
                else
                {
                    // Optionally, enhance fluffiness by adding more filled neighbors
                    if (filledNeighbors > 4 && cloudData[x, y] == '\0')
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
    #region dayNight cycle
    private HashSet<(int x, int y)> darkenedPositions = new HashSet<(int x, int y)>();
    private Dictionary<(int x, int y), int> darkenedPositionsIntensities = new Dictionary<(int x, int y), int>();
    public enum GradientDirection
    {
        TL_BR, // Top Left to Bottom Right
        BR_TL, // Bottom Right to Top Left
        BL_TR, // Bottom Left to Top Right
        TR_BL  // Top Right to Bottom Left
    }
    public GradientDirection CurrentGradientDirection { get; set; } = GradientDirection.TL_BR;
    public void UpdateGradientDirection(double timeOfDay)
    {
        if (timeOfDay == 0.0)
        {
            CurrentGradientDirection = GradientDirection.TL_BR;
        }
        else if (timeOfDay == 12.0)
        {
            CurrentGradientDirection = GradientDirection.TL_BR;
        }
    }
    public void DisplayDayNightTransition()
    {
        // Determine the current time and calculate transition progress
        double transitionProgress = GetTransitionProgress();

        // Clamp transitionProgress to stay within [0,1]
        transitionProgress = Math.Clamp(transitionProgress, 0.0, 1.0);

        // Use an easing function to simulate smooth transition
        double easedProgress = EaseInOutQuad(transitionProgress);

        // Increase maximum shadow intensity to make the effect noticeable
        double maxShadowIntensity = 50.0;

        // Define the width of the gradient transition (adjusted for complete coverage)
        double gradientWidth = 0.3;

        // Temporary list to track tiles to remove from darkened positions
        List<(int x, int y)> tilesToRemove = new List<(int x, int y)>();

        // Update only the tiles that need to be updated, excluding edges
        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                // Calculate normalized distance based on selected gradient direction
                double normalizedDistance = CalculateNormalizedDistance(x, y);

                // Calculate shadow progress with a smooth gradient
                double tileShadowProgress = Math.Clamp((easedProgress - normalizedDistance + gradientWidth) / gradientWidth, 0, 1);

                // Calculate current shadow intensity for this tile
                int tileShadowIntensity = (int)(tileShadowProgress * maxShadowIntensity);

                if (tileShadowIntensity > 0)
                {
                    // If the tile is already darkened, update its intensity if it has changed
                    if (darkenedPositionsIntensities.TryGetValue((x, y), out int currentIntensity))
                    {
                        if (currentIntensity != tileShadowIntensity)
                        {
                            darkenedPositionsIntensities[(x, y)] = tileShadowIntensity;
                            darkenedPositions.Add((x, y));

                            // Update tile with new shadow intensity
                            if ((isCloudsRendering && !IsTileUnderCloud(x, y)) || !isCloudsRendering) UpdateTileShadow(x, y, tileShadowIntensity);
                        }
                    }
                    else
                    {
                        // Add new darkened tile
                        darkenedPositionsIntensities[(x, y)] = tileShadowIntensity;
                        darkenedPositions.Add((x, y));

                        // Update tile with shadow
                        if ((isCloudsRendering && !IsTileUnderCloud(x, y)) || !isCloudsRendering) UpdateTileShadow(x, y, tileShadowIntensity);
                    }
                }
                else
                {
                    // Remove tiles that no longer have shadow
                    if (darkenedPositionsIntensities.ContainsKey((x, y)))
                    {
                        tilesToRemove.Add((x, y));
                    }
                }
            }
        }

        // Remove tiles that no longer have shadow
        foreach ((int x, int y) tile in tilesToRemove)
        {
            darkenedPositionsIntensities.Remove(tile);
            darkenedPositions.Remove(tile);

            // Reset tile color
            ResetTileColor(tile.x, tile.y);
        }
    }
    private void UpdateTileShadow(int x, int y, int shadowIntensity)
    {
        // Get the base color of the tile
        (int r, int g, int b) baseColor = GetTileBaseColor(x, y);

        // Apply the shadow intensity
        int r = Math.Max(0, baseColor.r - shadowIntensity);
        int g = Math.Max(0, baseColor.g - shadowIntensity);
        int b = Math.Max(0, baseColor.b - shadowIntensity);

        // Update tile with new color
        Console.SetCursorPosition(leftPadding + x * 2, y + topPadding);

        if (IsThereAnOverlayTile(x, y))
        {
            (int r, int g, int b) overlayColor = GetOverlayColor(overlayData[x, y]);

            // Apply shadow intensity to overlay color as well
            int or = Math.Max(0, overlayColor.r - shadowIntensity);
            int og = Math.Max(0, overlayColor.g - shadowIntensity);
            int ob = Math.Max(0, overlayColor.b - shadowIntensity);

            string background = SetBackgroundColor(r, g, b);
            string foreground = SetForegroundColor(or, og, ob);
            Console.Write(background + foreground + GetSpeciesIcon(overlayData[x, y]) + ResetColor());
        }
        else
        {
            string background = SetBackgroundColor(r, g, b);
            Console.Write(background + "  " + ResetColor());
        }
    }
    private void ResetTileColor(int x, int y)
    {
        // Get the base color of the tile
        (int r, int g, int b) baseColor = GetTileBaseColor(x, y);

        // Update tile with base color
        Console.SetCursorPosition(leftPadding + x * 2, y + topPadding);
        string background = SetBackgroundColor(baseColor.r, baseColor.g, baseColor.b);
        Console.Write(background + "  " + ResetColor());

        if (IsThereAnOverlayTile(x, y))
        {
            (int r, int g, int b) overlayColor = GetOverlayColor(overlayData[x, y]);
            string foreground = SetForegroundColor(overlayColor.r, overlayColor.g, overlayColor.b);
            Console.SetCursorPosition(leftPadding + x * 2, y + topPadding);
            Console.Write(background + foreground + $"{overlayData[x, y]}" + ResetColor());
        }
    }
    private double CalculateNormalizedDistance(int x, int y)
    {
        double dx = 0;
        double dy = 0;

        // Offset to move the gradient origin outside the display
        int gradientOffset = 50; // Adjust this value as needed

        switch (CurrentGradientDirection)
        {
            case GradientDirection.TL_BR:
                dx = x + gradientOffset;
                dy = y + gradientOffset;
                break;
            case GradientDirection.BR_TL:
                dx = (width - x) + gradientOffset;
                dy = (height - y) + gradientOffset;
                break;
            case GradientDirection.BL_TR:
                dx = x + gradientOffset;
                dy = (height - y) + gradientOffset;
                break;
            case GradientDirection.TR_BL:
                dx = (width - x) + gradientOffset;
                dy = y + gradientOffset;
                break;
        }

        double distance = Math.Sqrt(dx * dx + dy * dy);
        double maxDistance = Math.Sqrt((width + gradientOffset) * (width + gradientOffset) + (height + gradientOffset) * (height + gradientOffset));
        return distance / maxDistance;
    }
    private double GetTransitionProgress()
    {
        double timeOfDay = weather.TimeOfDay; // Current time in 24h format

        // Duration of the transition in hours (adjustable)
        double transitionDuration = 1.0;

        // Normalize timeOfDay to [0,24)
        timeOfDay %= 24.0;

        double transitionProgress = 0.0;

        // Setup transition periods
        double sunsetStart = sunsetTime - transitionDuration;
        if (sunsetStart < 0) sunsetStart += 24.0;

        double sunsetEnd = sunsetTime;

        double sunriseStart = sunriseTime;
        double sunriseEnd = sunriseTime + transitionDuration;
        if (sunriseEnd >= 24.0) sunriseEnd -= 24.0;

        if (IsTimeBetween(timeOfDay, sunsetStart, sunsetEnd))
        {
            // Sunset transition (progress from 0 to 1)
            double totalDuration = (sunsetEnd - sunsetStart + 24.0) % 24.0;
            transitionProgress = ((timeOfDay - sunsetStart + 24.0) % 24.0) / totalDuration;
        }
        else if (IsTimeBetween(timeOfDay, sunriseStart, sunriseEnd))
        {
            // Sunrise transition (progress from 1 to 0)
            double totalDuration = (sunriseEnd - sunriseStart + 24.0) % 24.0;
            transitionProgress = 1.0 - ((timeOfDay - sunriseStart + 24.0) % 24.0) / totalDuration;
        }
        else if (IsNightTime(timeOfDay, sunsetEnd, sunriseStart))
        {
            // Night time
            transitionProgress = 1.0;
        }
        else
        {
            // Day time
            transitionProgress = 0.0;
        }

        // Clamp transitionProgress to ensure it stays within bounds
        transitionProgress = Math.Clamp(transitionProgress, 0.0, 1.0);

        return transitionProgress;
    }
    private bool IsTimeBetween(double time, double start, double end)
    {
        if (start <= end)
        {
            return time >= start && time <= end;
        }
        else
        {
            return time >= start || time <= end;
        }
    }
    private bool IsNightTime(double time, double sunsetEnd, double sunriseStart)
    {
        return IsTimeBetween(time, sunsetEnd, sunriseStart);
    }
    private static double EaseInOutQuad(double t)
    {
        // Simulate smooth transition
        if (t < 0.5)
            return 2 * t * t;
        else
            return -1 + (4 - 2 * t) * t;
    }
    private (int r, int g, int b) GetTileBaseColor(int x, int y)
    {
        if (IsThereAWaveTile(x, y))
        {
            return GetWaveColor(x, y);
        }
        else
        {
            return GetColor(mapData[x, y], x, y);
        }
    }
    private bool IsTileDarkened(int x, int y)
    {
        return darkenedPositions.Contains((x, y));
    }
    private (int r, int g, int b) GetDarkenedColor(int x, int y)
    {
        if (darkenedPositionsIntensities.TryGetValue((x, y), out int intensity))
        {
            (int r, int g, int b) baseColor = GetTileBaseColor(x, y);
            int r = Math.Max(0, baseColor.r - intensity);
            int g = Math.Max(0, baseColor.g - intensity);
            int b = Math.Max(0, baseColor.b - intensity);
            return (r, g, b);
        }
        return GetTileBaseColor(x, y);
    }
    public void DisplayDarkenedTiles()
    {
        foreach ((int x, int y) in darkenedPositions)
        {
            Console.SetCursorPosition(leftPadding + x * 2, y + topPadding);
            (int r, int g, int b) color = GetDarkenedColor(x, y);
            Console.Write(SetBackgroundColor(color.r, color.g, color.b) + "  " + ResetColor());
        }
    }
    public void DisplayDarkenedWaveTiles()
    {
        foreach ((int x, int y) in wavePositions)
        {
            Console.SetCursorPosition(leftPadding + x * 2, y + topPadding);
            (int r, int g, int b) color = GetDarkenedColor(x, y);
            Console.Write(SetBackgroundColor(color.r, color.g, color.b) + "  " + ResetColor());
        }
    }
    private (int r, int g, int b) GetDarkenedTileColor(char tile, int x, int y)
    {
        (int r, int g, int b) baseColor = GetColor(tile, x, y);
        int r = Math.Max(0, baseColor.r - 50);
        int g = Math.Max(0, baseColor.g - 50);
        int b = Math.Max(0, baseColor.b - 50);
        return (r, g, b);

    }
    private double GetDarkenedTileIntensity(int x, int y)
    {
        if (darkenedPositionsIntensities.TryGetValue((x, y), out int intensity))
        {
            return intensity;
        }
        return 0;
    }
    private void DisplayDarkenedOverlayTiles()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (overlayData[x, y] != '\0')
                {
                    
                }
            }
        }
    }
    #endregion
    #endregion
    public void DisplayMap()
    {
        // Don't clear console, just move cursor to start position
        Console.SetCursorPosition(leftPadding, topPadding);

        for (int y = 0; y < height; y++)
        {
            // Set cursor position at start of each line
            Console.SetCursorPosition(leftPadding, y + topPadding);
            
            for (int x = 0; x < width; x++)
            {
                (int r, int g, int b) color = GetColor(mapData[x, y], x, y);
                Console.Write(SetBackgroundColor(color.r, color.g, color.b) + "  " + ResetColor());
            }
        }
        DisplayDarkenedTiles();
        // Continue with overlay tile rendering...
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
                {
                    continue;
                }

                if (overlayData[x, y] != '\0')
                {
                    Console.SetCursorPosition(leftPadding + x * 2, y + topPadding);
                    (int r, int g, int b) bgColor = GetDarkenedColor(x, y);
                    (int r, int g, int b) fgColor = GetOverlayColor(overlayData[x, y]);
                    string bg = SetBackgroundColor(bgColor.r, bgColor.g, bgColor.b);
                    string fg = SetForegroundColor(fgColor.r, fgColor.g, fgColor.b);
                    Console.Write(bg + fg + GetSpeciesIcon(overlayData[x, y]) + ResetColor());
                }
            }
        }
        Console.ResetColor();
        //Update();
        AnimateWater();
        DisplayCloudShadows();
        if (isCloudsRendering) RenderClouds();
        DisplayGUI();
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
    public void UpdateTile(int x, int y)
    {
        Console.SetCursorPosition(leftPadding + x * 2, y + topPadding);
        (int r, int g, int b) bgColor = GetColor(mapData[x, y], x, y); // Retrieve background color from ColorSpectrum
        string background = SetBackgroundColor(bgColor.r, bgColor.g, bgColor.b);
        Console.Write(background + "  " + ResetColor());
    }
    public void UpdateOverlayTile(int x, int y)
    {
        // Prevent overlay data from being displayed on the edges
        if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
        {
            return;
        }
        UpdateTile(x, y);

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

        string background = SetBackgroundColor(bgColor.r, bgColor.g, bgColor.b);
        string foreground = SetForegroundColor(fgColor.r, fgColor.g, fgColor.b);

        if (!isCloudsRendering || (isCloudsRendering && !IsTileUnderCloud(x, y)))
        {
            // Write the background color first
            Console.SetCursorPosition(leftPadding + x * 2, y + topPadding);
            Console.Write(background + "  " + ResetColor());

            // Write the overlay character with the correct background and foreground colors
            Console.SetCursorPosition(leftPadding + x * 2, y + topPadding);
            Console.Write(background + "  " + ResetColor());
            Console.SetCursorPosition(leftPadding + x * 2, y + topPadding);
            Console.Write(background + foreground + GetSpeciesIcon(overlayData[x, y]) + ResetColor());
        }
    }
    public string GetSpeciesIcon(char species)
    {
        return species switch
        {
            'c' => "󰃤 ",
            'T' => "󰳗 ",
            'C' => "󰆚 ",
            'S' => "󰳆 ",
            _ => "  "
        };
    }
    private bool IsThereAnOverlayTile(int x, int y)
    {
        return overlayData[x, y] != '\0';
    }
    public void DisplayCharacterOnTile(int x, int y, char character, string characterColor)
    {
        // Move cursor to position
        Console.SetCursorPosition(leftPadding + x * 2, y + topPadding);

        // Get RGB color based on characterColor using ColorSpectrum
        (int r, int g, int b) rgb = GetRGBFromColorCode(characterColor);
        string fg = SetForegroundColor(rgb.r, rgb.g, rgb.b);
        Console.Write(fg + character + ResetColor());
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
    private (int r, int g, int b) GetColor(char tile, int x, int y)
    {
        // Get the base color based on the tile type
        (int r, int g, int b) baseColor = tile switch
        {
            'F' => ColorSpectrum.DARK_GREEN, // Forest
            'P' => ColorSpectrum.GREEN, // Plains
            'M' => ColorSpectrum.DARK_GREY, // Mountain
            'm' => ColorSpectrum.GREY, // Dark mountain
            'S' => ColorSpectrum.WHITE, // Snow peak
            'R' or 'L' or 'O' or 's' => ColorSpectrum.BLUE, // Water
            'r' or 'l' or 'o' => ColorSpectrum.DARKER_BLUE, // Dark water
            'B' => ColorSpectrum.YELLOW, // Beach
            'b' => ColorSpectrum.DARK_YELLOW, // Dark beach
            '@' => ColorSpectrum.SILVER, // Border
            '1' => ColorSpectrum.CIRRUS, // Cirrus
            '2' => ColorSpectrum.ALTOCUMULUS, // Altocumulus
            '3' => ColorSpectrum.CUMULUS, // Cumulus
            '4' => ColorSpectrum.CUMULONIMBUS, // Cumulonimbus
            '5' => ColorSpectrum.NIMBOSTRATUS, // Nimbostratus
            '6' => ColorSpectrum.STRATUS, // Stratus
            '!' => ColorSpectrum.BRIGHT_RED, // Malware
            'X' => ColorSpectrum.SILVER, // Skull
            '%' => ColorSpectrum.WAVE_1, // Wave gradient 1
            '^' => ColorSpectrum.WAVE_2, // Wave gradient 2
            '&' => ColorSpectrum.WAVE_3, // Wave gradient 3
            '*' => ColorSpectrum.WAVE_4, // Wave gradient 4
            '(' => ColorSpectrum.WAVE_5, // Wave gradient 5
            _ => ColorSpectrum.BLUE // Default
        };
        int humZone = humidityData[x, y];
        int tempZone = temperatureData[x, y];

        int r, g, b;
        if (tile is '@')
        {
            return baseColor;
        }
        // Water
        if (tile is 'R' or 'L' or 'O' or 'r' or 'l' or 'o' or 's')
        {
            double baseWaterTemp = (avarageTempature - 0.6) * 100;
            double baseWaterHum = (avarageHumidity - 0.6) * 8;

            r = Math.Clamp(baseColor.r - (int)baseWaterHum, 0, 255);
            g = Math.Clamp(baseColor.g + (int)baseWaterTemp, 0, 255);
            b = Math.Clamp(baseColor.b - (int)baseWaterHum, 0, 255);

            return (r, g, b);
            //return GetOceanColor(avarageTempature, avarageHumidity);
        }

        if (tile is 'm' or 'M')
        {
            double baseMountainTemp = (avarageTempature - 0.5) * 60;
            double baseMountainHum = (avarageHumidity - 0.5) * 28;

            r = Math.Clamp(baseColor.r + (int)baseMountainTemp, 0, 255);
            g = Math.Clamp(baseColor.g + (int)baseMountainHum, 0, 255);
            b = Math.Clamp(baseColor.b - (int)baseMountainHum * 2, 0, 255);

            return (r, g, b);
        }
        // Determine temperature and humidity zones

        // Initialize color adjustments
        int rAdjustment = 0;
        int gAdjustment = 0;
        int bAdjustment = 0;

        // Adjust based on temperature zone
        switch (tempZone)
        {
            case 1: // Very Cold 
                bAdjustment -= 20;
                rAdjustment -= 20;
                break;
            case 2: // Cold 
                bAdjustment -= 15;
                gAdjustment -= 5;
                break;
            case 3: // Cool 
                gAdjustment += 5;
                bAdjustment += 10;
                break;
            case 4: // Temperate 
                gAdjustment += 10;
                rAdjustment += 5;
                break;
            case 5: // Warm
                rAdjustment += 10;
                gAdjustment += 15;
                break;
        }

        // Adjust based on humidity zone
        switch (humZone)
        {
            case 1: // Very Dry
                gAdjustment -= 15;
                bAdjustment -= 5;
                break;
            case 2: // Dry 
                gAdjustment -= 10;
                break;
            case 3: // Moderate 
                break;
            case 4: // Humid 
                gAdjustment += 15;
                bAdjustment += 10;
                break;
            case 5: // Very Humid 
                gAdjustment += 20;
                bAdjustment += 5;
                break;
        }

        // Apply adjustments to base color
        r = Math.Clamp(baseColor.r + rAdjustment, 0, 255);
        g = Math.Clamp(baseColor.g + gAdjustment, 0, 255);
        b = Math.Clamp(baseColor.b + bAdjustment, 0, 255);
        return (r, g, b);

    }
    public (int r, int g, int b) GetOverlayColor(char overlayTile)
    {
        switch (overlayTile)
        {
            case 'c': return ColorSpectrum.BRIGHT_RED; // Crabs
            case 'T': return ColorSpectrum.DARK_GREEN; // Turtles
            case 'C': return ColorSpectrum.BLACK; // Cows
            case 'S': return ColorSpectrum.BROWN; // Sheep
            case 'W': return ColorSpectrum.GREY; // Wolves
            case 'B': return ColorSpectrum.BROWN; // Bears
            case 'G': return ColorSpectrum.BROWN; // Goats
            case 'F': return ColorSpectrum.BRIGHT_RED; // Fish
            case 'A': return ColorSpectrum.BRIGHT_RED; // Birds
            case 'V': return ColorSpectrum.BROWN; // Villagers
            default: return ColorSpectrum.BLACK;
        }
    }
    public static string SetForegroundColor(int r, int g, int b)
    {
        return $"\u001b[38;2;{r};{g};{b}m";
    }
    public static string SetBackgroundColor(int r, int g, int b)
    {
        return $"\u001b[48;2;{r};{g};{b}m";
    }
    public static string ResetColor()
    {
        return "\u001b[0m";
    }
    #endregion
    #region temperature and humidity noise
    public void RenderTemperatureNoise()
    {
        for (int y = 0; y < height; y++)
        {
            Console.SetCursorPosition(leftPadding, y + topPadding);
            for (int x = 0; x < width; x++)
            {
                int tempValue = temperatureData[x, y];
                (int r, int g, int b) color = TemperatureZoneToColor(tempValue);
                Console.Write(SetBackgroundColor(color.r, color.g, color.b) + "  " + ResetColor());
            }
        }
    }
    public void RenderHumidityNoise()
    {
        for (int y = 0; y < height; y++)
        {
            Console.SetCursorPosition(leftPadding, y + topPadding);
            for (int x = 0; x < width; x++)
            {
                int humidityValue = humidityData[x, y];
                (int r, int g, int b) color = HumidityZoneToColor(humidityValue);
                Console.Write(SetBackgroundColor(color.r, color.g, color.b) + "  " + ResetColor());
            }
        }
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
    #region GUI functions
    public void DisplayGUI()
    {
        double time = Math.Round(weather.TimeOfDay, 2);
        double season = Math.Round(weather.Season, 2);
        WeatherType currentWeather = weather.CurrentWeather;
        WeatherType nextWeather = weather.NextWeather;
        double temperature = Math.Round(weather.Temperature, 2);
        double humidity = Math.Round(weather.Humidity, 2);
        double pressure = Math.Round(weather.Pressure, 2);
        double windSpeed = Math.Round(weather.WindSpeed, 2);
        double windDirection = Math.Round(weather.WindDirection, 2);
        // Get console dimensions
        int consoleWidth = Console.WindowWidth;
        int consoleHeight = Console.WindowHeight;

        // Initialize GUI Configuration
        GUIConfig config = new GUIConfig(consoleWidth, consoleHeight);

        // Check console size
        if (consoleWidth < config.MinConsoleWidth || consoleHeight < config.MinConsoleHeight)
        {
            Console.Clear();
            Console.SetCursorPosition(0, 0);
            Console.Write("Please resize the console window to a larger size.");
            shouldSimulationContinue = false;
            return;
        }

        // Define margins
        int leftMargin = leftPadding;
        int topMargin = topPadding;
        int rightMargin = rightPadding;
        int bottomMargin = bottomPadding;

        // Calculate content area dimensions
        int contentWidth = consoleWidth - leftMargin - rightMargin;
        int contentHeight = consoleHeight - topMargin - bottomMargin;

        if (consoleWidth >= config.MinConsoleWidth && consoleHeight >= config.MinConsoleHeight)
        {
            // Weather Radar
            DrawBox(0, 0, config.RadarWidth, config.RadarHeight, "Weather Radar");
            Console.SetCursorPosition(2, 1);
            Console.Write($"Not Implemented Yet");
            // Time Info
            DisplayTimeInfo(time, season, config.RadarWidth, config.TimeWidth, config.TimeHeight);
            // Weather Stats
            DisplayWeatherStats(
                currentWeather, nextWeather, temperature, humidity, pressure, windSpeed, windDirection,
                config.RadarWidth, config.StatsWidth, config.StatsHeight
            );
            // Thanks Info
            if (Console.WindowWidth > 200 && rightMargin >= 20)
            {
                DisplayThanksMessage(config.ThanksWidth, config.ThanksHeight);
            }
            // Title
            DisplayTitleAndSignature(config.TitleWidth, config.TitleHeight, "Chambers");
            // Output Log
            DisplayOutputLog(config.OutputWidth, config.OutputHeight, config.TitleWidth);
        }
        else
        {
            // Display message if the console is too small
            Console.Clear();
            Console.SetCursorPosition(0, 0);
            Console.Write("Please resize the console window to a larger size.");
            shouldSimulationContinue = false;
        }
        if (rightMargin >= 20)
        {
            // Help Info
            DisplayHelpInfo(config.HelpWidth, config.HelpHeight);
            // Tile Info
            DisplayTileInfo(config.TileWidth, config.TileHeight);
        }

        Console.SetCursorPosition(0, height + topMargin - 1);
    }
    public void UpdateGUIValues()
    {
        double time = Math.Round(weather.TimeOfDay, 2);
        double season = Math.Round(weather.Season, 2);
        WeatherType currentWeather = weather.CurrentWeather;
        WeatherType nextWeather = weather.NextWeather;
        double temperature = Math.Round(weather.Temperature, 2);
        double humidity = Math.Round(weather.Humidity, 2);
        double pressure = Math.Round(weather.Pressure, 2);
        double windSpeed = Math.Round(weather.WindSpeed, 2);
        double windDirection = Math.Round(weather.WindDirection, 2);

        // Get console dimensions
        int consoleWidth = Console.WindowWidth;
        int consoleHeight = Console.WindowHeight;

        // Initialize GUI Configuration
        GUIConfig config = new GUIConfig(consoleWidth, consoleHeight);

        // Time Info
        UpdateTimeInfo(time, season, config.RadarWidth, config.TimeWidth, config.TimeHeight);

        // Weather Stats
        UpdateWeatherStats(
            currentWeather, nextWeather, temperature, humidity, pressure, windSpeed, windDirection,
            config.RadarWidth, config.StatsWidth, config.StatsHeight
        );
        
        // Output Log
        UpdateOutputLog(config.OutputWidth, config.OutputHeight, config.TitleWidth);
    }
    private void DisplayWeatherRadar(int x, int y, int width, int height)
    {
    }
    private void DisplayWeatherStats(WeatherType currentWeather, WeatherType nextWeather, double temperature, double humidity,
    double pressure, double windSpeed, double windDirection, int radarWidth, int statsWidth, int statsHeight)
    {
        string currentWeatherDisplay = statsWidth < 43 ? GetShortWeatherName(currentWeather) : currentWeather.ToString();
        string nextWeatherDisplay = statsWidth < 43 ? GetShortWeatherName(nextWeather) : nextWeather.ToString();

        DrawBox(radarWidth - 1, 0, statsWidth + 2, 3, "Weather Stats");
        Console.SetCursorPosition(radarWidth + 1, 1);
        Console.Write($"{Map.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}Current: {currentWeatherDisplay}, Next: {nextWeatherDisplay}{Map.ResetColor()}");
        DrawBox(radarWidth - 1, 2, statsWidth + 2, statsHeight, " ");
        Console.SetCursorPosition(radarWidth + 1, 3);
        Console.Write($"{Map.SetForegroundColor(ColorSpectrum.CYAN.r, ColorSpectrum.CYAN.g, ColorSpectrum.CYAN.b)}Cloud Formations: {GetCloudFormations()}{Map.ResetColor()}");
        Console.SetCursorPosition(radarWidth + 1, 4);
        Console.Write($"{Map.SetForegroundColor(ColorSpectrum.CYAN.r, ColorSpectrum.CYAN.g, ColorSpectrum.CYAN.b)}Cloud Tiles: {GetCloudTilesCount()}{Map.ResetColor()}");
        Console.SetCursorPosition(radarWidth + 1, 5);
        Console.Write($"{Map.SetForegroundColor(ColorSpectrum.GREEN.r, ColorSpectrum.GREEN.g, ColorSpectrum.GREEN.b)}Temperature: {temperature}°C{Map.ResetColor()}");
        Console.SetCursorPosition(radarWidth + 1, 6);
        Console.Write($"{Map.SetForegroundColor(ColorSpectrum.BLUE.r, ColorSpectrum.BLUE.g, ColorSpectrum.BLUE.b)}Humidity: {humidity}%{Map.ResetColor()}");
        Console.SetCursorPosition(radarWidth + 1, 7);
        Console.Write($"{Map.SetForegroundColor(ColorSpectrum.MAGENTA.r, ColorSpectrum.MAGENTA.g, ColorSpectrum.MAGENTA.b)}Pressure: {pressure}hPa{Map.ResetColor()}");
        Console.SetCursorPosition(radarWidth + 1, 8);
        Console.Write($"{Map.SetForegroundColor(ColorSpectrum.ORANGE.r, ColorSpectrum.ORANGE.g, ColorSpectrum.ORANGE.b)}Wind Speed: {windSpeed}m/s{Map.ResetColor()}");
        Console.SetCursorPosition(radarWidth + 1, 9);
        Console.Write($"{Map.SetForegroundColor(ColorSpectrum.PURPLE.r, ColorSpectrum.PURPLE.g, ColorSpectrum.PURPLE.b)}Wind Direction: {windDirection}°{Map.ResetColor()}");
    }
    private void UpdateWeatherStats(WeatherType currentWeather, WeatherType nextWeather, double temperature, double humidity,
    double pressure, double windSpeed, double windDirection, int radarWidth, int statsWidth, int statsHeight)
    {
        string currentWeatherDisplay = statsWidth < 43 ? GetShortWeatherName(currentWeather) : currentWeather.ToString();
        string nextWeatherDisplay = statsWidth < 43 ? GetShortWeatherName(nextWeather) : nextWeather.ToString();

        Console.SetCursorPosition(radarWidth + 1, 1);
        Console.Write($"{Map.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}Current: {currentWeatherDisplay}, Next: {nextWeatherDisplay}{Map.ResetColor()}    ");
        Console.SetCursorPosition(radarWidth + 1, 5);
        Console.Write($"{Map.SetForegroundColor(ColorSpectrum.GREEN.r, ColorSpectrum.GREEN.g, ColorSpectrum.GREEN.b)}Temperature: {temperature}°C{Map.ResetColor()}   ");
        Console.SetCursorPosition(radarWidth + 1, 6);
        Console.Write($"{Map.SetForegroundColor(ColorSpectrum.BLUE.r, ColorSpectrum.BLUE.g, ColorSpectrum.BLUE.b)}Humidity: {humidity}%{Map.ResetColor()}   ");
        Console.SetCursorPosition(radarWidth + 1, 7);
        Console.Write($"{Map.SetForegroundColor(ColorSpectrum.MAGENTA.r, ColorSpectrum.MAGENTA.g, ColorSpectrum.MAGENTA.b)}Pressure: {pressure}hPa{Map.ResetColor()}   ");
        Console.SetCursorPosition(radarWidth + 1, 8);
        Console.Write($"{Map.SetForegroundColor(ColorSpectrum.ORANGE.r, ColorSpectrum.ORANGE.g, ColorSpectrum.ORANGE.b)}Wind Speed: {windSpeed}m/s{Map.ResetColor()}   ");
        Console.SetCursorPosition(radarWidth + 1, 9);
        Console.Write($"{Map.SetForegroundColor(ColorSpectrum.PURPLE.r, ColorSpectrum.PURPLE.g, ColorSpectrum.PURPLE.b)}Wind Direction: {windDirection}°{Map.ResetColor()}   ");
    }
    private string GetShortWeatherName(WeatherType weather)
    {
        return weather switch
        {
            WeatherType.Clear => "Clear",
            WeatherType.Drizzle => "Drizz",
            WeatherType.Rain => "Rain",
            WeatherType.Snow => "Snow",
            WeatherType.Thunderstorm => "Thund",
            WeatherType.Fog => "Foggy",
            WeatherType.Overcast => "Overc",
            WeatherType.Sleet => "Sleet",
            WeatherType.Hail => "Hail",
            WeatherType.BlowingSnow => "BlowS",
            WeatherType.Sandstorm => "Sand",
            _ => weather.ToString()
        };
    }
    private void DisplayTimeInfo(double time, double season, int radarWidth, int statsWidth, int statsHeight)
    {
        DrawBox(radarWidth + statsWidth - 2, 0, statsWidth, statsHeight + 2, "Time Info");
        Console.SetCursorPosition(radarWidth + statsWidth, 1);
        Console.Write($" {Map.SetForegroundColor(ColorSpectrum.LIGHT_BLUE.r, ColorSpectrum.LIGHT_BLUE.g, ColorSpectrum.LIGHT_BLUE.b)}Time: {time:F2}h{Map.ResetColor()}   ");
        Console.SetCursorPosition(radarWidth + statsWidth, 2);
        Console.Write($" {Map.SetForegroundColor(ColorSpectrum.LIGHT_GREEN.r, ColorSpectrum.LIGHT_GREEN.g, ColorSpectrum.LIGHT_GREEN.b)}Season: {season}{Map.ResetColor()}   ");
        Console.SetCursorPosition(radarWidth + statsWidth, 3);
        Console.Write($" {Map.SetForegroundColor(ColorSpectrum.ORANGE.r, ColorSpectrum.ORANGE.g, ColorSpectrum.ORANGE.b)}Sunrise: {sunriseTime}h{Map.ResetColor()}   ");
        Console.SetCursorPosition(radarWidth + statsWidth, 4);
        Console.Write($" {Map.SetForegroundColor(ColorSpectrum.ORANGE.r, ColorSpectrum.ORANGE.g, ColorSpectrum.ORANGE.b)}Sunset: {sunsetTime}h{Map.ResetColor()}  ");
        Console.SetCursorPosition(radarWidth + statsWidth, 5);
        if (time < sunriseTime)
        {
            Console.Write($" {Map.SetForegroundColor(ColorSpectrum.GREEN.r, ColorSpectrum.GREEN.g, ColorSpectrum.GREEN.b)}Time Until Sunrise: {sunriseTime - time:F2}h{Map.ResetColor()}");
        }
        else if (time >= sunriseTime && time < sunsetTime)
        {
            Console.Write($" {Map.SetForegroundColor(ColorSpectrum.RED.r, ColorSpectrum.RED.g, ColorSpectrum.RED.b)}Time Until Sunset: {sunsetTime - time:F2}h{Map.ResetColor()}");
        }
        else
        {
            double timeUntilMidnight = 24.0 - time;
            double timeUntilSunrise = timeUntilMidnight + sunriseTime;
            Console.Write($" {Map.SetForegroundColor(ColorSpectrum.GREEN.r, ColorSpectrum.GREEN.g, ColorSpectrum.GREEN.b)}Time Until Sunrise: {timeUntilSunrise:F2}h{Map.ResetColor()}");
        }
        Console.SetCursorPosition(radarWidth + statsWidth, 6);
        Console.Write($" {Map.SetForegroundColor(ColorSpectrum.PINK.r, ColorSpectrum.PINK.g, ColorSpectrum.PINK.b)}Day: {dayCount}{Map.ResetColor()}   ");
        
    }
    private void UpdateTimeInfo(double time, double season, int radarWidth, int statsWidth, int statsHeight)
    {
        Console.SetCursorPosition(radarWidth + statsWidth, 1);
        Console.Write($" {Map.SetForegroundColor(ColorSpectrum.LIGHT_BLUE.r, ColorSpectrum.LIGHT_BLUE.g, ColorSpectrum.LIGHT_BLUE.b)}Time: {time:F2}h{Map.ResetColor()}   ");
        Console.SetCursorPosition(radarWidth + statsWidth, 2);
        Console.Write($" {Map.SetForegroundColor(ColorSpectrum.LIGHT_GREEN.r, ColorSpectrum.LIGHT_GREEN.g, ColorSpectrum.LIGHT_GREEN.b)}Season: {season}{Map.ResetColor()}   ");
        Console.SetCursorPosition(radarWidth + statsWidth, 3);
        Console.Write($" {Map.SetForegroundColor(ColorSpectrum.ORANGE.r, ColorSpectrum.ORANGE.g, ColorSpectrum.ORANGE.b)}Sunrise: {sunriseTime}h{Map.ResetColor()}   ");
        Console.SetCursorPosition(radarWidth + statsWidth, 4);
        Console.Write($" {Map.SetForegroundColor(ColorSpectrum.ORANGE.r, ColorSpectrum.ORANGE.g, ColorSpectrum.ORANGE.b)}Sunset: {sunsetTime}h{Map.ResetColor()}  ");
        Console.SetCursorPosition(radarWidth + statsWidth, 5);
        if (time < sunriseTime)
        {
            Console.Write($" {Map.SetForegroundColor(ColorSpectrum.GREEN.r, ColorSpectrum.GREEN.g, ColorSpectrum.GREEN.b)}Time Until Sunrise: {sunriseTime - time:F2}h{Map.ResetColor()}   ");
        }
        else if (time >= sunriseTime && time < sunsetTime)
        {
            Console.Write($" {Map.SetForegroundColor(ColorSpectrum.RED.r, ColorSpectrum.RED.g, ColorSpectrum.RED.b)}Time Until Sunset: {sunsetTime - time:F2}h{Map.ResetColor()}   ");
        }
        else
        {
            double timeUntilMidnight = 24.0 - time;
            double timeUntilSunrise = timeUntilMidnight + sunriseTime;
            Console.Write($" {Map.SetForegroundColor(ColorSpectrum.GREEN.r, ColorSpectrum.GREEN.g, ColorSpectrum.GREEN.b)}Time Until Sunrise: {timeUntilSunrise:F2}h{Map.ResetColor()}   ");
        }
        Console.SetCursorPosition(radarWidth + statsWidth, 6);
        Console.Write($" {Map.SetForegroundColor(ColorSpectrum.PINK.r, ColorSpectrum.PINK.g, ColorSpectrum.PINK.b)}Day: {dayCount}{Map.ResetColor()}   ");
    }
    private void DisplayHelpInfo(int helpWidth, int helpHeight)
    {
        string line = new string('-', helpWidth - 3);
        if (helpHeight < 30)
        {
            DrawBox(Console.WindowWidth - rightPadding * 2, Console.WindowHeight - bottomPadding - helpHeight, helpWidth, helpHeight, "Help Menu");
            Console.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 1);
            Console.Write($"{Map.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}P/Space:{Map.ResetColor()}");
            Console.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 2);
            Console.Write("Toggle updating");
            Console.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 3);
            Console.Write($"{Map.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}PgUp/PgDn:{Map.ResetColor()}");
            Console.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 4);
            Console.Write("Increase/Decrease updating speed");
            Console.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 5);
            Console.Write($"{Map.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}Q:{Map.ResetColor()}");
            Console.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 6);
            Console.Write("Toggle cloud rendering");
            Console.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 7);
            Console.Write($"{Map.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}Up/Down:{Map.ResetColor()}");
            Console.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 8);
            Console.Write("Go to last/first chamber");
            Console.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 9);
            Console.Write($"{Map.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}Left/Right:{Map.ResetColor()}");
            Console.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 10);
            Console.Write("Previous/Next chamber");
            Console.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 11);
            Console.Write($"{Map.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}1 - 9:{Map.ResetColor()}");
            Console.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 12);
            Console.Write("Go to chamber 1 - 9");
            Console.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 13);
            Console.Write($"{Map.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}C:{Map.ResetColor()}");
            Console.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 14);
            Console.Write("Open console");
        }
        else
        {
            DrawBox(Console.WindowWidth - rightPadding * 2, Console.WindowHeight - bottomPadding - helpHeight, helpWidth, helpHeight, "Help Menu");
            Console.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 1);
            Console.Write($"{Map.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}P/Space:{Map.ResetColor()}");
            Console.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 2);
            Console.Write("Toggle updating");
            Console.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 3);
            Console.Write(line);
            Console.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 4);
            Console.Write($"{Map.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}PgUp/PgDn:{Map.ResetColor()}");
            Console.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 5);
            Console.Write("Increase/Decrease updating speed");
            Console.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 6);
            Console.Write(line);
            Console.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 7);
            Console.Write($"{Map.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}Q:{Map.ResetColor()}");
            Console.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 8);
            Console.Write("Toggle cloud rendering");
            Console.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 9);
            Console.Write(line);
            Console.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 10);
            Console.Write($"{Map.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}Up/Down:{Map.ResetColor()}");
            Console.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 11);
            Console.Write("Go to last/first chamber");
            Console.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 12);
            Console.Write(line);
            Console.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 13);
            Console.Write($"{Map.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}Left/Right:{Map.ResetColor()}");
            Console.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 14);
            Console.Write("Previous/Next chamber");
            Console.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 15);
            Console.Write(line);
            Console.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 16);
            Console.Write($"{Map.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}1 - 9:{Map.ResetColor()}");
            Console.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 17);
            Console.Write("Go to chamber 1 - 9");
            Console.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 18);
            Console.Write(line);
            Console.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 19);
            Console.Write($"{Map.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}C:{Map.ResetColor()}");
            Console.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 20);
            Console.Write("Open console");
        }
    }
    private void DisplayTileInfo(int tileWidth, int tileHeight)
    {
        DrawBox(Console.WindowWidth - tileWidth, 0 + topPadding + 1, tileWidth, tileHeight, "Tile Info");
        if (rightPadding >= 20)
        {
            Console.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 1 + topPadding + 1);
            Console.Write($"{Map.SetForegroundColor(ColorSpectrum.DARK_GREEN.r, ColorSpectrum.DARK_GREEN.g, ColorSpectrum.DARK_GREEN.b)}Dark Green:{Map.ResetColor()} Forest");
            Console.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 2 + topPadding + 1);
            Console.Write($"{Map.SetForegroundColor(ColorSpectrum.GREEN.r, ColorSpectrum.GREEN.g, ColorSpectrum.GREEN.b)}Green:{Map.ResetColor()} Plains");
            Console.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 3 + topPadding + 1);
            Console.Write($"{Map.SetForegroundColor(ColorSpectrum.GREY.r, ColorSpectrum.GREY.g, ColorSpectrum.GREY.b)}Grey / {Map.SetForegroundColor(ColorSpectrum.DARK_GREY.r, ColorSpectrum.DARK_GREY.g, ColorSpectrum.DARK_GREY.b)}Dark Gray:{Map.ResetColor()} Mountain");
            Console.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 4 + topPadding + 1);
            Console.Write($"{Map.SetForegroundColor(ColorSpectrum.WHITE.r, ColorSpectrum.WHITE.g, ColorSpectrum.WHITE.b)}White:{Map.ResetColor()} Snow Peak");
            Console.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 5 + topPadding + 1);
            Console.Write($"{Map.SetForegroundColor(ColorSpectrum.BLUE.r, ColorSpectrum.BLUE.g, ColorSpectrum.BLUE.b)}Blue:{Map.ResetColor()} Water");
            Console.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 6 + topPadding + 1);
            Console.Write($"{Map.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}Yellow:{Map.ResetColor()} Beach");
        }
        else
        {
            Console.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 1 + topPadding + 1);
            Console.Write($"{Map.SetForegroundColor(ColorSpectrum.DARK_GREEN.r, ColorSpectrum.DARK_GREEN.g, ColorSpectrum.DARK_GREEN.b)}Dark Green:{Map.ResetColor()}");
            Console.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 2 + topPadding + 1);
            Console.Write("Forest");
            Console.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 3 + topPadding + 1);
            Console.Write($"{Map.SetForegroundColor(ColorSpectrum.GREEN.r, ColorSpectrum.GREEN.g, ColorSpectrum.GREEN.b)}Green:{Map.ResetColor()}");
            Console.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 4 + topPadding + 1);
            Console.Write("Plains");
            Console.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 5 + topPadding + 1);
            Console.Write($"{Map.SetForegroundColor(ColorSpectrum.GREY.r, ColorSpectrum.GREY.g, ColorSpectrum.GREY.b)}Grey / {Map.SetForegroundColor(ColorSpectrum.DARK_GREY.r, ColorSpectrum.DARK_GREY.g, ColorSpectrum.DARK_GREY.b)}Dark Gray:{Map.ResetColor()}");
            Console.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 6 + topPadding + 1);
            Console.Write("Mountain");
            Console.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 7 + topPadding + 1);
            Console.Write($"{Map.SetForegroundColor(ColorSpectrum.WHITE.r, ColorSpectrum.WHITE.g, ColorSpectrum.WHITE.b)}White:{Map.ResetColor()}");
            Console.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 8 + topPadding + 1);
            Console.Write("Snow Peak");
            Console.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 9 + topPadding + 1);
            Console.Write($"{Map.SetForegroundColor(ColorSpectrum.BLUE.r, ColorSpectrum.BLUE.g, ColorSpectrum.BLUE.b)}Blue:{Map.ResetColor()}");
            Console.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 10 + topPadding + 1);
            Console.Write("Water");
            Console.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 11 + topPadding + 1);
            Console.Write($"{Map.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}Yellow:{Map.ResetColor()}");
            Console.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 12 + topPadding + 1);
            Console.Write("Beach");
        }
    }
    private void DisplayThanksMessage(int thanksWidth, int thanksHeight)
    {
        string thanks = "Thanks";
        string line = new string('-', thanksWidth - 3);
        string halfLine = new string('-', thanksWidth / 2 - 2 - thanks.Length / 2 - 1);
        DrawBox(Console.WindowWidth - thanksWidth, 0, thanksWidth, thanksHeight, "Other");
        Console.SetCursorPosition(Console.WindowWidth - thanksWidth + 2, 1);
        Console.Write($"{Map.SetForegroundColor(ColorSpectrum.PURPLE.r, ColorSpectrum.PURPLE.g, ColorSpectrum.PURPLE.b)}Thanks for playing!{Map.ResetColor()}");
        Console.SetCursorPosition(Console.WindowWidth - thanksWidth + 2, 2);
        Console.Write(line);
        Console.SetCursorPosition(Console.WindowWidth - thanksWidth + 2, 3);
        Console.Write($"{Map.SetForegroundColor(ColorSpectrum.CYAN.r, ColorSpectrum.CYAN.g, ColorSpectrum.CYAN.b)}This project was created as{Map.ResetColor()}");
        Console.SetCursorPosition(Console.WindowWidth - thanksWidth + 2, 4);
        Console.Write($"{Map.SetForegroundColor(ColorSpectrum.CYAN.r, ColorSpectrum.CYAN.g, ColorSpectrum.CYAN.b)}a starting project for learning C#. {Map.ResetColor()}");
        Console.SetCursorPosition(Console.WindowWidth - thanksWidth + 2, 5);
        Console.Write(line);
        Console.SetCursorPosition(Console.WindowWidth - thanksWidth + 2, thanksHeight - 4);
        Console.Write($"{halfLine}{Map.SetForegroundColor(ColorSpectrum.GREEN.r, ColorSpectrum.GREEN.g, ColorSpectrum.GREEN.b)} {thanks} {Map.ResetColor()}{halfLine}");
    }
    private void DisplayTitleAndSignature(int titleWidth, int titleHeight, string title)
    {
        // Draw the box
        DrawBox(Console.WindowWidth / 2 - titleWidth / 2, 0, titleWidth, 3, " ");

        int x = Console.WindowWidth / 2 - titleWidth / 2;
        int y = 0;

        // Define the side patterns
        string leftSide = "~~//";
        string rightSide = "//~~";

        // Construct the title with side patterns
        string name = leftSide + title + rightSide;

        // Ensure the name fits within titleWidth
        int maxNameLength = titleWidth - 2; // Subtract borders
        if (name.Length > maxNameLength)
        {
            // Truncate the title to fit
            int maxTitleLength = maxNameLength - leftSide.Length - rightSide.Length;
            title = title.Substring(0, Math.Max(maxTitleLength, 0));
            name = leftSide + title + rightSide;
        }

        // Calculate positions
        int nameStartX = x + (titleWidth - name.Length) / 2;
        int titleY = y + 1;

        // Write the name
        Console.SetCursorPosition(nameStartX, titleY);
        Console.Write($"{Map.SetForegroundColor(ColorSpectrum.CYAN.r, ColorSpectrum.CYAN.g, ColorSpectrum.CYAN.b)}{name}{Map.ResetColor()}");

        DrawBox(Console.WindowWidth / 2 - titleWidth / 2, 2, titleWidth, titleHeight, " ");
        // Centered and fancy signature
        int centerX = Console.WindowWidth / 2;
        int centerY = topPadding / 2;

        string signature1 = "** Made by: @cybutr **";
        string signature2 = "* On GitHub *";

        Console.SetCursorPosition(centerX - signature1.Length / 2, centerY);
        Console.Write($"{Map.SetForegroundColor(ColorSpectrum.MAGENTA.r, ColorSpectrum.MAGENTA.g, ColorSpectrum.MAGENTA.b)}{signature1}{Map.ResetColor()}");

        Console.SetCursorPosition(centerX - signature2.Length / 2, centerY + 1);
        Console.Write($"{Map.SetForegroundColor(ColorSpectrum.CYAN.r, ColorSpectrum.CYAN.g, ColorSpectrum.CYAN.b)}{signature2}{Map.ResetColor()}");
    }
    public void DisplayOutputLog(int outputWidth, int outputHeight, int titleWidth)
    {
        int startX = Console.WindowWidth / 2 + titleWidth / 2 - 1;
        int startY = 2;

        if (startX < 0) startX = 0;
        if (startY < 0) startY = 0;

        DrawBox(startX, 0, outputWidth, 3, "Current Event");
        string text = eventBuffer.LastOrDefault() ?? "";
        int textLength = text.Length;
        int xPosition = startX + (outputWidth - textLength) / 2;
        Console.SetCursorPosition(xPosition, 1);
        Console.Write($"{Map.SetForegroundColor(ColorSpectrum.CYAN.r, ColorSpectrum.CYAN.g, ColorSpectrum.CYAN.b)}{text}{Map.ResetColor()}");
        DrawBox(startX, startY, outputWidth, outputHeight - 3, " ");
        int cursorX = startX + 2;
        int cursorY = startY + 1;

        int maxLines = outputHeight - 5;
        int linesToDisplay = Math.Min(actualOutputBuffer.Count, maxLines);

        // Define keyword-color mapping using ColorSpectrum
        Dictionary<string, (int r, int g, int b)> keywordColors = new Dictionary<string, (int r, int g, int b)>(StringComparer.OrdinalIgnoreCase)
        {
            { "Added", ColorSpectrum.GREEN },
            { "Removed", ColorSpectrum.RED },
            { "Updated", ColorSpectrum.YELLOW },
            { "Regenerated", ColorSpectrum.CYAN },
            { "Chamber", ColorSpectrum.DARKER_BLUE },
            { "Error", ColorSpectrum.RED },
            { "Warning", ColorSpectrum.YELLOW },
            { "Info", ColorSpectrum.CYAN },
            { "Debug", ColorSpectrum.DARK_GREY },
            { "Crab", ColorSpectrum.RED },
            { "Turtle", ColorSpectrum.GREEN },
            { "Sheep", ColorSpectrum.SILVER },
            { "Cow", ColorSpectrum.SILVER },
            { "Pig", ColorSpectrum.PINK },
            { "Chicken", ColorSpectrum.YELLOW },
            { "Fox", ColorSpectrum.ORANGE },
            { "Rabbit", ColorSpectrum.GREY },
            { "Wolf", ColorSpectrum.GREY },
            { "Bear", ColorSpectrum.BROWN },
            { "Goat", ColorSpectrum.DARK_GREY},
            { "Fish", ColorSpectrum.BLUE_VIOLET},
            { "Bird", ColorSpectrum.DARK_GREY},
            { "Weather", ColorSpectrum.CYAN },
            { "Time", ColorSpectrum.LIGHT_BLUE },
            { "Season", ColorSpectrum.LIGHT_GREEN },
            { "Temperature", ColorSpectrum.GREEN },
            { "Humidity", ColorSpectrum.BLUE },
            { "Pressure", ColorSpectrum.MAGENTA },
            { "Wind", ColorSpectrum.ORANGE },
            { "Sunrise", ColorSpectrum.ORANGE },
            { "Sunset", ColorSpectrum.ORANGE },
            { "Day", ColorSpectrum.PINK },
            { "Cloud", ColorSpectrum.SILVER},
            { "Plains", ColorSpectrum.GREEN},
            { "Forest", ColorSpectrum.DARK_GREEN},
            { "Mountain", ColorSpectrum.GREY},
            { "Snow", ColorSpectrum.WHITE},
            { "Water", ColorSpectrum.BLUE},
            { "Beach", ColorSpectrum.YELLOW},
        };

        string Reset = "\u001b[0m";

        // Get the latest lines and reverse them to display newest at the top
        List<string> lines = actualOutputBuffer.Skip(Math.Max(0, actualOutputBuffer.Count - maxLines))
                                .Take(maxLines)
                                .Reverse()
                                .ToList();

        for (int i = 0; i < lines.Count; i++)
        {
            string line = lines[i];
            string[] words = line.Split(' ');
            Console.SetCursorPosition(cursorX, cursorY + i);
            foreach (string word in words)
            {
                string trimmedWord = word.Trim(',', '.', '!', '?'); // Trim punctuation
                if (keywordColors.ContainsKey(trimmedWord))
                {
                    (int r, int g, int b) color = keywordColors[trimmedWord];
                    Console.Write($"{Map.SetForegroundColor(color.r, color.g, color.b)}{word}{Reset} ");
                }
                else
                {
                    Console.Write($"{word} ");
                }
            }
        }
    }
    public void UpdateOutputLog(int outputWidth, int outputHeight, int titleWidth)
    {
        int startX = Console.WindowWidth / 2 + titleWidth / 2 - 1;
        int startY = 2;

        if (startX < 0) startX = 0;
        if (startY < 0) startY = 0;

        string text = eventBuffer.LastOrDefault() ?? "";
        int textLength = text.Length;
        int xPosition = startX + (outputWidth - textLength) / 2;
        Console.SetCursorPosition(xPosition, 1);
        Console.Write($"{Map.SetForegroundColor(ColorSpectrum.CYAN.r, ColorSpectrum.CYAN.g, ColorSpectrum.CYAN.b)}{text}{Map.ResetColor()}");
        int cursorX = startX + 2;
        int cursorY = startY + 1;

        int maxLines = outputHeight - 5;
        int linesToDisplay = Math.Min(actualOutputBuffer.Count, maxLines);

        // Define keyword-color mapping using ColorSpectrum
        Dictionary<string, (int r, int g, int b)> keywordColors = new Dictionary<string, (int r, int g, int b)>(StringComparer.OrdinalIgnoreCase)
        {
            { "Added", ColorSpectrum.GREEN },
            { "Removed", ColorSpectrum.RED },
            { "Updated", ColorSpectrum.YELLOW },
            { "Regenerated", ColorSpectrum.CYAN },
            { "Chamber", ColorSpectrum.DARKER_BLUE },
            { "Error", ColorSpectrum.RED },
            { "Warning", ColorSpectrum.YELLOW },
            { "Info", ColorSpectrum.CYAN },
            { "Debug", ColorSpectrum.DARK_GREY },
            { "Crab", ColorSpectrum.RED },
            { "Turtle", ColorSpectrum.GREEN },
            { "Sheep", ColorSpectrum.SILVER },
            { "Cow", ColorSpectrum.SILVER },
            { "Pig", ColorSpectrum.PINK },
            { "Chicken", ColorSpectrum.YELLOW },
            { "Fox", ColorSpectrum.ORANGE },
            { "Rabbit", ColorSpectrum.GREY },
            { "Wolf", ColorSpectrum.GREY },
            { "Bear", ColorSpectrum.BROWN },
            { "Goat", ColorSpectrum.DARK_GREY},
            { "Fish", ColorSpectrum.BLUE_VIOLET},
            { "Bird", ColorSpectrum.DARK_GREY},
            { "Weather", ColorSpectrum.CYAN },
            { "Time", ColorSpectrum.LIGHT_BLUE },
            { "Season", ColorSpectrum.LIGHT_GREEN },
            { "Temperature", ColorSpectrum.GREEN },
            { "Humidity", ColorSpectrum.BLUE },
            { "Pressure", ColorSpectrum.MAGENTA },
            { "Wind", ColorSpectrum.ORANGE },
            { "Sunrise", ColorSpectrum.ORANGE },
            { "Sunset", ColorSpectrum.ORANGE },
            { "Day", ColorSpectrum.PINK },
            { "Cloud", ColorSpectrum.SILVER},
            { "Plains", ColorSpectrum.GREEN},
            { "Forest", ColorSpectrum.DARK_GREEN},
            { "Mountain", ColorSpectrum.GREY},
            { "Snow", ColorSpectrum.WHITE},
            { "Water", ColorSpectrum.BLUE},
            { "Beach", ColorSpectrum.YELLOW},
        };
        string Reset = "\u001b[0m";

        // Get the latest lines and reverse them to display newest at the top
        List<string> lines = actualOutputBuffer.Skip(Math.Max(0, actualOutputBuffer.Count - maxLines))
                                .Take(maxLines)
                                .Reverse()
                                .ToList();

        foreach (string? line in lines)
        {
            string[] words = line.Split(' ');
            int currentX = cursorX;
            int currentY = cursorY;
            foreach (string word in words)
            {
                string trimmedWord = word.Trim(',', '.', '!', '?');
                string displayWord = word + " ";
                int wordLength = displayWord.Length;

                // Split word if it's longer than outputWidth - 3
                if (wordLength > outputWidth - 3)
                {
                    int splitIndex = outputWidth - 3;
                    string firstPart = displayWord.Substring(0, splitIndex);
                    string remainingPart = displayWord.Substring(splitIndex);

                    // Write the first part
                    if (currentX + firstPart.Length > startX + outputWidth - 2)
                    {
                        currentX = startX + 2;
                        currentY += 1;
                        if (currentY >= startY + maxLines)
                            break;
                    }

                    if (keywordColors.ContainsKey(trimmedWord))
                    {
                        (int r, int g, int b) color = keywordColors[trimmedWord];
                        Console.SetCursorPosition(currentX, currentY);
                        Console.Write($"{Map.SetForegroundColor(color.r, color.g, color.b)}{firstPart}{Reset}");
                    }
                    else
                    {
                        Console.SetCursorPosition(currentX, currentY);
                        Console.Write($"{firstPart}");
                    }
                    currentX += firstPart.Length;

                    // Prepare the remaining part
                    if (!string.IsNullOrWhiteSpace(remainingPart))
                    {
                        currentX = startX + 2;
                        currentY += 1;
                        if (currentY >= startY + maxLines)
                            break;
                        displayWord = remainingPart;
                        wordLength = displayWord.Length;
                    }
                    else
                    {
                        continue;
                    }
                }

                // Check if the word fits in the current line
                if (currentX + wordLength > startX + outputWidth - 2)
                {
                    // Move to next line
                    currentX = startX + 2;
                    currentY += 1;
                    if (currentY >= startY + maxLines)
                        break;
                }

                // Write word with color
                if (keywordColors.ContainsKey(trimmedWord))
                {
                    (int r, int g, int b) color = keywordColors[trimmedWord];
                    Console.SetCursorPosition(currentX, currentY);
                    Console.Write($"{Map.SetForegroundColor(color.r, color.g, color.b)}{word}{Reset} ");
                }
                else
                {
                    Console.SetCursorPosition(currentX, currentY);
                    Console.Write($"{word} ");
                }

                currentX += wordLength;
            }
        }
    }
    private void DrawBox(int x, int y, int width, int height, string title, string titleLeftDecor = "{", string titleRightDecor = "}")
    {
        
        // Define box drawing characters
        string topLeft = "╔";
        string topRight = "╗";
        string bottomLeft = "╚";
        string bottomRight = "╝";
        string doubleHorizontal = "═";
        string doubleVertical = "║";
        string horizontal = "-";
        string vertical = "|";
        string corner = "+";

        // Draw top border with double lines
        Console.SetCursorPosition(x, y);
        if (!isLinux) Console.Write(topLeft + new string(doubleHorizontal[0], width - 2) + topRight);
        else Console.Write(corner + new string(horizontal[0], width - 2) + corner);

        // Draw sides and content area
        for (int i = 1; i < height - 1; i++)
        {
            Console.SetCursorPosition(x, y + i);
            if (!isLinux) Console.Write(doubleVertical + new string(' ', width - 2) + doubleVertical);
            else Console.Write(vertical + new string(' ', width - 2) + vertical);
        }

        // Draw bottom border with double lines
        Console.SetCursorPosition(x, y + height - 1);
        if (!isLinux) Console.Write(bottomLeft + new string(doubleHorizontal[0], width - 2) + bottomRight);
        else Console.Write(corner + new string(horizontal[0], width - 2) + horizontal);

        // Write the decorated title in the middle of the top of the box if not empty or whitespace
        if (!string.IsNullOrWhiteSpace(title))
        {
            string decoratedTitle = $"{titleLeftDecor} {title} {titleRightDecor}";
            int titleLength = decoratedTitle.Length;
            int padding = (width - 2 - titleLength) / 2;
            int cursorX = x + 1 + padding;
            int cursorY = y;

            if (cursorX + titleLength < Console.WindowWidth && cursorY < Console.WindowHeight)
            {
                Console.SetCursorPosition(cursorX, cursorY);
                Console.Write(Map.SetForegroundColor(ColorSpectrum.DARK_GREEN.r, ColorSpectrum.DARK_GREEN.g, ColorSpectrum.DARK_GREEN.b) + decoratedTitle + Map.ResetColor());
            }
        }

        // Add decorative corners
        if (!isLinux)
        {
            Console.SetCursorPosition(x, y);
            Console.Write(topLeft);
            Console.SetCursorPosition(x + width - 1, y);
            Console.Write(topRight);
            Console.SetCursorPosition(x, y + height - 1);
            Console.Write(bottomLeft);
            Console.SetCursorPosition(x + width - 1, y + height - 1);
            Console.Write(bottomRight);
        }
        else 
        {
            Console.SetCursorPosition(x, y);
            Console.Write(corner);
            Console.SetCursorPosition(x + width - 1, y);
            Console.Write(corner);
            Console.SetCursorPosition(x, y + height - 1);
            Console.Write(corner);
            Console.SetCursorPosition(x + width - 1, y + height - 1);
            Console.Write(corner);
        }
    }
    private void DrawColoredBox(int x, int y, int width, int height, string title, (int r, int g, int b) color, string titleLeftDecor = "{", string titleRightDecor = "}")
    {
        
        // Define box drawing characters
        string topLeft = "╔";
        string topRight = "╗";
        string bottomLeft = "╚";
        string bottomRight = "╝";
        string doubleHorizontal = "═";
        string doubleVertical = "║";
        string horizontal = "-";
        string vertical = "|";
        string corner = "+";

        // Draw top border with double lines
        Console.SetCursorPosition(x, y);
        if (!isLinux)
            Console.Write(Map.SetForegroundColor(color.r, color.g, color.b) + topLeft + new string(doubleHorizontal[0], width - 2) + topRight + Map.ResetColor());
        else
            Console.Write(Map.SetForegroundColor(color.r, color.g, color.b) + corner + new string(horizontal[0], width - 2) + corner + Map.ResetColor());

        // Draw sides and content area
        for (int i = 1; i < height - 1; i++)
        {
            Console.SetCursorPosition(x, y + i);
            if (!isLinux)
                Console.Write(Map.SetForegroundColor(color.r, color.g, color.b) + doubleVertical + new string(' ', width - 2) + doubleVertical + Map.ResetColor());
            else
                Console.Write(Map.SetForegroundColor(color.r, color.g, color.b) + vertical + new string(' ', width - 2) + vertical + Map.ResetColor());
        }

        // Draw bottom border with double lines
        Console.SetCursorPosition(x, y + height - 1);
        if (!isLinux)
            Console.Write(Map.SetForegroundColor(color.r, color.g, color.b) + bottomLeft + new string(doubleHorizontal[0], width - 2) + bottomRight + Map.ResetColor());
        else
            Console.Write(Map.SetForegroundColor(color.r, color.g, color.b) + corner + new string(horizontal[0], width - 2) + horizontal + Map.ResetColor());

        // Write the decorated title in the middle of the top of the box if not empty or whitespace
        if (!string.IsNullOrWhiteSpace(title))
        {
            string decoratedTitle = $"{titleLeftDecor} {title} {titleRightDecor}";
            int titleLength = decoratedTitle.Length;
            int padding = (width - 2 - titleLength) / 2;
            int cursorX = x + 1 + padding;
            int cursorY = y;

            if (cursorX + titleLength < Console.WindowWidth && cursorY < Console.WindowHeight)
            {
                Console.SetCursorPosition(cursorX, cursorY);
                Console.Write(Map.SetForegroundColor(ColorSpectrum.DARK_GREEN.r, ColorSpectrum.DARK_GREEN.g, ColorSpectrum.DARK_GREEN.b) + decoratedTitle + Map.ResetColor());
            }
        }

        // Add decorative corners
        if (!isLinux)
        {
            Console.SetCursorPosition(x, y);
            Console.Write(Map.SetForegroundColor(color.r, color.g, color.b), topLeft + Map.ResetColor());
            Console.SetCursorPosition(x + width - 1, y);
            Console.Write(Map.SetForegroundColor(color.r, color.g, color.b), topRight + Map.ResetColor());
            Console.SetCursorPosition(x, y + height - 1);
            Console.Write(Map.SetForegroundColor(color.r, color.g, color.b), bottomLeft + Map.ResetColor());
            Console.SetCursorPosition(x + width - 1, y + height - 1);
            Console.Write(Map.SetForegroundColor(color.r, color.g, color.b), bottomRight + Map.ResetColor());
        }
        else 
        {
            Console.SetCursorPosition(x, y);
            Console.Write(Map.SetForegroundColor(color.r, color.g, color.b), corner + Map.ResetColor());
            Console.SetCursorPosition(x + width - 1, y);
            Console.Write(Map.SetForegroundColor(color.r, color.g, color.b), corner + Map.ResetColor());
            Console.SetCursorPosition(x, y + height - 1);
            Console.Write(Map.SetForegroundColor(color.r, color.g, color.b), corner + Map.ResetColor());
            Console.SetCursorPosition(x + width - 1, y + height - 1);
            Console.Write(Map.SetForegroundColor(color.r, color.g, color.b), corner + Map.ResetColor());
        }
    }
    private void DisplayCenteredTextAtCords(string text, int x, int y, (int r, int g, int b) color)
    {
        string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        int maxLineWidth = lines.Max(l => l.Length);
        int width = maxLineWidth + 2;
        int height = lines.Length + 2;

        int startX = x - width / 2;
        int startY = y - height / 2;

        for (int i = 0; i < lines.Length; i++)
        {
            Console.SetCursorPosition(startX + 1, startY + 1 + i);
            Console.Write(Map.SetForegroundColor(color.r, color.g, color.b) + lines[i] + Map.ResetColor());
        }
    }
    #endregion
    #region map config GUI
    public bool GetConfig()
    {
        DisplayMapConfig();
        Console.ReadKey(true);
        Console.Clear();
        return false;
    }
    string title = @"
   __  _ __  _   _   __ ___   ___  ___    ___
 ,'_/ /// /.' \ / \,' // o.) / _/ / o | ,' _/
/ /_ / ` // o // \,' // o \ / _/ /  ,' _\ `. 
|__//_n_//_n_//_/ /_//___,'/___//_/`_\/___,' 
";
    public void DisplayMapConfig()
    {
        int configWidth = 95;
        int configHeight = Console.WindowHeight;
        int mapConfigOffset = 25;
        int mapConfigHeight = configHeight / 2 - 10;
        int gamerulesHeight = configHeight / 2 - 20;
        int structuresHeight = 15;
        int bottomHeight = configHeight / 2 - 18;
        int heightOffset = (Console.WindowHeight - (10 + gamerulesHeight + structuresHeight + bottomHeight)) / 2;

        (int r, int g, int b) tColor = ColorSpectrum.CYAN;
        (int x, int y) terminalCentre = (Console.WindowWidth / 2, Console.WindowHeight / 2);
        DrawColoredBox(terminalCentre.x - configWidth / 2, terminalCentre.y - configHeight / 2 + heightOffset, configWidth, 10, "", ColorSpectrum.LIGHT_CYAN); // Title
        DrawColoredBox(terminalCentre.x - configWidth / 2 + mapConfigOffset, terminalCentre.y - configHeight / 2 + 10 + heightOffset, configWidth - mapConfigOffset, mapConfigHeight, "Map Config", ColorSpectrum.BURNT_ORANGE); // Map Config
        DrawColoredBox(terminalCentre.x - configWidth / 2 + mapConfigOffset, terminalCentre.y - configHeight / 2 + 10 + configHeight / 2 - 10 + heightOffset, configWidth - mapConfigOffset, gamerulesHeight + structuresHeight - mapConfigHeight, "Events", ColorSpectrum.YELLOW); // Events
        DrawColoredBox(terminalCentre.x - configWidth / 2, terminalCentre.y - configHeight / 2 + 10 + heightOffset, mapConfigOffset - 1, gamerulesHeight, "Gamerules", ColorSpectrum.LIGHT_CORAL); // Gamerules
        DrawColoredBox(terminalCentre.x - configWidth / 2, terminalCentre.y - configHeight / 2 + 10 + configHeight / 2 - 20 + heightOffset, mapConfigOffset - 1, structuresHeight, "Structures", ColorSpectrum.BROWN); // Structures
        DrawColoredBox(terminalCentre.x - configWidth / 2, terminalCentre.y - configHeight / 2 + 10 + configHeight / 2 - 20 + structuresHeight + heightOffset, mapConfigOffset - 1, bottomHeight, "Economy", ColorSpectrum.GREEN); // Economy
        DrawColoredBox(terminalCentre.x - configWidth / 2 + mapConfigOffset, terminalCentre.y + gamerulesHeight + structuresHeight - mapConfigHeight + heightOffset, (configWidth - mapConfigOffset) / 2 - 1, bottomHeight, "Animals", ColorSpectrum.PALE_TURQUOISE); // Animals
        DrawColoredBox(terminalCentre.x - configWidth / 2 + mapConfigOffset + (configWidth - mapConfigOffset) / 2, terminalCentre.y + gamerulesHeight + structuresHeight - mapConfigHeight + heightOffset, (configWidth - mapConfigOffset) / 2, bottomHeight / 2, "Disasters", ColorSpectrum.INDIAN_RED); // Disasters
        DrawColoredBox(terminalCentre.x - configWidth / 2 + mapConfigOffset + (configWidth - mapConfigOffset) / 2, terminalCentre.y + gamerulesHeight + structuresHeight - mapConfigHeight + bottomHeight / 2 + heightOffset, (configWidth - mapConfigOffset) / 2, bottomHeight % 2 == 0 ? bottomHeight / 2 : bottomHeight / 2 + 1, "Visuals", ColorSpectrum.LIGHT_STEEL_BLUE);  // Visuals

        DisplayCenteredTextAtCords(title, terminalCentre.x, terminalCentre.y - configHeight / 2 + heightOffset + 5, tColor);
    }
    #endregion
    #region update functions
    private List<Crab> crabs = new List<Crab>();
    private List<Turtle> turtles = new List<Turtle>();
    public List<Cow> cows = new List<Cow>();
    public List<Sheep> sheeps = new List<Sheep>();
    public void InitializeSpecies(int minSpecies, int maxSpecies, Species species)
    {
        List<char> allowedTiles = new List<char> { };

        if (species is Crab or Turtle)
        {
            allowedTiles.Add('B');
            allowedTiles.Add('b');
        }
        else if (species is Wolf or Bear)
        {
            allowedTiles.Add('F');
        }
        else if (species is Sheep or Cow)
        {
            allowedTiles.Add('P');
        }
        else if (species is Goat)
        {
            allowedTiles.Add('M');
            allowedTiles.Add('m');
        }
        else if (species is Fish)
        {
            allowedTiles.Add('O');
            allowedTiles.Add('o');
            allowedTiles.Add('L');
            allowedTiles.Add('l');
            allowedTiles.Add('R');
            allowedTiles.Add('r');
        }
        else if (species is Bird)
        {
            allowedTiles.Add('F');
            allowedTiles.Add('P');
            allowedTiles.Add('B');
            allowedTiles.Add('b');
            allowedTiles.Add('O');
            allowedTiles.Add('o');
            allowedTiles.Add('L');
            allowedTiles.Add('l');
            allowedTiles.Add('R');
            allowedTiles.Add('r');
            allowedTiles.Add('M');
            allowedTiles.Add('m');
            allowedTiles.Add('S');
        }

        int habitatTiles = 0;
        foreach (char tile in allowedTiles)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (mapData[x, y] == tile)
                    {
                        habitatTiles++;
                    }
                }
            }
        }
        if (habitatTiles < maxSpecies)
        {
            maxSpecies = (int)Math.Round((double)habitatTiles / rng.Next(1, 3));
            minSpecies = 0;
        }
        int numberOfSpecies = 0;
        if (habitatTiles > 0) numberOfSpecies = rng.Next(minSpecies, maxSpecies);
        if (species is Crab)
        {
            for (int i = 0; i < numberOfSpecies; i++)
            {
                try
                {
                    (int x, int y) = GetRandomPointInAllowedTiles(allowedTiles);
                    Crab crab = new Crab(x, y, conf.Width, conf.Height, mapData, seed);
                    crabs.Add(crab);
                    overlayData[x, y] = 'c'; // Represent Crab with 'C'
                }
                catch (InvalidOperationException)
                {
                    // Handle case where no beach biomes are available
                    outputBuffer.Add("No beach biomes available to place crabs.");
                    break;
                }
            }
        }
        else if (species is Turtle)
        {
            for (int i = 0; i < numberOfSpecies; i++)
            {
                try
                {
                    (int x, int y) = GetRandomPointInAllowedTiles(allowedTiles);
                    Turtle turtle = new Turtle(x, y, conf.Width, conf.Height, mapData, overlayData, conf.Height, conf.Width, seed);
                    turtles.Add(turtle);
                    overlayData[x, y] = 'T'; // Represent Turtle with 'T'
                }
                catch (InvalidOperationException)
                {
                    // Handle case where no beach biomes are available
                    outputBuffer.Add("No beach biomes available to place turtles.");
                    break;
                }
            }
        }
    }
    private void InitializeCows(int minGroups, int maxGroups, int minPerGroup, int maxPerGroup)
    {
        int numberOfGroups = rng.Next(minGroups, maxGroups);
        for (int i = 0; i < numberOfGroups; i++)
        {
            int numberOfCows = rng.Next(minPerGroup, maxPerGroup);
            bool validPointFound = false;
            (int startX, int startY) = (0, 0);
            int attempts = 0;

            while (!validPointFound && attempts < 100)
            {
                (startX, startY) = GetRandomPointInAllowedTiles(new List<char> { 'P' });
                if (IsAtLeastDistanceFromMountains(startX, startY, 5))
                {
                    validPointFound = true;
                }
                attempts++;
            }

            if (!validPointFound)
            {
                outputBuffer.Add("Failed to find a valid starting point for cow group.");
                continue;
            }

            for (int j = 0; j < numberOfCows; j++)
            {
                try
                {
                    int iterations = 0;
                    (int x, int y) = (0, 0);
                    bool placedCows = false;
                    while (!placedCows && iterations < 100)
                    {
                        (x, y) = GetRandomPointInRange(startX, startY, 1, 3);
                        if (mapData[x, y] == 'P')
                        {
                            Cow cow = new Cow(x, y, mapData, overlayData, seed);
                            cows.Add(cow);
                            overlayData[x, y] = 'C'; // Represent Cow with 'C'
                            placedCows = true;
                        }
                        iterations++;
                    }
                    if (iterations >= 100)
                    {
                        outputBuffer.Add("Failed to place cows.");
                        break;
                    }
                }
                catch (InvalidOperationException)
                {
                    // Handle case where no plains biomes are available
                    outputBuffer.Add("No plains biomes available to place cows.");
                    break;
                }
            }
        }
    }
    private void InitializeSheeps(int minGroups, int maxGroups, int minPerGroup, int maxPerGroup)
    {
        int numberOfGroups = rng.Next(minGroups, maxGroups);
        for (int i = 0; i < numberOfGroups; i++)
        {
            int numberOfSheeps = rng.Next(minPerGroup, maxPerGroup);
            bool validPointFound = false;
            (int startX, int startY) = (0, 0);
            int attempts = 0;

            while (!validPointFound && attempts < 100)
            {
                (startX, startY) = GetRandomPointInAllowedTiles(new List<char> { 'P' });
                if (IsAtLeastDistanceFromMountains(startX, startY, 5))
                {
                    validPointFound = true;
                }
                attempts++;
            }

            if (!validPointFound)
            {
                outputBuffer.Add("Failed to find a valid starting point for sheep group.");
                continue;
            }

            for (int j = 0; j < numberOfSheeps; j++)
            {
                try
                {
                    int iterations = 0;
                    (int x, int y) = (0, 0);
                    bool placedCows = false;
                    while (!placedCows && iterations < 100)
                    {
                        (x, y) = GetRandomPointInRange(startX, startY, 1, 3);
                        if (mapData[x, y] == 'P')
                        {
                            Sheep sheep = new Sheep(x, y, mapData, overlayData, seed);
                            sheeps.Add(sheep);
                            overlayData[x, y] = 'S'; // Represent Sheep with 'S'
                            placedCows = true;
                        }
                        iterations++;
                    }
                    if (iterations >= 100)
                    {
                        outputBuffer.Add("Failed to place sheeps.");
                        break;
                    }
                }
                catch (InvalidOperationException)
                {
                    // Handle case where no plains biomes are available
                    outputBuffer.Add("No plains biomes available to place sheeps.");
                    break;
                }
            }
        }
    }
    private bool IsAtLeastDistanceFromMountains(int x, int y, int minDistance)
    {
        for (int dx = -minDistance; dx <= minDistance; dx++)
        {
            for (int dy = -minDistance; dy <= minDistance; dy++)
            {
                int checkX = x + dx;
                int checkY = y + dy;
                if (checkX >= 0 && checkX < conf.Width && checkY >= 0 && checkY < conf.Height)
                {
                    if (mapData[checkX, checkY] == 'M' || mapData[checkX, checkY] == 'm')
                    {
                        return false;
                    }
                }
            }
        }
        return true;
    }
    private (int x, int y) GetRandomPointInAllowedTiles(List<char> allowedTiles)
    {
        int maxAttempts = 1000;
        int attempts = 0;
        int x, y;
        do
        {
            x = rng.Next(0, width);
            y = rng.Next(0, height);
            attempts++;
        } while (!allowedTiles.Contains(mapData[x, y]) && attempts < maxAttempts);

        if (attempts >= maxAttempts)
        {
            outputBuffer.Add("Failed to find a valid point.");
        }

        return (x, y);
    }
    public void UpdateCrabs()
    {
        foreach (Crab crab in crabs)
        {
            // Store old position
            int oldX = crab.X;
            int oldY = crab.Y;

            // Update crab behavior
            crab.Behave();

            // Check if new position is already occupied
            if (overlayData[crab.X, crab.Y] != '\0')
            {
                // Assign crab back to old position
                crab.X = oldX;
                crab.Y = oldY;
            }
            else
            {
                // Erase old position from overlayData
                overlayData[oldX, oldY] = '\0';

                // Draw new position on overlayData if not under a cloud
                if (!IsTileUnderCloud(crab.X, crab.Y))
                {
                    overlayData[crab.X, crab.Y] = 'c';
                }
            }
        }
    }
    public void UpdateTurtles()
    {
        foreach (Turtle turtle in turtles)
        {
            // Store old position
            int oldX = turtle.X;
            int oldY = turtle.Y;

            // Update turtle behavior
            turtle.Behave();

            // Check if new position is already occupied
            if (overlayData[turtle.X, turtle.Y] != '\0')
            {
                // Assign turtle back to old position
                turtle.X = oldX;
                turtle.Y = oldY;
            }
            else
            {
                // Erase old position from overlayData
                overlayData[oldX, oldY] = '\0';

                // Draw new position on overlayData if not under a cloud
                if (!IsTileUnderCloud(turtle.X, turtle.Y))
                {
                    overlayData[turtle.X, turtle.Y] = 'T';
                }
            }
        }
    }
    public void UpdateCows()
    {
        foreach (Cow cow in cows)
        {
            // Set time values before behaving
            cow.SetTime(weather.TimeOfDay, sunriseTime, sunsetTime);
            
            // Store old position
            int oldX = cow.X;
            int oldY = cow.Y;

            // Update cow behavior
            cow.Behave();

            // Check if new position is already occupied
            if (overlayData[cow.X, cow.Y] != '\0')
            {
                // Assign cow back to old position
                cow.X = oldX;
                cow.Y = oldY;
            }
            else
            {
                // Erase old position from overlayData
                overlayData[oldX, oldY] = '\0';

                // Draw new position on overlayData if not under a cloud
                if (!IsTileUnderCloud(cow.X, cow.Y))
                {
                    overlayData[cow.X, cow.Y] = 'C';
                }
            }
        }
    }
    public void UpdateSheeps()
    {
        foreach (Sheep sheep in sheeps)
        {
            // Set time values before behaving
            sheep.SetTime(weather.TimeOfDay, sunriseTime, sunsetTime);
            
            // Store old position
            int oldX = sheep.X;
            int oldY = sheep.Y;

            // Update sheep behavior
            sheep.Behave();

            // Check if new position is occupied
            if (overlayData[sheep.X, sheep.Y] != '\0')
            {
                // Assign sheep back to old position
                sheep.X = oldX;
                sheep.Y = oldY;
            }
            else
            {
                // Erase old position from overlayData
                overlayData[oldX, oldY] = '\0';

                // Draw new position on overlayData if not under a cloud
                if (!IsTileUnderCloud(sheep.X, sheep.Y))
                {
                    overlayData[sheep.X, sheep.Y] = 'S';
                }
            }
        }
    }
    #endregion
    public void Update()
    {
        UpdateWeather();
        UpdateGradientDirection(weather.TimeOfDay);
        UpdateCloudProperties();
        UpdateCrabs();
        UpdateTurtles();
        UpdateCows();
        UpdateSheeps();
    }
}