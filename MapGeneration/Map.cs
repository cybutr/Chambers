using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Internal;
using static Internal.GUI;
public partial class Map
{
    private static bool isLinux { get; set; } = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    public static List<string> outputBuffer { get; set; } = new List<string>();
    public List<string> actualOutputBuffer { get; set; } = new List<string>();
    public static List<string> eventBuffer { get; set; } = new List<string>();
    public int cloudShadowOffsetX { get; set; }
    public int cloudShadowOffsetY { get; set; }            
    private double sunriseTime { get; set; }
    private double sunsetTime { get; set; }
    public double time { get; set; }
    public bool isCloudsRendering { get; set; }
    public bool isCloudsShadowsRendering { get; set; }
    public bool shouldSimulationContinue { get; set; } = true;
    public bool debug { get; set; } = false;
    #region map parameters
    public int width { get; set; }
    public int height { get; set; }
    public int cloudDataWidth { get; set; }
    public int cloudDataHeight { get; set; }
    public int cloudDataOffsetX { get; set; }
    public int cloudDataOffsetY { get; set; }
    public char[,] mapData { get; set; }
    public char[,] overlayData { get; set; }
    public char[,] previousOverlayData { get; set; }
    public char[,] previousMapData { get; set; }
    public char[,] cloudData { get; set; }
    public char[,] previousCloudData { get; set; }
    public int[,] cloudDepthData { get; set; }
    public double[,] precipitationData { get; set; }
    public double[,] previousPrecipitationData { get; set; }
    public Random rng { get; set; }
    public double[,] noise { get; set; }
    public double[,] tempatureNoise { get; set; }
    public double[,] humidityNoise { get; set; }
    public int[,] temperatureData { get; set; }
    public int[,] humidityData { get; set; }
    public double avarageTempature { get; set; }
    public double avarageHumidity { get; set; }
    public int topPadding { get; set; }
    public int bottomPadding { get; set; }
    public int leftPadding { get; set; }
    public int rightPadding { get; set; }
    public int seed { get; set; } = 0;
    public Config conf { get; set; }
    private static int numberOfWaves { get; set; }
    public Map()
    {
        // Safe console dimension access with fallback values
        int safeWidth, safeHeight;
        try
        {
            int consoleWidth = Console.WindowWidth;
            int consoleHeight = Console.WindowHeight;
            safeWidth = Math.Max(1, consoleWidth / 2 - GUIConfig.LeftPadding - GUIConfig.RightPadding);
            safeHeight = Math.Max(1, consoleHeight - GUIConfig.BottomPadding - GUIConfig.TopPadding);
        }
        catch
        {
            // Fallback to reasonable defaults if console is not available
            safeWidth = 80;
            safeHeight = 25;
        }
        
        conf = new Config(safeWidth, safeHeight, 10.0, Math.Round(new Random().Next() * ((new Random().NextDouble() - 0.5) * 2)).ToString());
        rng = new Random(seed);
        topPadding = GUIConfig.TopPadding;
        bottomPadding = GUIConfig.BottomPadding;
        leftPadding = GUIConfig.LeftPadding;
        rightPadding = GUIConfig.RightPadding;
        width = Math.Max(1, conf.Width);
        height = Math.Max(1, conf.Height);
        
        // Safety checks to prevent overflow
        cloudDataWidth = Math.Max(1, Math.Min(width * 3, 10000)); // Cap at reasonable max
        cloudDataHeight = Math.Max(1, Math.Min(height * 3, 10000));
        cloudDataOffsetX = width;
        cloudDataOffsetY = height;
        
        // Create arrays with safety checks
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
        cloudIsNight = new bool[width, height];
        cloudIsDarkening = new bool[width, height];
        noise = new double[width, height];
        tempatureNoise = new double[width, height];
        humidityNoise = new double[width, height];
    }
    #endregion
    public void Generate()
    {
        isCloudsShadowsRendering = conf.DisplayShadows;
        seed = Program.ConvertStringToNumbers(conf.Seed);
        rng = new Random(seed);
        noise = Perlin.GeneratePerlinNoise(width, height, conf.NoiseScale, rng.Next());
        tempatureNoise = Perlin.GeneratePerlinNoise(width, height, conf.NoiseScale * 25, rng.Next());
        humidityNoise = Perlin.GeneratePerlinNoise(width, height, conf.NoiseScale * 12, rng.Next());
        if (debug) HandleDebugGen();
        else HandleGen();
    }
    public void HandleGen()
    {
        SetAvarageTempatureHumidity();
        AssignTempAndHumData();
        SmoothOutTempatureHumidity();
        AssignBiomes(noise);
        EnsureMinimumBiomeSize();
        ReplaceBiome('M', 'P');
        if (conf.EnableMountainRanges) CreateMountains();
        if (conf.EnableRivers) CreateRiver();
        if (conf.EnableLakes) CreateLakes();
        CreateComplexFrame();
        SingleTileCheckPF('P', 'F', 2);
        CreateBeaches();
        //CreateStreams();
        WaterDepth();
        FrameMap('@');
        RemoveSeperatedOceanTiles();
        if (conf.GenerateAnimals)
        {
            InitializeSpecies(3, 6, new Crab(0, 0, 0, 0, mapData, seed));
            InitializeSpecies(2, 4, new Turtle(0, 0, 0, 0, mapData, overlayData, width, height, seed));
            InitializeCows(1, 2, 2, 4);
            InitializeSheeps(1, 2, 1, 3);
        }
        if (conf.DisplayWaves) InitializeWaves();
        if (conf.DoWeatherCycle) InitializeWeather();
        if (conf.DoWeatherCycle) InitializeClouds();
    }
    public void HandleDebugGen()
    {
        SetAvarageTempatureHumidity();
        GUI.WriteLine(1);
        AssignTempAndHumData();
        GUI.WriteLine(2);
        SmoothOutTempatureHumidity();
        GUI.WriteLine(3);
        AssignBiomes(noise);
        GUI.WriteLine(4);
        EnsureMinimumBiomeSize();
        GUI.WriteLine(5);
        ReplaceBiome('M', 'P');
        GUI.WriteLine(6);
        if (conf.EnableMountainRanges) CreateMountains();
        GUI.WriteLine(7);
        if (conf.EnableRivers) CreateRiver();
        GUI.WriteLine(8);
        if (conf.EnableLakes) CreateLakes();
        GUI.WriteLine(9);
        CreateComplexFrame();
        GUI.WriteLine(10);
        SingleTileCheckPF('P', 'F', 2);
        GUI.WriteLine(11);
        CreateBeaches();
        GUI.WriteLine(12);
        //CreateStreams();
        WaterDepth();
        GUI.WriteLine(13);
        FrameMap('@');
        GUI.WriteLine(14);
        RemoveSeperatedOceanTiles();
        if (conf.GenerateAnimals)
        {
            InitializeSpecies(3, 6, new Crab(0, 0, 0, 0, mapData, seed));
            GUI.WriteLine(15);
            InitializeSpecies(2, 4, new Turtle(0, 0, 0, 0, mapData, overlayData, width, height, seed));
            GUI.WriteLine(16);
            InitializeCows(1, 2, 2, 4);
            GUI.WriteLine(17);
            InitializeSheeps(1, 2, 1, 3);
            GUI.WriteLine(18);
        }
        if (conf.DisplayWaves) InitializeWaves();
        GUI.WriteLine(19);
        if (conf.DoWeatherCycle) InitializeWeather();
        GUI.WriteLine(20);
        if (conf.DoWeatherCycle) InitializeClouds();
    }
    public void HandleTestGen()
    {
        seed = new Random().Next();
        rng = new Random(seed);
        //noise = Perlin.GeneratePerlinNoise(width, height, conf.NoiseScale, rng.Next());
        //tempatureNoise = Perlin.GeneratePerlinNoise(width, height, conf.NoiseScale * 25, rng.Next());
        //humidityNoise = Perlin.GeneratePerlinNoise(width, height, conf.NoiseScale * 12, rng.Next());
        avarageHumidity = 0.5f;
        avarageTempature = 0.5f;
        if (debug) GUI.WriteLine("1");
        GeneratePlainsOnly();
        if (debug) GUI.WriteLine("2");
        //AssignBiomes(noise);
        //ReplaceBiome('M', 'P');
        //EnsureMinimumBiomeSize();
        //SingleTileCheckPF('F', 'P', 3);
        CreateMountains();
        if (debug) GUI.WriteLine("3");
        CreateLakes();
        if (debug) GUI.WriteLine("4");
        WaterDepth();
        if (debug) GUI.WriteLine("5");
        //FillCircle(GetMapCenter().Item1, GetMapCenter().Item2, 'M', 16, 20);
        FrameMap('@');
        if (debug) GUI.WriteLine("6");
        CreateStreams();
        if (debug) GUI.WriteLine("7");
    }
    public void GeneratePlainsOnly()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                mapData[x, y] = 'P'; // Pure plains
                overlayData[x, y] = ' '; // No overlay
                temperatureData[x, y] = 20; // Neutral temperature
                humidityData[x, y] = 50; // Neutral humidity
                cloudIsNight[x, y] = false;
                cloudIsDarkening[x, y] = false;

                // Neutral noise values
                noise[x, y] = 0.5;
                tempatureNoise[x, y] = 0.5;
                humidityNoise[x, y] = 0.5;
            }
        }
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                previousMapData[x, y] = 'P';
                previousOverlayData[x, y] = ' ';
            }
        }
    }
    #region essential functions
    private void AssignBiomes(double[,] noise)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (noise[x, y] < conf.ForestHeightThreshold)
                {
                    mapData[x, y] = 'F'; // Forest
                }
                else if (noise[x, y] < conf.PlainsHeightThreshold)
                {
                    mapData[x, y] = 'P'; // Plains
                }
                else if (noise[x, y] < conf.MountainHeightThreshold)
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
        bool[,] visited = new bool[width, height];
        int minSize = conf.MinBiomeSize * conf.MinBiomeSize;
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (visited[x, y]) continue;
                
                char biomeType = mapData[x, y];
                List<(int x, int y)> biomeRegion = new List<(int x, int y)>();
                
                // Find all connected tiles of this biome type
                FloodFillRegion(x, y, biomeType, visited, biomeRegion);
                
                // If region is too small, expand it
                if (biomeRegion.Count < minSize)
                {
                    ExpandSmallBiome(biomeRegion, biomeType);
                }
            }
        }
    }

    #endregion
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
                if (rng.NextDouble() > 0.1) // Imperfections
                {
                    mapData[x, y] = '!'; // Replace tile with '!'
                }
            }
            // Delay after each row
            Thread.Sleep(150);
        }
        GUI.SetCursorPosition(0, 9999);
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
    #endregion
    #region weather system
    #region essentials
    public Weather weather {get; set;} = new Weather();
    public List<Cloud> clouds {get; set;} = new List<Cloud>();
    public double deltaTime {get; set;} = 0.5;
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
    public Dictionary<(int x, int y), int> cloudSizes {get; set;} = new Dictionary<(int x, int y), int>();
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
        int weatherRng = rng.Next(0, 2);
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
        weather.Season = rng.NextDouble() * 4.0;
        weather.Temperature = GetTemperature(weather.Season, weather.TimeOfDay, weather.CurrentWeather, avarageTempature);
        weather.Humidity = GetHumidity(weather.CurrentWeather, weather.Temperature, weather.TimeOfDay, weather.Season, avarageHumidity, seed);
        weather.Pressure = GetPressure();
        weather.WindSpeed = rng.NextDouble() * 10.0 + 5.0;
        weather.WindDirection = rng.Next(361);
        sunriseTime = 6.0;
        sunsetTime = 18.0;
    }
    public void UpdateWeather()
    {
        if (conf.DoTimeCycle)
        {
            // Update time of day and season
            weather.TimeOfDay += deltaTime * 24.0 / 720.0; 
            time = weather.TimeOfDay;
            if (weather.TimeOfDay >= 24.0) // One day is 12 minutes
            {
                weather.TimeOfDay -= 24.0;
                dayCount++;
            }

            int daysPerSeason = 10; // Customize the number of days per season
            weather.Season += deltaTime / (daysPerSeason * 720.0);
            if (weather.Season >= 4.0)
                weather.Season -= 4.0;
        }
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
        UpdateWind();
        UpdateCloudShadows();
        UpdateTime();
        UpdateSeason();
        if (conf.DoTimeCycle) UpdateSunTimes(weather.Season);
    }
    public void UpdateSunTimes(double season)
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
    public double timeSinceLastWeatherChange {get; set;} = 0.0;

    // Average times for sunrise and sunset at equinoxes and solstices (in hours)
    public static readonly double equinoxSunrise = 6.0;
    public static readonly double equinoxSunset = 18.0;
    public static readonly double summerSolsticeSunrise = 5.0;
    public static readonly double summerSolsticeSunset = 21.0;
    public static readonly double winterSolsticeSunrise = 7.0;
    public static readonly double winterSolsticeSunset = 17.0;
    public int dayCount {get; set;}
    public double minTimeBetweenChanges {get; set;}
    private WeatherType GetSeasonWeatherType()
    {
        double temperature = weather.Temperature;
        double humidity = weather.Humidity;
        double pressure = weather.Pressure;
        _ = weather.WindSpeed;
        _ = weather.WindDirection;
        _ = weather.TimeOfDay;
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
    public static double weatherFactor {get; set;} = 40;
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
    public double minWindChangeInterval {get; set;} = 30.0; // minimum interval in seconds
    public double maxWindChangeInterval {get; set;} = 90.0; // maximum interval in seconds
    public double windChangeTimer {get; set;}
    public double windTargetDirection {get; set;}
    public bool isTurning {get; set;} = false;

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
                if (IsInCloudBounds(cloudX, cloudY) && cloudData[cloudX, cloudY] != '\0')
                {
                    _ = cloudDepthData[cloudX, cloudY];
                    _ = GetCloudType(cloudData[cloudX, cloudY]);
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
                        if (rng.NextDouble() > 0.5)
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
    public HashSet<(int x, int y)> darkenedPositions {get; set;} = new HashSet<(int x, int y)>();
    public Dictionary<(int x, int y), int> darkenedPositionsIntensities {get; set;} = new Dictionary<(int x, int y), int>();
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
        if (timeOfDay > 0.0 && timeOfDay < 6.0)
        {
            CurrentGradientDirection = GradientDirection.BR_TL;
        }
        else if (timeOfDay > 12.0 && timeOfDay < 18.0)
        {
            CurrentGradientDirection = GradientDirection.TL_BR;
        }
    }
    public void DisplayDayNightTransition(bool displayGUI = true)
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
                            if ((isCloudsRendering && !IsTileUnderCloud(x, y)) || !isCloudsRendering) UpdateTileShadow(x, y, tileShadowIntensity, displayGUI);
                        }
                    }
                    else
                    {
                        // Add new darkened tile
                        darkenedPositionsIntensities[(x, y)] = tileShadowIntensity;
                        darkenedPositions.Add((x, y));

                        // Update tile with shadow
                        if ((isCloudsRendering && !IsTileUnderCloud(x, y)) || !isCloudsRendering) UpdateTileShadow(x, y, tileShadowIntensity, displayGUI);
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
            ResetTileColor(tile.x, tile.y, displayGUI);
        }
    }
    private void UpdateTileShadow(int x, int y, int shadowIntensity, bool displayGUI = true)
    {
        int effectiveLeftPadding = displayGUI ? leftPadding : 0;
        int effectiveTopPadding = displayGUI ? topPadding : 2;
        // Get the base color of the tile
        (int r, int g, int b) baseColor = GetTileBaseColor(x, y);

        // Apply the shadow intensity
        int r = Math.Max(0, baseColor.r - shadowIntensity);
        int g = Math.Max(0, baseColor.g - shadowIntensity);
        int b = Math.Max(0, baseColor.b - shadowIntensity);

        // Update tile with new color
        GUI.SetCursorPosition(effectiveLeftPadding + x * 2, y + effectiveTopPadding);

        if (IsThereAnOverlayTile(x, y))
        {
            (int r, int g, int b) overlayColor = GetOverlayColor(overlayData[x, y]);

            // Apply shadow intensity to overlay color as well
            int or = Math.Max(0, overlayColor.r - shadowIntensity);
            int og = Math.Max(0, overlayColor.g - shadowIntensity);
            int ob = Math.Max(0, overlayColor.b - shadowIntensity);

            string background = GUI.SetBackgroundColor(r, g, b);
            string foreground = GUI.SetForegroundColor(or, og, ob);
            GUI.Write(background + foreground + GetSpeciesIcon(overlayData[x, y]) + GUI.ResetColor());
        }
        else
        {
            string background = GUI.SetBackgroundColor(r, g, b);
            GUI.Write(background + "  " + GUI.ResetColor());
        }
    }
    private void ResetTileColor(int x, int y, bool displayGUI = true)
    {
        int effectiveLeftPadding = displayGUI ? leftPadding : 0;
        int effectiveTopPadding = displayGUI ? topPadding : 2;
        // Get the base color of the tile
        (int r, int g, int b) baseColor = GetTileBaseColor(x, y);

        // Update tile with base color
        GUI.SetCursorPosition(effectiveLeftPadding + x * 2, y + effectiveTopPadding);
        string background = GUI.SetBackgroundColor(baseColor.r, baseColor.g, baseColor.b);
        GUI.Write(background + "  " + GUI.ResetColor());

        if (IsThereAnOverlayTile(x, y))
        {
            (int r, int g, int b) overlayColor = GetOverlayColor(overlayData[x, y]);
            string foreground = GUI.SetForegroundColor(overlayColor.r, overlayColor.g, overlayColor.b);
            GUI.SetCursorPosition(effectiveLeftPadding + x * 2, y + effectiveTopPadding);
            GUI.Write(background + foreground + $"{overlayData[x, y]}" + GUI.ResetColor());
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
    public void DisplayDarkenedTiles(bool displayGUI = true)
    {
        int effectiveLeftPadding = displayGUI ? leftPadding : 0;
        int effectiveTopPadding = displayGUI ? topPadding : 2;
        foreach ((int x, int y) in darkenedPositions)
        {
            GUI.SetCursorPosition(effectiveLeftPadding + x * 2, y + effectiveTopPadding);
            (int r, int g, int b) color = GetDarkenedColor(x, y);
            GUI.Write(GUI.SetBackgroundColor(color.r, color.g, color.b) + "  " + GUI.ResetColor());
        }
    }
    public void DisplayDarkenedWaveTiles(bool displayGUI = true)
    {
        int effectiveLeftPadding = displayGUI ? leftPadding : 0;
        int effectiveTopPadding = displayGUI ? topPadding : 2;
        foreach ((int x, int y) in wavePositions)
        {
            GUI.SetCursorPosition(effectiveLeftPadding + x * 2, y + effectiveTopPadding);
            (int r, int g, int b) color = GetDarkenedColor(x, y);
            GUI.Write(GUI.SetBackgroundColor(color.r, color.g, color.b) + "  " + GUI.ResetColor());
        }
    }
    
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
                        char tile = mapData[x, y];
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

                        (int r, int g, int b) waveColor = GetColor('O', x, y);
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
                    if (IsInCloudBounds(cloudX, cloudY) && cloudData[cloudX, cloudY] != '\0')
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
    public void DisplayMap(bool displayGUI = true)
    {
        // Adjust padding based on GUI state
        int effectiveLeftPadding = displayGUI ? leftPadding : 0;
        int effectiveTopPadding = displayGUI ? topPadding : 2;

        // Don't clear console, just move cursor to start position
        GUI.SetCursorPosition(effectiveLeftPadding, effectiveTopPadding);

        for (int y = 0; y < height; y++)
        {
            // Set cursor position at start of each line
            GUI.SetCursorPosition(effectiveLeftPadding, y + effectiveTopPadding);
            
            for (int x = 0; x < width; x++)
            {
                (int r, int g, int b) color = GetColor(mapData[x, y], x, y);
                GUI.Write(GUI.SetBackgroundColor(color.r, color.g, color.b) + "  " + GUI.ResetColor());
            }
        }
        DisplayDarkenedTiles(displayGUI);
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
                    GUI.SetCursorPosition(effectiveLeftPadding + x * 2, y + effectiveTopPadding);
                    (int r, int g, int b) bgColor = GetDarkenedColor(x, y);
                    (int r, int g, int b) fgColor = GetOverlayColor(overlayData[x, y]);
                    string bg = GUI.SetBackgroundColor(bgColor.r, bgColor.g, bgColor.b);
                    string fg = GUI.SetForegroundColor(fgColor.r, fgColor.g, fgColor.b);
                    GUI.Write(bg + fg + GetSpeciesIcon(overlayData[x, y]) + GUI.ResetColor());
                }
            }
        }
        GUI.ResetColor();
        //Update();
        
        // Only run animations and updates when simulation is active
        if (Program.isUpdating)
        {
            AnimateWater(displayGUI);
            DisplayCloudShadows(displayGUI);
            if (isCloudsRendering) RenderClouds(displayGUI);
        }
        else
        {
            // When paused, draw current state without updating
            DrawCurrentDarkness(displayGUI);
            DrawCurrentWaves(displayGUI);
            DrawCurrentCloudShadows(displayGUI);
            DrawCurrentClouds(displayGUI);
        }

        if (displayGUI) DisplayGUI();
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
    public void UpdateTile(int x, int y, bool displayGUI = true)
    {
        int effectiveLeftPadding = displayGUI ? leftPadding : 0;
        int effectiveTopPadding = displayGUI ? topPadding : 2;
        GUI.SetCursorPosition(effectiveLeftPadding + x * 2, y + effectiveTopPadding);
        (int r, int g, int b) bgColor = GetColor(mapData[x, y], x, y); // Retrieve background color from ColorSpectrum
        string background = GUI.SetBackgroundColor(bgColor.r, bgColor.g, bgColor.b);
        GUI.Write(background + "  " + GUI.ResetColor());
    }
    public void UpdateOverlayTile(int x, int y, bool displayGUI = true)
    {
        int effectiveLeftPadding = displayGUI ? leftPadding : 0;
        int effectiveTopPadding = displayGUI ? topPadding : 2;
        // Prevent overlay data from being displayed on the edges
        if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
        {
            return;
        }
        UpdateTile(x, y, displayGUI);

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

        string background = GUI.SetBackgroundColor(bgColor.r, bgColor.g, bgColor.b);
        string foreground = GUI.SetForegroundColor(fgColor.r, fgColor.g, fgColor.b);

        if (!isCloudsRendering || (isCloudsRendering && !IsTileUnderCloud(x, y)))
        {
            // Write the background color first
            GUI.SetCursorPosition(effectiveLeftPadding + x * 2, y + effectiveTopPadding);
            GUI.Write(background + "  " + GUI.ResetColor());

            // Write the overlay character with the correct background and foreground colors
            GUI.SetCursorPosition(effectiveLeftPadding + x * 2, y + effectiveTopPadding);
            GUI.Write(background + "  " + GUI.ResetColor());
            GUI.SetCursorPosition(effectiveLeftPadding + x * 2, y + effectiveTopPadding);
            GUI.Write(background + foreground + GetSpeciesIcon(overlayData[x, y]) + GUI.ResetColor());
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
    public void DisplayCharacterOnTile(int x, int y, char character, string characterColor, bool displayGUI = true)
    {
        int effectiveLeftPadding = displayGUI ? leftPadding : 0;
        int effectiveTopPadding = displayGUI ? topPadding : 2;
        // Move cursor to position
        GUI.SetCursorPosition(effectiveLeftPadding + x * 2, y + effectiveTopPadding);

        // Get RGB color based on characterColor using ColorSpectrum
        (int r, int g, int b) rgb = GetRGBFromColorCode(characterColor);
        string fg = GUI.SetForegroundColor(rgb.r, rgb.g, rgb.b);
        GUI.Write(fg + character + GUI.ResetColor());
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
        if (conf.EnableTempatureBiomeChanges)
        {
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

        }
        // Adjust based on humidity zone
        if (conf.EnableHumidityBiomeChanges)
        {
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

    #endregion
    #region temperature and humidity noise
    public void RenderTemperatureNoise()
    {
        GUI.DrawGrid(width, height, leftPadding, topPadding, (x, y) => {
            int tempValue = temperatureData[x, y];
            return TemperatureZoneToColor(tempValue);
        });
    }
    public void RenderHumidityNoise()
    {
        GUI.DrawGrid(width, height, leftPadding, topPadding, (x, y) => {
            int humidityValue = humidityData[x, y];
            return HumidityZoneToColor(humidityValue);
        });
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
            GUI.Clear();
            GUI.SetCursorPosition(0, 0);
            GUI.Write("Please resize the console window to a larger size.");
            shouldSimulationContinue = false;
            return;
        }

        // Define margins
        int leftMargin = leftPadding;
        int topMargin = topPadding;
        int rightMargin = rightPadding;
        int bottomMargin = bottomPadding;

        // Calculate content area dimensions
        _ = consoleWidth - leftMargin - rightMargin;
        _ = consoleHeight - topMargin - bottomMargin;

        if (consoleWidth >= config.MinConsoleWidth && consoleHeight >= config.MinConsoleHeight)
        {
            // Weather Radar
            DrawBox(0, 0, config.RadarWidth, config.RadarHeight, "Weather Radar");
            GUI.SetCursorPosition(2, 1);
            GUI.Write($"Not Implemented Yet");
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
            GUI.Clear();
            GUI.SetCursorPosition(0, 0);
            GUI.Write("Please resize the console window to a larger size.");
            shouldSimulationContinue = false;
        }
        if (rightMargin >= 20)
        {
            // Help Info
            DisplayHelpInfo(config.HelpWidth, config.HelpHeight);
            // Tile Info
            DisplayTileInfo(config.TileWidth, config.TileHeight);
        }

        GUI.SetCursorPosition(0, height + topMargin - 1);
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
        GUI.SetCursorPosition(radarWidth + 1, 1);
        GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}Current: {currentWeatherDisplay}, Next: {nextWeatherDisplay}{GUI.ResetColor()}");
        DrawBox(radarWidth - 1, 2, statsWidth + 2, statsHeight, " ");
        GUI.SetCursorPosition(radarWidth + 1, 3);
        GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.CYAN.r, ColorSpectrum.CYAN.g, ColorSpectrum.CYAN.b)}Cloud Formations: {GetCloudFormations()}{GUI.ResetColor()}");
        GUI.SetCursorPosition(radarWidth + 1, 4);
        GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.CYAN.r, ColorSpectrum.CYAN.g, ColorSpectrum.CYAN.b)}Cloud Tiles: {GetCloudTilesCount()}{GUI.ResetColor()}");
        GUI.SetCursorPosition(radarWidth + 1, 5);
        GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.GREEN.r, ColorSpectrum.GREEN.g, ColorSpectrum.GREEN.b)}Temperature: {temperature}°C{GUI.ResetColor()}");
        GUI.SetCursorPosition(radarWidth + 1, 6);
        GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.BLUE.r, ColorSpectrum.BLUE.g, ColorSpectrum.BLUE.b)}Humidity: {humidity}%{GUI.ResetColor()}");
        GUI.SetCursorPosition(radarWidth + 1, 7);
        GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.MAGENTA.r, ColorSpectrum.MAGENTA.g, ColorSpectrum.MAGENTA.b)}Pressure: {pressure}hPa{GUI.ResetColor()}");
        GUI.SetCursorPosition(radarWidth + 1, 8);
        GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.ORANGE.r, ColorSpectrum.ORANGE.g, ColorSpectrum.ORANGE.b)}Wind Speed: {windSpeed}m/s{GUI.ResetColor()}");
        GUI.SetCursorPosition(radarWidth + 1, 9);
        GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.PURPLE.r, ColorSpectrum.PURPLE.g, ColorSpectrum.PURPLE.b)}Wind Direction: {windDirection}°{GUI.ResetColor()}");
    }
    private void UpdateWeatherStats(WeatherType currentWeather, WeatherType nextWeather, double temperature, double humidity,
    double pressure, double windSpeed, double windDirection, int radarWidth, int statsWidth, int statsHeight)
    {
        string currentWeatherDisplay = statsWidth < 43 ? GetShortWeatherName(currentWeather) : currentWeather.ToString();
        string nextWeatherDisplay = statsWidth < 43 ? GetShortWeatherName(nextWeather) : nextWeather.ToString();

        GUI.SetCursorPosition(radarWidth + 1, 1);
        GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}Current: {currentWeatherDisplay}, Next: {nextWeatherDisplay}{GUI.ResetColor()}    ");
        GUI.SetCursorPosition(radarWidth + 1, 5);
        GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.GREEN.r, ColorSpectrum.GREEN.g, ColorSpectrum.GREEN.b)}Temperature: {temperature}°C{GUI.ResetColor()}   ");
        GUI.SetCursorPosition(radarWidth + 1, 6);
        GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.BLUE.r, ColorSpectrum.BLUE.g, ColorSpectrum.BLUE.b)}Humidity: {humidity}%{GUI.ResetColor()}   ");
        GUI.SetCursorPosition(radarWidth + 1, 7);
        GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.MAGENTA.r, ColorSpectrum.MAGENTA.g, ColorSpectrum.MAGENTA.b)}Pressure: {pressure}hPa{GUI.ResetColor()}   ");
        GUI.SetCursorPosition(radarWidth + 1, 8);
        GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.ORANGE.r, ColorSpectrum.ORANGE.g, ColorSpectrum.ORANGE.b)}Wind Speed: {windSpeed}m/s{GUI.ResetColor()}   ");
        GUI.SetCursorPosition(radarWidth + 1, 9);
        GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.PURPLE.r, ColorSpectrum.PURPLE.g, ColorSpectrum.PURPLE.b)}Wind Direction: {windDirection}°{GUI.ResetColor()}   ");
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
        GUI.SetCursorPosition(radarWidth + statsWidth, 1);
        GUI.Write($" {GUI.SetForegroundColor(ColorSpectrum.LIGHT_BLUE.r, ColorSpectrum.LIGHT_BLUE.g, ColorSpectrum.LIGHT_BLUE.b)}Time: {time:F2}h{GUI.ResetColor()}   ");
        GUI.SetCursorPosition(radarWidth + statsWidth, 2);
        GUI.Write($" {GUI.SetForegroundColor(ColorSpectrum.LIGHT_GREEN.r, ColorSpectrum.LIGHT_GREEN.g, ColorSpectrum.LIGHT_GREEN.b)}Season: {season}{GUI.ResetColor()}   ");
        GUI.SetCursorPosition(radarWidth + statsWidth, 3);
        GUI.Write($" {GUI.SetForegroundColor(ColorSpectrum.ORANGE.r, ColorSpectrum.ORANGE.g, ColorSpectrum.ORANGE.b)}Sunrise: {sunriseTime}h{GUI.ResetColor()}   ");
        GUI.SetCursorPosition(radarWidth + statsWidth, 4);
        GUI.Write($" {GUI.SetForegroundColor(ColorSpectrum.ORANGE.r, ColorSpectrum.ORANGE.g, ColorSpectrum.ORANGE.b)}Sunset: {sunsetTime}h{GUI.ResetColor()}  ");
        GUI.SetCursorPosition(radarWidth + statsWidth, 5);
        if (time < sunriseTime)
        {
            GUI.Write($" {GUI.SetForegroundColor(ColorSpectrum.GREEN.r, ColorSpectrum.GREEN.g, ColorSpectrum.GREEN.b)}Time Until Sunrise: {sunriseTime - time:F2}h{GUI.ResetColor()}");
        }
        else if (time >= sunriseTime && time < sunsetTime)
        {
            GUI.Write($" {GUI.SetForegroundColor(ColorSpectrum.RED.r, ColorSpectrum.RED.g, ColorSpectrum.RED.b)}Time Until Sunset: {sunsetTime - time:F2}h{GUI.ResetColor()}");
        }
        else
        {
            double timeUntilMidnight = 24.0 - time;
            double timeUntilSunrise = timeUntilMidnight + sunriseTime;
            GUI.Write($" {GUI.SetForegroundColor(ColorSpectrum.GREEN.r, ColorSpectrum.GREEN.g, ColorSpectrum.GREEN.b)}Time Until Sunrise: {timeUntilSunrise:F2}h{GUI.ResetColor()}");
        }
        GUI.SetCursorPosition(radarWidth + statsWidth, 6);
        GUI.Write($" {GUI.SetForegroundColor(ColorSpectrum.PINK.r, ColorSpectrum.PINK.g, ColorSpectrum.PINK.b)}Day: {dayCount}{GUI.ResetColor()}   ");
        
    }
    private void UpdateTimeInfo(double time, double season, int radarWidth, int statsWidth, int statsHeight)
    {
        GUI.SetCursorPosition(radarWidth + statsWidth, 1);
        GUI.Write($" {GUI.SetForegroundColor(ColorSpectrum.LIGHT_BLUE.r, ColorSpectrum.LIGHT_BLUE.g, ColorSpectrum.LIGHT_BLUE.b)}Time: {time:F2}h{GUI.ResetColor()}   ");
        GUI.SetCursorPosition(radarWidth + statsWidth, 2);
        GUI.Write($" {GUI.SetForegroundColor(ColorSpectrum.LIGHT_GREEN.r, ColorSpectrum.LIGHT_GREEN.g, ColorSpectrum.LIGHT_GREEN.b)}Season: {season}{GUI.ResetColor()}   ");
        GUI.SetCursorPosition(radarWidth + statsWidth, 3);
        GUI.Write($" {GUI.SetForegroundColor(ColorSpectrum.ORANGE.r, ColorSpectrum.ORANGE.g, ColorSpectrum.ORANGE.b)}Sunrise: {sunriseTime}h{GUI.ResetColor()}   ");
        GUI.SetCursorPosition(radarWidth + statsWidth, 4);
        GUI.Write($" {GUI.SetForegroundColor(ColorSpectrum.ORANGE.r, ColorSpectrum.ORANGE.g, ColorSpectrum.ORANGE.b)}Sunset: {sunsetTime}h{GUI.ResetColor()}  ");
        GUI.SetCursorPosition(radarWidth + statsWidth, 5);
        if (time < sunriseTime)
        {
            GUI.Write($" {GUI.SetForegroundColor(ColorSpectrum.GREEN.r, ColorSpectrum.GREEN.g, ColorSpectrum.GREEN.b)}Time Until Sunrise: {sunriseTime - time:F2}h{GUI.ResetColor()}   ");
        }
        else if (time >= sunriseTime && time < sunsetTime)
        {
            GUI.Write($" {GUI.SetForegroundColor(ColorSpectrum.RED.r, ColorSpectrum.RED.g, ColorSpectrum.RED.b)}Time Until Sunset: {sunsetTime - time:F2}h{GUI.ResetColor()}   ");
        }
        else
        {
            double timeUntilMidnight = 24.0 - time;
            double timeUntilSunrise = timeUntilMidnight + sunriseTime;
            GUI.Write($" {GUI.SetForegroundColor(ColorSpectrum.GREEN.r, ColorSpectrum.GREEN.g, ColorSpectrum.GREEN.b)}Time Until Sunrise: {timeUntilSunrise:F2}h{GUI.ResetColor()}   ");
        }
        GUI.SetCursorPosition(radarWidth + statsWidth, 6);
        GUI.Write($" {GUI.SetForegroundColor(ColorSpectrum.PINK.r, ColorSpectrum.PINK.g, ColorSpectrum.PINK.b)}Day: {dayCount}{GUI.ResetColor()}   ");
    }
    private void DisplayHelpInfo(int helpWidth, int helpHeight)
    {
        string line = new string('-', helpWidth - 3);
        if (helpHeight < 30)
        {
            DrawBox(Console.WindowWidth - rightPadding * 2, Console.WindowHeight - bottomPadding - helpHeight, helpWidth, helpHeight, "Help Menu");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 1);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}P/Space:{GUI.ResetColor()}");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 2);
            GUI.Write("Toggle updating");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 3);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}PgUp/PgDn:{GUI.ResetColor()}");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 4);
            GUI.Write("Increase/Decrease updating speed");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 5);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}Q:{GUI.ResetColor()}");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 6);
            GUI.Write("Toggle cloud rendering");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 7);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}Up/Down:{GUI.ResetColor()}");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 8);
            GUI.Write("Go to last/first chamber");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 9);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}Left/Right:{GUI.ResetColor()}");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 10);
            GUI.Write("Previous/Next chamber");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 11);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}1 - 9:{GUI.ResetColor()}");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 12);
            GUI.Write("Go to chamber 1 - 9");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 13);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}C:{GUI.ResetColor()}");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 14);
            GUI.Write("Open console");
        }
        else
        {
            DrawBox(Console.WindowWidth - rightPadding * 2, Console.WindowHeight - bottomPadding - helpHeight, helpWidth, helpHeight, "Help Menu");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 1);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}P/Space:{GUI.ResetColor()}");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 2);
            GUI.Write("Toggle updating");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 3);
            GUI.Write(line);
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 4);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}PgUp/PgDn:{GUI.ResetColor()}");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 5);
            GUI.Write("Increase/Decrease updating speed");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 6);
            GUI.Write(line);
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 7);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}Q:{GUI.ResetColor()}");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 8);
            GUI.Write("Toggle cloud rendering");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 9);
            GUI.Write(line);
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 10);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}Up/Down:{GUI.ResetColor()}");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 11);
            GUI.Write("Go to last/first chamber");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 12);
            GUI.Write(line);
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 13);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}Left/Right:{GUI.ResetColor()}");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 14);
            GUI.Write("Previous/Next chamber");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 15);
            GUI.Write(line);
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 16);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}1 - 9:{GUI.ResetColor()}");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 17);
            GUI.Write("Go to chamber 1 - 9");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 18);
            GUI.Write(line);
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 19);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}C:{GUI.ResetColor()}");
            GUI.SetCursorPosition(Console.WindowWidth - rightPadding * 2 + 2, Console.WindowHeight - bottomPadding - helpHeight + 20);
            GUI.Write("Open console");
        }
    }
    private void DisplayTileInfo(int tileWidth, int tileHeight)
    {
        DrawBox(Console.WindowWidth - tileWidth, 0 + topPadding + 1, tileWidth, tileHeight, "Tile Info");
        if (rightPadding >= 20)
        {
            GUI.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 1 + topPadding + 1);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.DARK_GREEN.r, ColorSpectrum.DARK_GREEN.g, ColorSpectrum.DARK_GREEN.b)}Dark Green:{GUI.ResetColor()} Forest");
            GUI.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 2 + topPadding + 1);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.GREEN.r, ColorSpectrum.GREEN.g, ColorSpectrum.GREEN.b)}Green:{GUI.ResetColor()} Plains");
            GUI.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 3 + topPadding + 1);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.GREY.r, ColorSpectrum.GREY.g, ColorSpectrum.GREY.b)}Grey / {GUI.SetForegroundColor(ColorSpectrum.DARK_GREY.r, ColorSpectrum.DARK_GREY.g, ColorSpectrum.DARK_GREY.b)}Dark Gray:{GUI.ResetColor()} Mountain");
            GUI.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 4 + topPadding + 1);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.WHITE.r, ColorSpectrum.WHITE.g, ColorSpectrum.WHITE.b)}White:{GUI.ResetColor()} Snow Peak");
            GUI.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 5 + topPadding + 1);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.BLUE.r, ColorSpectrum.BLUE.g, ColorSpectrum.BLUE.b)}Blue:{GUI.ResetColor()} Water");
            GUI.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 6 + topPadding + 1);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}Yellow:{GUI.ResetColor()} Beach");
        }
        else
        {
            GUI.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 1 + topPadding + 1);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.DARK_GREEN.r, ColorSpectrum.DARK_GREEN.g, ColorSpectrum.DARK_GREEN.b)}Dark Green:{GUI.ResetColor()}");
            GUI.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 2 + topPadding + 1);
            GUI.Write("Forest");
            GUI.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 3 + topPadding + 1);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.GREEN.r, ColorSpectrum.GREEN.g, ColorSpectrum.GREEN.b)}Green:{GUI.ResetColor()}");
            GUI.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 4 + topPadding + 1);
            GUI.Write("Plains");
            GUI.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 5 + topPadding + 1);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.GREY.r, ColorSpectrum.GREY.g, ColorSpectrum.GREY.b)}Grey / {GUI.SetForegroundColor(ColorSpectrum.DARK_GREY.r, ColorSpectrum.DARK_GREY.g, ColorSpectrum.DARK_GREY.b)}Dark Gray:{GUI.ResetColor()}");
            GUI.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 6 + topPadding + 1);
            GUI.Write("Mountain");
            GUI.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 7 + topPadding + 1);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.WHITE.r, ColorSpectrum.WHITE.g, ColorSpectrum.WHITE.b)}White:{GUI.ResetColor()}");
            GUI.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 8 + topPadding + 1);
            GUI.Write("Snow Peak");
            GUI.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 9 + topPadding + 1);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.BLUE.r, ColorSpectrum.BLUE.g, ColorSpectrum.BLUE.b)}Blue:{GUI.ResetColor()}");
            GUI.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 10 + topPadding + 1);
            GUI.Write("Water");
            GUI.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 11 + topPadding + 1);
            GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b)}Yellow:{GUI.ResetColor()}");
            GUI.SetCursorPosition(Console.WindowWidth - tileWidth + 2, 12 + topPadding + 1);
            GUI.Write("Beach");
        }
    }
    private void DisplayThanksMessage(int thanksWidth, int thanksHeight)
    {
        string thanks = "Thanks";
        string line = new string('-', thanksWidth - 3);
        string halfLine = new string('-', thanksWidth / 2 - 2 - thanks.Length / 2 - 1);
        DrawBox(Console.WindowWidth - thanksWidth, 0, thanksWidth, thanksHeight, "Other");
        GUI.SetCursorPosition(Console.WindowWidth - thanksWidth + 2, 1);
        GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.PURPLE.r, ColorSpectrum.PURPLE.g, ColorSpectrum.PURPLE.b)}Thanks for playing!{GUI.ResetColor()}");
        GUI.SetCursorPosition(Console.WindowWidth - thanksWidth + 2, 2);
        GUI.Write(line);
        GUI.SetCursorPosition(Console.WindowWidth - thanksWidth + 2, 3);
        GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.CYAN.r, ColorSpectrum.CYAN.g, ColorSpectrum.CYAN.b)}This project was created as{GUI.ResetColor()}");
        GUI.SetCursorPosition(Console.WindowWidth - thanksWidth + 2, 4);
        GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.CYAN.r, ColorSpectrum.CYAN.g, ColorSpectrum.CYAN.b)}a starting project for learning C#. {GUI.ResetColor()}");
        GUI.SetCursorPosition(Console.WindowWidth - thanksWidth + 2, 5);
        GUI.Write(line);
        GUI.SetCursorPosition(Console.WindowWidth - thanksWidth + 2, thanksHeight - 4);
        GUI.Write($"{halfLine}{GUI.SetForegroundColor(ColorSpectrum.GREEN.r, ColorSpectrum.GREEN.g, ColorSpectrum.GREEN.b)} {thanks} {GUI.ResetColor()}{halfLine}");
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
        GUI.SetCursorPosition(nameStartX, titleY);
        GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.CYAN.r, ColorSpectrum.CYAN.g, ColorSpectrum.CYAN.b)}{name}{GUI.ResetColor()}");

        DrawBox(Console.WindowWidth / 2 - titleWidth / 2, 2, titleWidth, titleHeight, " ");
        // Centered and fancy signature
        int centerX = Console.WindowWidth / 2;
        int centerY = topPadding / 2;

        string signature1 = "** Made by: @cybutr **";
        string signature2 = "* On GitHub *";

        GUI.SetCursorPosition(centerX - signature1.Length / 2, centerY);
        GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.MAGENTA.r, ColorSpectrum.MAGENTA.g, ColorSpectrum.MAGENTA.b)}{signature1}{GUI.ResetColor()}");

        GUI.SetCursorPosition(centerX - signature2.Length / 2, centerY + 1);
        GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.CYAN.r, ColorSpectrum.CYAN.g, ColorSpectrum.CYAN.b)}{signature2}{GUI.ResetColor()}");
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
        GUI.SetCursorPosition(xPosition, 1);
        GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.CYAN.r, ColorSpectrum.CYAN.g, ColorSpectrum.CYAN.b)}{text}{GUI.ResetColor()}");
        DrawBox(startX, startY, outputWidth, outputHeight - 3, " ");
        int cursorX = startX + 2;
        int cursorY = startY + 1;

        int maxLines = outputHeight - 5;
        _ = Math.Min(actualOutputBuffer.Count, maxLines);

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
            GUI.SetCursorPosition(cursorX, cursorY + i);
            foreach (string word in words)
            {
                string trimmedWord = word.Trim(',', '.', '!', '?'); // Trim punctuation
                if (keywordColors.ContainsKey(trimmedWord))
                {
                    (int r, int g, int b) color = keywordColors[trimmedWord];
                    GUI.Write($"{GUI.SetForegroundColor(color.r, color.g, color.b)}{word}{Reset} ");
                }
                else
                {
                    GUI.Write($"{word} ");
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
        GUI.SetCursorPosition(xPosition, 1);
        GUI.Write($"{GUI.SetForegroundColor(ColorSpectrum.CYAN.r, ColorSpectrum.CYAN.g, ColorSpectrum.CYAN.b)}{text}{GUI.ResetColor()}");
        int cursorX = startX + 2;
        int cursorY = startY + 1;

        int maxLines = outputHeight - 5;
        _ = Math.Min(actualOutputBuffer.Count, maxLines);

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
                        GUI.SetCursorPosition(currentX, currentY);
                        GUI.Write($"{GUI.SetForegroundColor(color.r, color.g, color.b)}{firstPart}{Reset}");
                    }
                    else
                    {
                        GUI.SetCursorPosition(currentX, currentY);
                        GUI.Write($"{firstPart}");
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
                    GUI.SetCursorPosition(currentX, currentY);
                    GUI.Write($"{GUI.SetForegroundColor(color.r, color.g, color.b)}{word}{Reset} ");
                }
                else
                {
                    GUI.SetCursorPosition(currentX, currentY);
                    GUI.Write($"{word} ");
                }

                currentX += wordLength;
            }
        }
    }

    #endregion
    #region map config GUI
    public bool GetConfig()
    {
        DisplayMapConfig();
        GUI.SetCursorPosition(terminalCentre.x - saveWidth / 2 + saveWidth / 2 - 2 , terminalCentre.y + gamerulesHeight + structuresHeight - mapConfigHeight + heightOffset + bottomHeight + 1);
        GUI.Write(
            GUI.SetBackgroundColor(selectColor.r, selectColor.g, selectColor.b) +
            GUI.SetForegroundColor(ColorSpectrum.BLACK.r, ColorSpectrum.BLACK.g, ColorSpectrum.BLACK.b) +
            $"SAVE{GUI.ResetColor()}"
        );
        ManageParamNavigation();
        numberOfWaves = conf.NumberOfWaves;
        GUI.Clear();
        return false;
    }
    #region params
    public enum SettingType
    {
        MapConfig,
        Gamerules,
        Structures,
        Economy,
        Animals,
        Disasters,
        Visuals,
        Events
    } 
    public enum ParamType
    {
        Int,
        Double,
        Bool,
        String
    }
    public class ParamCoordinate
    {
        public string PropertyName { get; set; }
        public SettingType SettingType { get; set; }
        public ParamType ParamType { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        public ParamCoordinate(string propertyName, SettingType settingType, ParamType type, int x, int y)
        {
            PropertyName = propertyName;
            SettingType = settingType;
            ParamType = type;
            X = x;
            Y = y;
        }
    }
    public static List<ParamCoordinate> mapConfigParams {get; set;} = new List<ParamCoordinate>
    {
        new ParamCoordinate("Seed", SettingType.MapConfig, ParamType.String, 0, 0),
        new ParamCoordinate("NoiseScale", SettingType.MapConfig, ParamType.Double, 0, 0),
        new ParamCoordinate("ErosionFactor", SettingType.MapConfig, ParamType.Double, 0, 0),
        new ParamCoordinate("MinBiomeSize", SettingType.MapConfig, ParamType.Int, 0, 0),
        new ParamCoordinate("MinLakeSize", SettingType.MapConfig, ParamType.Int, 0, 0),
        new ParamCoordinate("MinRiverWidth", SettingType.MapConfig, ParamType.Int, 0, 0),
        new ParamCoordinate("MaxRiverWidth", SettingType.MapConfig, ParamType.Int, 0, 0),
        new ParamCoordinate("MinMountainWidth", SettingType.MapConfig, ParamType.Int, 0, 0),
        new ParamCoordinate("MaxMountainWidth", SettingType.MapConfig, ParamType.Int, 0, 0),
        new ParamCoordinate("RiverFlowChance", SettingType.MapConfig, ParamType.Double, 0, 0),
        new ParamCoordinate("PlainsHeightThreshold", SettingType.MapConfig, ParamType.Double, 0, 0),
        new ParamCoordinate("ForestHeightThreshold", SettingType.MapConfig, ParamType.Double, 0, 0),
        new ParamCoordinate("MountainHeightThreshold", SettingType.MapConfig, ParamType.Double, 0, 0),
        new ParamCoordinate("EnableRivers", SettingType.MapConfig, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableLakes", SettingType.MapConfig, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableMountainRanges", SettingType.MapConfig, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableTempatureBiomeChanges", SettingType.MapConfig, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableHumidityBiomeChanges", SettingType.MapConfig, ParamType.Bool, 0, 0),
        new ParamCoordinate("BiomeBlend", SettingType.MapConfig, ParamType.Int, 0, 0)
    };
    public static List<ParamCoordinate> gameruleParams {get; set;} = new List<ParamCoordinate>
    {
        new ParamCoordinate("EnableWildfires", SettingType.Gamerules, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableSecrets", SettingType.Gamerules, ParamType.Bool, 0, 0),
        new ParamCoordinate("DoTimeCycle", SettingType.Gamerules, ParamType.Bool, 0, 0),
        new ParamCoordinate("DoWeatherCycle", SettingType.Gamerules, ParamType.Bool, 0, 0)
    };
    public static List<ParamCoordinate> structureParams {get; set;} = new List<ParamCoordinate>
    {
        new ParamCoordinate("GenerateStructrs", SettingType.Structures, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableVillages", SettingType.Structures, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableCities", SettingType.Structures, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableDungeons", SettingType.Structures, ParamType.Bool, 0, 0)
    };
    public static List<ParamCoordinate> economyParams {get; set;} = new List<ParamCoordinate>
    {
        new ParamCoordinate("EnableTrades", SettingType.Economy, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableTrades", SettingType.Economy, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableCurrency", SettingType.Economy, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableTaxes", SettingType.Economy, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableBanks", SettingType.Economy, ParamType.Bool, 0, 0)
    };
    public static List<ParamCoordinate> animalParams {get; set;} = new List<ParamCoordinate>
    {
        new ParamCoordinate("GenerateAnimals", SettingType.Animals, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnablePredators", SettingType.Animals, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableAnimalMovement", SettingType.Animals, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableAnimalBreeding", SettingType.Animals, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableAnimalDeath", SettingType.Animals, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableAnimalExtinction", SettingType.Animals, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableAnimalMigration", SettingType.Animals, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableAnimalHunting", SettingType.Animals, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableAnimalDomestication", SettingType.Animals, ParamType.Bool, 0, 0)
    };
    public static List<ParamCoordinate> disasterParams {get; set;} = new List<ParamCoordinate>
    {
        new ParamCoordinate("EnableTornadoes", SettingType.Disasters, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableEarthquakes", SettingType.Disasters, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableVolcanoes", SettingType.Disasters, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableFloods", SettingType.Disasters, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableMeteors", SettingType.Disasters, ParamType.Bool, 0, 0)
    };
    public static List<ParamCoordinate> eventParams {get; set;} = new List<ParamCoordinate>
    {
        new ParamCoordinate("EnableRobberies", SettingType.Events, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableMurders", SettingType.Events, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableRiots", SettingType.Events, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnablePlagues", SettingType.Events, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableWars", SettingType.Events, ParamType.Bool, 0, 0)
    };
    public static List<ParamCoordinate> visualParams {get; set;} = new List<ParamCoordinate>
    {
        new ParamCoordinate("DisplayShadows", SettingType.Visuals, ParamType.Bool, 0, 0),
        new ParamCoordinate("DisplayWaves", SettingType.Visuals, ParamType.Bool, 0, 0),
        new ParamCoordinate("NumberOfWaves", SettingType.Visuals, ParamType.Int, 0, 0)
    };
    public static List<ParamCoordinate> nullParams {get; set;} = new List<ParamCoordinate>();
    public static List<ParamCoordinate>[,] allParams {get; set;} = new List<ParamCoordinate>[4, 3]
    {
        { gameruleParams, mapConfigParams, nullParams },
        { structureParams, eventParams, nullParams },
        { economyParams, animalParams, disasterParams },
        { nullParams, nullParams, visualParams }
    };
    #endregion
    public static string title {get; set;} = @"
   __  _ __  _   _   __ ___   ___  ___    ___
 ,'_/ /// /.' \ / \,' // o.) / _/ / o | ,' _/
/ /_ / ` // o // \,' // o \ / _/ /  ,' _\ `. 
|__//_n_//_n_//_/ /_//___,'/___//_/`_\/___,' 
";
    private int GetConfWindowWidth(List<ParamCoordinate> paramList)
    {
        switch (paramList)
        {
            case List<ParamCoordinate> _ when paramList == mapConfigParams:
                return configWidth - mapConfigOffset;
            case List<ParamCoordinate> _ when paramList == gameruleParams:
                return mapConfigOffset - 1;
            case List<ParamCoordinate> _ when paramList == structureParams:
                return mapConfigOffset - 1;
            case List<ParamCoordinate> _ when paramList == economyParams:
                return mapConfigOffset - 1;
            case List<ParamCoordinate> _ when paramList == animalParams:
                return (configWidth - mapConfigOffset) / 2 - 1;
            case List<ParamCoordinate> _ when paramList == disasterParams:
                return (configWidth - mapConfigOffset) / 2;
            case List<ParamCoordinate> _ when paramList == visualParams:
                return (configWidth - mapConfigOffset) / 2;
            case List<ParamCoordinate> _ when paramList == eventParams:
                return configWidth - mapConfigOffset;
            default:
                return 20;
        }
    }
    public static int currentParamX {get; set;}
    public static int currentParamY {get; set;}
    public static int configWidth {get; set;} = 95;
    public static int configHeight {get; set;} = 62;
    public static int mapConfigOffset {get; set;} = 25;
    public static int mapConfigHeight {get; set;} = configHeight / 2 - 10;
    public static int gamerulesHeight {get; set;} = configHeight / 2 - 20;
    public static int structuresHeight {get; set;} = 15;
    public static int bottomHeight {get; set;} = configHeight / 2 - 16;
    public static int saveWidth {get; set;} = 10;
    public static int heightOffset {get; set;} = Math.Max(0, (Console.WindowHeight - (10 + gamerulesHeight + structuresHeight + bottomHeight)) / 6);
    public (int r, int g, int b) selectColor {get; set;} = ColorSpectrum.SILVER;
    public static (int x, int y) terminalCentre {get; set;} = (Console.WindowWidth / 2, Console.WindowHeight / 2);
    public void CalculateParamCoordinates()
    {
        int centerX = Console.WindowWidth / 2;
        int startY =  terminalCentre.y - configHeight / 2 + heightOffset;
    
        // Calculate the starting X position based on configWidth to center the window
        int startX = centerX - (configWidth / 2);
    
        startY += 11;
        // Assign coordinates for Gamerules
        foreach (var param in gameruleParams)
        {
            param.X = startX + 2;
            param.Y = startY++;
        }
    
        startY += gamerulesHeight - gameruleParams.Count(); // Add spacing between sections
    
        // Assign coordinates for Structures
        foreach (var param in structureParams)
        {
            param.X = startX + 2;
            param.Y = startY++;
        }
    
        startY -= structureParams.Count() + 11; // Add spacing between sections
    
        // Assign coordinates for Map Config
        foreach (var param in mapConfigParams)
        {
            param.X = startX + mapConfigOffset + 2;
            param.Y = startY++;
        }
    
        startY += structuresHeight - structureParams.Count() * 2; // Add spacing between sections
    
        // Assign coordinates for Economy
        foreach (var param in economyParams)
        {
            param.X = startX + 2;
            param.Y = startY++;
        }
    
        startY -= economyParams.Count(); // Add spacing between sections
    
        // Assign coordinates for Animals
        foreach (var param in animalParams)
        {
            param.X = startX + mapConfigOffset + 2;
            param.Y = startY++;
        }
    
        startY -= animalParams.Count(); // Add spacing between sections
    
        // Assign coordinates for Disasters
        foreach (var param in disasterParams)
        {
            param.X = startX + mapConfigOffset + ((configWidth - mapConfigOffset) / 2) + 2;
            param.Y = startY++;
        }
    
        startY += 2; // Add spacing between sections
    
        // Assign coordinates for Visuals
        foreach (var param in visualParams)
        {
            param.X = startX + mapConfigOffset + ((configWidth - mapConfigOffset) / 2) + 2;
            param.Y = startY++;
        }
    
        startY -= animalParams.Count() * 2 - 3; // Add spacing between sections
    
        // Assign coordinates for Events
        for (int i = 0; i < eventParams.Count(); i++)
        {
            if (i < gamerulesHeight + structuresHeight - mapConfigHeight - 2)
            {
                eventParams[i].X = startX + mapConfigOffset + 2;
                eventParams[i].Y = startY++;
            }
            else
            {
                eventParams[i].X = startX + configWidth - eventParams[i].PropertyName.Length - 6;
                eventParams[i].Y = startY++ - gamerulesHeight - structuresHeight + mapConfigHeight + 2;
            }
        }
    
        // Ensure all parameters have valid coordinates
        foreach (var list in new List<List<ParamCoordinate>> 
        { 
            mapConfigParams, 
            gameruleParams, 
            structureParams, 
            economyParams, 
            animalParams, 
            disasterParams, 
            visualParams, 
            eventParams 
        })
        {
            foreach (var param in list)
            {
                // Clamp X and Y to console boundaries
                param.X = Math.Clamp(param.X, 0, Console.WindowWidth - 1);
                param.Y = Math.Clamp(param.Y, 0, Console.WindowHeight - 1);
            }
        }
    }
    private void DrawAllParams()
    {
        foreach (var param in mapConfigParams)
        {
            GUI.SetCursorPosition(param.X, param.Y);
            GUI.Write($"{param.PropertyName} - {GetParamValue(param)}");
        }
        foreach (var param in gameruleParams)
        {
            GUI.SetCursorPosition(param.X, param.Y);
            GUI.Write($"{param.PropertyName} - {GetParamValue(param)}");
        }
        foreach (var param in structureParams)
        {
            GUI.SetCursorPosition(param.X, param.Y);
            GUI.Write($"{param.PropertyName} - {GetParamValue(param)}");
        }
        foreach (var param in economyParams)
        {
            GUI.SetCursorPosition(param.X, param.Y);
            GUI.Write($"{param.PropertyName} - {GetParamValue(param)}");
        }
        foreach (var param in animalParams)
        {
            GUI.SetCursorPosition(param.X, param.Y);
            GUI.Write($"{param.PropertyName} - {GetParamValue(param)}");
        }
        foreach (var param in disasterParams)
        {
            GUI.SetCursorPosition(param.X, param.Y);
            GUI.Write($"{param.PropertyName} - {GetParamValue(param)}");
        }
        foreach (var param in visualParams)
        {
            GUI.SetCursorPosition(param.X, param.Y);
            GUI.Write($"{param.PropertyName} - {GetParamValue(param)}");
        }
        foreach (var param in eventParams)
        {
            GUI.SetCursorPosition(param.X, param.Y);
            GUI.Write($"{param.PropertyName} - {GetParamValue(param)}");
        }
    }
    public string GetParamValue(ParamCoordinate param)
    {
        switch (param.ParamType)
        {
            case ParamType.Bool:
                return GetParamBool(param);
            case ParamType.Int:
                return GetParamInt(param).ToString();
            case ParamType.Double:
                return GetParamDouble(param).ToString();
            case ParamType.String:
                return GetParamString(param);
            default:
                return "";
        }
    }
    private string GetParamBool(ParamCoordinate param)
    {
        // Get the property value from conf using reflection
        bool value = conf.GetType().GetProperty(param.PropertyName)?.GetValue(conf) as bool? ?? false;
        // Choose the display character based on the value
        string displayChar = value ? "✔" : "✖";

        // Return the display character instead of writing to console
        return displayChar;
    }
    private int GetParamInt(ParamCoordinate param)
    {
        // Get the property value from conf using reflection
        int value = conf.GetType().GetProperty(param.PropertyName)?.GetValue(conf) as int? ?? 0;

        // Return the value instead of writing to console
        return value;
    }
    private double GetParamDouble(ParamCoordinate param)
    {
        // Get the property value from conf using reflection
        double value = conf.GetType().GetProperty(param.PropertyName)?.GetValue(conf) as double? ?? 0.0f;

        // Return the value instead of writing to console
        return value;
    }
    private string GetParamString(ParamCoordinate param)
    {
        // Get the property value from conf using reflection
        string value = conf.GetType().GetProperty(param.PropertyName)?.GetValue(conf) as string ?? "";

        // Return the value instead of writing to console
        return value;
    }
    public void ManageParamNavigation()
    {
        void DrawParam(ParamCoordinate param, int redrawDistance, bool isSelected, string? tempStringValue = null)
        {
            GUI.SetCursorPosition(param.X, param.Y);
            // Base value from config
            string value = param.ParamType switch
            {
                ParamType.Bool => GetParamBool(param),
                ParamType.Int => GetParamInt(param).ToString(),
                ParamType.Double => GetParamDouble(param).ToString("0.##"),
                ParamType.String => GetParamString(param),
                _ => ""
            };
            // If we're editing any parameter type, show the temporary string instead
            if (tempStringValue != null && (param.ParamType == ParamType.String || param.ParamType == ParamType.Int || param.ParamType == ParamType.Double))
            {
                value = tempStringValue;
            }

            GUI.SetCursorPosition(param.X, param.Y);
            if (isSelected)
            {
                if (param.ParamType != ParamType.Bool)
                {
                    GUI.Write(
                        GUI.SetBackgroundColor(selectColor.r, selectColor.g, selectColor.b) +
                        GUI.SetForegroundColor(ColorSpectrum.BLACK.r, ColorSpectrum.BLACK.g, ColorSpectrum.BLACK.b) +
                        $"{param.PropertyName} - {value}{GUI.ResetColor()}{new string(' ', Math.Max(redrawDistance, 0))}"
                    );
                }
                else
                {
                    GUI.Write(
                        GUI.SetBackgroundColor(selectColor.r, selectColor.g, selectColor.b) +
                        GUI.SetForegroundColor(ColorSpectrum.BLACK.r, ColorSpectrum.BLACK.g, ColorSpectrum.BLACK.b) +
                        $"{param.PropertyName} - {value}{GUI.ResetColor()}"
                    );
                }
            }
            else
            {
                if (param.ParamType != ParamType.Bool)
                    GUI.Write($"{param.PropertyName} - {value}{new string(' ', redrawDistance)}");
                else
                    GUI.Write($"{param.PropertyName} - {value}");
            }
        }

        currentParamX = 2;
        currentParamY = 1;
        List<ParamCoordinate> currentList = allParams[currentParamX, currentParamY];
        int listParamIndex = currentList.Count - 1;
        int redrawDistance = listParamIndex >= 0
            ? GetRedrawDistance(currentList, currentList[listParamIndex]) : 0;
        
        bool typingString = false;
        string tempStringValue = "";
        bool isSave = true;
        bool selecting = true;
        while (selecting)
        {
            ConsoleKeyInfo key = Console.ReadKey(true);
            if (!typingString)
            {
                int oldIndex = listParamIndex;
                int oldX = currentParamX;
                int oldY = currentParamY;
                ParamCoordinate ?oldParam = oldIndex >= 0 ? currentList[oldIndex] : null;
                int oldRedrawDistance = oldParam != null
                    ? GetRedrawDistance(currentList, oldParam, true) : 0;
                switch (key.Key)
                {
                    case ConsoleKey.Escape:
                        conf.ShouldSave = false;
                        return;
                    case ConsoleKey.UpArrow:
                    case ConsoleKey.W:
                        // Special 2-column logic for x=1, y=1
                        if (currentParamX == 1 && currentParamY == 1 && currentList.Count > 1)
                        {
                            int halfCount = (int)Math.Ceiling(currentList.Count / 2.0);
                            bool isLeftSide = listParamIndex < halfCount;
                            if (isLeftSide)
                            {
                                // If not at top, move up; else go x-1
                                if (listParamIndex > 0)
                                    listParamIndex--;
                                else
                                {
                                    currentParamX--;
                                    currentList = allParams[currentParamX, currentParamY];
                                    listParamIndex = currentList.Count - 1;
                                }
                            }
                            else
                            {
                                // If not at top of right, move up; else go x-1
                                if (listParamIndex > halfCount)
                                    listParamIndex--;
                                else
                                {
                                    currentParamX--;
                                    currentList = allParams[currentParamX, currentParamY];
                                    listParamIndex = currentList.Count - 1;
                                }
                            }
                        }
                        else
                        {
                            if (listParamIndex > 0 && !isSave)
                                listParamIndex--;
                            else if (currentParamX == 2 && currentParamY == 1 & !isSave)
                            {
                                currentParamX--;
                                currentList = eventParams;
                                listParamIndex = (int)Math.Ceiling(currentList.Count / 2.0) - 1;
                            }
                            else if (currentParamX == 2 && currentParamY == 2 && !isSave)
                            {
                                currentParamX--;
                                currentParamY--;
                                currentList = eventParams;
                                listParamIndex = currentList.Count - 1;
                            }
                            else if (!isSave)
                            {
                                if (currentParamX > 0
                                    && allParams[currentParamX - 1, currentParamY] != null
                                    && allParams[currentParamX - 1, currentParamY].Count > 0)
                                {
                                    currentParamX--;
                                    currentList = allParams[currentParamX, currentParamY];
                                    listParamIndex = currentList.Count - 1;
                                }
                            }
                            else isSave = false;
                        }
                        break;

                    case ConsoleKey.DownArrow:
                    case ConsoleKey.S:
                        // Special 2-column logic for x=1, y=1
                        if (currentParamX == 1 && currentParamY == 1 && currentList.Count > 1)
                        {
                            int halfCount = (int)Math.Ceiling(currentList.Count / 2.0);
                            bool isLeftSide = listParamIndex < halfCount;
                            if (isLeftSide)
                            {
                                // Go down if not bottom; else x + 1
                                if (listParamIndex < halfCount - 1)
                                    listParamIndex++;
                                else
                                {
                                    currentParamX++;
                                    currentList = allParams[currentParamX, currentParamY];
                                    listParamIndex = 0;
                                }
                            }
                            else
                            {
                                // Go down if not bottom; else x+1
                                if (listParamIndex < currentList.Count - 1)
                                    listParamIndex++;
                                else
                                {
                                    currentParamX++;
                                    currentParamY++;
                                    currentList = allParams[currentParamX, currentParamY];
                                    listParamIndex = 0;
                                }
                            }
                        }
                        else
                        {
                            if (listParamIndex == currentList.Count - 1 && currentParamY == 1 && currentParamX == 2 && !isSave)
                            {
                                isSave = true;
                                DrawParam(currentList[listParamIndex], redrawDistance, false);
                            }
                            else if (listParamIndex < currentList.Count - 1 && listParamIndex >= 0 && !isSave)
                                listParamIndex++;
                            else if (!isSave)
                            {
                                if (currentParamX < allParams.GetLength(0) - 1
                                    && allParams[currentParamX + 1, currentParamY] != null
                                    && allParams[currentParamX + 1, currentParamY].Count > 0)
                                {
                                    currentParamX++;
                                    currentList = allParams[currentParamX, currentParamY];
                                    listParamIndex = 0;
                                }
                            }
                        }
                        break;

                    case ConsoleKey.LeftArrow:
                    case ConsoleKey.A:
                        // Check if Ctrl is held for numeric value modification
                        if ((key.Modifiers & ConsoleModifiers.Control) != 0 && listParamIndex >= 0 && !isSave)
                        {
                            var param = currentList[listParamIndex];
                            if (param.ParamType == ParamType.Int || param.ParamType == ParamType.Double)
                            {
                                ModifyParamValue(param, false); // Decrease value
                                DrawParam(param, redrawDistance, true);
                                break;
                            }
                        }
                        
                        // Special 2-column logic for x=1, y=1
                        if (currentParamX == 1 && currentParamY == 1 && currentList.Count > 1)
                        {
                            int halfCount = (int)Math.Ceiling(currentList.Count / 2.0);
                            bool isLeftSide = listParamIndex < halfCount;
                            if (!isLeftSide)
                            {
                                // Move to first item on left side
                                listParamIndex = 0;
                            }
                            else
                            {
                                // Regular behavior if on left already
                                if (currentParamY > 0
                                    && allParams[currentParamX, currentParamY - 1] != null
                                    && allParams[currentParamX, currentParamY - 1].Count > 0)
                                {
                                    currentParamY--;
                                    currentList = allParams[currentParamX, currentParamY];
                                    listParamIndex = 0;
                                }
                            }
                        }
                        else
                        {
                            if (currentParamY == 2 && currentParamX == 3 && !isSave)
                            {
                                currentParamY--;
                                currentParamX--;
                                currentList = allParams[currentParamX, currentParamY];
                                listParamIndex = currentList.Count - 1;
                            }
                            else if (currentParamY > 0
                                && allParams[currentParamX, currentParamY - 1] != null
                                && allParams[currentParamX, currentParamY - 1].Count > 0
                                && !isSave)
                            {
                                currentParamY--;
                                currentList = allParams[currentParamX, currentParamY];
                                listParamIndex = 0;
                            }
                        }
                        break;

                    case ConsoleKey.RightArrow:
                    case ConsoleKey.D:
                        // Check if Ctrl is held for numeric value modification
                        if ((key.Modifiers & ConsoleModifiers.Control) != 0 && listParamIndex >= 0 && !isSave)
                        {
                            var param = currentList[listParamIndex];
                            if (param.ParamType == ParamType.Int || param.ParamType == ParamType.Double)
                            {
                                ModifyParamValue(param, true); // Increase value
                                DrawParam(param, redrawDistance, true);
                                break;
                            }
                        }
                        
                        // Special 2-column logic for x=1, y=1
                        if (currentParamX == 1 && currentParamY == 1 && currentList.Count > 1)
                        {
                            int halfCount = (int)Math.Ceiling(currentList.Count / 2.0);
                            bool isLeftSide = listParamIndex < halfCount;
                            if (isLeftSide)
                            {
                                // Move to the right side at matching row
                                listParamIndex = halfCount + listParamIndex;
                                if (listParamIndex >= currentList.Count) listParamIndex = currentList.Count - 1;
                            }
                        }
                        else
                        {
                            if (currentParamX == 2 && currentParamY == 1 && listParamIndex > (int)Math.Ceiling(currentList.Count / 2.0) - 1 && !isSave)
                            {
                                currentParamX++;
                                currentParamY++;
                                currentList = allParams[currentParamX, currentParamY];
                                listParamIndex = 0;
                            }
                            else if (currentParamY < allParams.GetLength(1) - 1
                                && allParams[currentParamX, currentParamY + 1] != null
                                && allParams[currentParamX, currentParamY + 1].Count > 0
                                && !isSave)
                            {
                                currentParamY++;
                                currentList = allParams[currentParamX, currentParamY];
                                listParamIndex = 0;
                            }
                        }
                        break;
                    case ConsoleKey.Spacebar:
                    case ConsoleKey.Enter:
                        if (listParamIndex >= 0 && !isSave)
                        {
                            var p = currentList[listParamIndex];
                            if (p.ParamType == ParamType.Bool)
                            {
                                ToggleBoolParam(p);
                                DrawParam(p, redrawDistance, true);
                            }
                            else if (p.ParamType == ParamType.String)
                            {
                                typingString = true;
                                tempStringValue = GetParamString(p);
                                DrawParam(p, redrawDistance, true, tempStringValue);
                            }
                            else if (p.ParamType == ParamType.Int || p.ParamType == ParamType.Double)
                            {
                                // Start editing mode for numeric values
                                typingString = true;
                                tempStringValue = p.ParamType == ParamType.Int ? 
                                    GetParamInt(p).ToString() : 
                                    GetParamDouble(p).ToString("0.##");
                                DrawParam(p, redrawDistance, true, tempStringValue);
                            }
                        }
                        else selecting = false;
                        break;
                }

                if (oldIndex >= 0 && oldParam != null && oldIndex < (allParams[oldX, oldY] ?? new()).Count && !isSave)
                {
                    DrawParam(allParams[oldX, oldY][oldIndex], oldRedrawDistance, false);
                }
                if (listParamIndex >= 0 && listParamIndex < currentList.Count && !isSave)
                {
                    redrawDistance = GetRedrawDistance(currentList, currentList[listParamIndex]);
                    DrawParam(currentList[listParamIndex], redrawDistance, true);
                }
                else if (isSave)
                {
                    GUI.SetCursorPosition(terminalCentre.x - saveWidth / 2 + saveWidth / 2 - 2 , terminalCentre.y + gamerulesHeight + structuresHeight - mapConfigHeight + heightOffset + bottomHeight + 1);
                    GUI.Write(
                        GUI.SetBackgroundColor(selectColor.r, selectColor.g, selectColor.b) +
                        GUI.SetForegroundColor(ColorSpectrum.BLACK.r, ColorSpectrum.BLACK.g, ColorSpectrum.BLACK.b) +
                        $"SAVE{GUI.ResetColor()}"
                    );
                }
                if (!isSave)
                {
                    GUI.SetCursorPosition(terminalCentre.x - saveWidth / 2 + saveWidth / 2 - 2 , terminalCentre.y + gamerulesHeight + structuresHeight - mapConfigHeight + heightOffset + bottomHeight + 1);
                    GUI.Write("SAVE");
                }
            }
            else
            {
                if (key.Key == ConsoleKey.Backspace)
                {
                    if (tempStringValue.Length > 0)
                    {
                        tempStringValue = tempStringValue[..^1];
                        DrawParam(currentList[listParamIndex], redrawDistance, true, tempStringValue);
                    }
                }
                else if (key.Key == ConsoleKey.Enter)
                {
                    typingString = false;
                    if (listParamIndex >= 0)
                    {
                        var param = currentList[listParamIndex];
                        object? parsedValue = null;
                        
                        // Parse the input based on parameter type
                        switch (param.ParamType)
                        {
                            case ParamType.String:
                                parsedValue = tempStringValue;
                                break;
                            case ParamType.Int:
                                if (int.TryParse(tempStringValue, out int intVal))
                                    parsedValue = intVal;
                                else
                                {
                                    // Invalid input, revert to original value
                                    tempStringValue = GetParamInt(param).ToString();
                                    DrawParam(param, redrawDistance, true);
                                    continue;
                                }
                                break;
                            case ParamType.Double:
                                if (double.TryParse(tempStringValue, out double doubleVal))
                                    parsedValue = Math.Round(doubleVal, 2);
                                else
                                {
                                    // Invalid input, revert to original value
                                    tempStringValue = GetParamDouble(param).ToString("0.##");
                                    DrawParam(param, redrawDistance, true);
                                    continue;
                                }
                                break;
                        }
                        
                        if (parsedValue != null)
                        {
                            SetParamValue(param, parsedValue, conf);
                        }
                        DrawParam(param, redrawDistance, true);
                    }
                }
                else if (key.Key == ConsoleKey.Escape)
                {
                    typingString = false;
                    if (listParamIndex >= 0)
                    {
                        var param = currentList[listParamIndex];
                        // Restore original value based on parameter type
                        tempStringValue = param.ParamType switch
                        {
                            ParamType.String => GetParamString(param),
                            ParamType.Int => GetParamInt(param).ToString(),
                            ParamType.Double => GetParamDouble(param).ToString("0.##"),
                            _ => ""
                        };
                        DrawParam(param, redrawDistance, true);
                    }
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    if (tempStringValue.Length < 20 && listParamIndex >= 0)
                    {
                        var param = currentList[listParamIndex];
                        bool isValidChar = param.ParamType switch
                        {
                            ParamType.String => true, // Allow any character for strings
                            ParamType.Int => char.IsDigit(key.KeyChar) || (key.KeyChar == '-' && tempStringValue.Length == 0), // Digits and minus at start
                            ParamType.Double => char.IsDigit(key.KeyChar) || key.KeyChar == '.' || (key.KeyChar == '-' && tempStringValue.Length == 0), // Digits, decimal point, and minus at start
                            _ => false
                        };

                        if (isValidChar)
                        {
                            // Additional validation for decimal point in doubles
                            if (param.ParamType == ParamType.Double && key.KeyChar == '.' && tempStringValue.Contains('.'))
                            {
                                // Don't allow multiple decimal points
                                return;
                            }

                            tempStringValue += key.KeyChar;
                            DrawParam(param, redrawDistance, true, tempStringValue);
                        }
                    }
                }
            }
            // GUI.SetCursorPosition(0, 0);
            // GUI.Write(isSave);
        }
    }
    private int GetRedrawDistance(List<ParamCoordinate> paramList, ParamCoordinate param, bool old = false)
    {
        return !old ? Math.Max(GetConfWindowWidth(paramList) - 2 - param.PropertyName.Length - 6 - GetParamValue(param).Length, 0) :
        Math.Max(GetConfWindowWidth(paramList) - 2 - param.PropertyName.Length - 6 - GetParamValue(param).Length, 0);
    }
    private void ModifyParamValue(ParamCoordinate param, bool increase)
    {
        if (param.ParamType == ParamType.Int)
        {
            int val = GetParamInt(param);
            int newVal = val + (increase ? 1 : -1);
            
            // Add reasonable constraints for certain parameters
            if (param.PropertyName.Contains("Size") || param.PropertyName.Contains("Width"))
            {
                newVal = Math.Max(1, newVal); // Minimum size of 1
            }
            else if (param.PropertyName == "NumberOfWaves")
            {
                newVal = Math.Max(0, Math.Min(100, newVal)); // Waves between 0-100
            }
            
            SetParamValue(param, newVal, conf);
        }
        else if (param.ParamType == ParamType.Double)
        {
            double val = GetParamDouble(param);
            double increment = increase ? 0.1 : -0.1;
            
            // Use different increment for scale values
            if (param.PropertyName.Contains("Scale"))
            {
                increment = increase ? 1.0 : -1.0;
            }
            
            double newVal = val + increment;
            
            // Add reasonable constraints
            if (param.PropertyName.Contains("Scale"))
            {
                newVal = Math.Max(0.1, newVal); // Minimum scale
            }
            else if (param.PropertyName.Contains("Factor"))
            {
                newVal = Math.Max(0, newVal); // Non-negative factors
            }
            
            SetParamValue(param, Math.Round(newVal, 2), conf);
        }
    }
    private void ToggleBoolParam(ParamCoordinate param)
    {
        bool current = GetParamBool(param) == "✔";
        SetParamValue(param, !current, conf);
    }
    private static void SetParamValue(ParamCoordinate param, object newValue, Config config)
    {
        var prop = config.GetType().GetProperty(param.PropertyName);
        if (prop != null && prop.CanWrite)
        {
            try
            {
                var convertedValue = Convert.ChangeType(newValue, prop.PropertyType);
                prop.SetValue(config, convertedValue);
            }
            catch (Exception ex)
            {
                GUI.WriteLine($"Error setting property {param.PropertyName}: {ex.Message}");
            }
        }
    }
    public void SelectParam(ParamCoordinate param)
    {
        string value = param.ParamType switch
        {
            ParamType.Bool => GetParamBool(param),
            ParamType.Int => GetParamInt(param).ToString(),
            ParamType.Double => GetParamDouble(param).ToString(),
            ParamType.String => GetParamString(param),
            _ => ""
        };
        GUI.SetCursorPosition(param.X, param.Y);
        GUI.Write(
            $"{GUI.SetBackgroundColor(selectColor.r, selectColor.g, selectColor.g)}" +
            $"{GUI.SetForegroundColor(ColorSpectrum.BLACK.r, ColorSpectrum.BLACK.g, ColorSpectrum.BLACK.b)}" +
            $"{param.PropertyName} - {value}{GUI.ResetColor()}"
        );
    }
    public void DisplayMapConfig()
    {
        GUI.Clear();
        (int r, int g, int b) tColor = ColorSpectrum.CYAN;
        DrawColoredBox(terminalCentre.x - configWidth / 2, terminalCentre.y - configHeight / 2 + heightOffset, configWidth, 10, "", ColorSpectrum.LIGHT_CYAN); // Title
        DrawColoredBox(terminalCentre.x - configWidth / 2 + mapConfigOffset, terminalCentre.y - configHeight / 2 + 10 + heightOffset, configWidth - mapConfigOffset, mapConfigHeight, "Map Config", ColorSpectrum.BURNT_ORANGE); // Map Config
        DrawColoredBox(terminalCentre.x - configWidth / 2 + mapConfigOffset, terminalCentre.y - configHeight / 2 + 10 + configHeight / 2 - 10 + heightOffset, configWidth - mapConfigOffset, gamerulesHeight + structuresHeight - mapConfigHeight, "Events", ColorSpectrum.YELLOW); // Events
        DrawColoredBox(terminalCentre.x - configWidth / 2, terminalCentre.y - configHeight / 2 + 10 + heightOffset, mapConfigOffset - 1, gamerulesHeight, "Gamerules", ColorSpectrum.LIGHT_CORAL); // Gamerules
        DrawColoredBox(terminalCentre.x - configWidth / 2, terminalCentre.y - configHeight / 2 + 10 + configHeight / 2 - 20 + heightOffset, mapConfigOffset - 1, structuresHeight, "Structures", ColorSpectrum.BROWN); // Structures
        DrawColoredBox(terminalCentre.x - configWidth / 2, terminalCentre.y - configHeight / 2 + 10 + configHeight / 2 - 20 + structuresHeight + heightOffset, mapConfigOffset - 1, bottomHeight, "Economy", ColorSpectrum.GREEN); // Economy
        DrawColoredBox(terminalCentre.x - configWidth / 2 + mapConfigOffset, terminalCentre.y + gamerulesHeight + structuresHeight - mapConfigHeight + heightOffset, (configWidth - mapConfigOffset) / 2 - 1, bottomHeight, "Animals", ColorSpectrum.PALE_TURQUOISE); // Animals
        DrawColoredBox(terminalCentre.x - configWidth / 2 + mapConfigOffset + (configWidth - mapConfigOffset) / 2, terminalCentre.y + gamerulesHeight + structuresHeight - mapConfigHeight + heightOffset, (configWidth - mapConfigOffset) / 2, bottomHeight / 2, "Disasters", ColorSpectrum.INDIAN_RED); // Disasters
        DrawColoredBox(terminalCentre.x - configWidth / 2 + mapConfigOffset + (configWidth - mapConfigOffset) / 2, terminalCentre.y + gamerulesHeight + structuresHeight - mapConfigHeight + bottomHeight / 2 + heightOffset, (configWidth - mapConfigOffset) / 2, bottomHeight % 2 == 0 ? bottomHeight / 2 : bottomHeight / 2 + 1, "Visuals", ColorSpectrum.LIGHT_STEEL_BLUE);  // Visuals
        DrawColoredBox(terminalCentre.x - saveWidth / 2, terminalCentre.y + gamerulesHeight + structuresHeight - mapConfigHeight + heightOffset + bottomHeight, saveWidth, 3, "", ColorSpectrum.LIGHT_CYAN); // Bottom
        DisplayCenteredTextAtCords(title, terminalCentre.x, terminalCentre.y - configHeight / 2 + heightOffset + 5, tColor);
        GUI.SetCursorPosition(terminalCentre.x - saveWidth / 2 + saveWidth / 2 - 2 , terminalCentre.y + gamerulesHeight + structuresHeight - mapConfigHeight + heightOffset + bottomHeight + 1);
        GUI.Write("SAVE");
        CalculateParamCoordinates();
        DrawAllParams();
    }
    #endregion
    #region update funcstions
    public List<Crab> crabs {get; set;} = new List<Crab>();
    public List<Turtle> turtles {get; set;} = new List<Turtle>();
    public List<Cow> cows {get; set;} = new List<Cow>();
    public List<Sheep> sheeps {get; set;} = new List<Sheep>();
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
                    Crab crab = new Crab(x, y, conf.Width, conf.Height, mapData, rng.Next());
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
                    Turtle turtle = new Turtle(x, y, conf.Width, conf.Height, mapData, overlayData, conf.Height, conf.Width, rng.Next());
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
                            Cow cow = new Cow(x, y, mapData, overlayData, rng.Next());
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
                            Sheep sheep = new Sheep(x, y, mapData, overlayData, rng.Next());
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
        if (conf.EnableAnimalMovement) 
        {
            UpdateCrabs();
            UpdateTurtles();
            UpdateCows();
            UpdateSheeps();
        }
    }
}