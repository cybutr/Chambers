using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
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
    public TileId[,] mapData { get; set; }
    public EntityId[,] overlayData { get; set; }
    public CloudType[,] cloudData { get; set; }
    [JsonIgnore] public EntityId[,] previousOverlayData { get; set; }
    [JsonIgnore] public TileId[,] previousMapData { get; set; }
    [JsonIgnore] public CloudType[,] previousCloudData { get; set; }
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
        
        var _r = new Random();
        conf = new Config(safeWidth, safeHeight, 10.0, Math.Round(_r.Next() * ((_r.NextDouble() - 0.5) * 2)).ToString());
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
        mapData = new TileId[width, height];
        previousMapData = new TileId[width, height];
        overlayData = new EntityId[width, height];
        previousOverlayData = new EntityId[width, height];
        cloudData = new CloudType[cloudDataWidth, cloudDataHeight];
        previousCloudData = new CloudType[cloudDataWidth, cloudDataHeight];
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
        ReplaceBiome(TileId.Mountain, TileId.Plains);
        if (conf.EnableMountainRanges) CreateMountains();
        if (conf.EnableRivers) CreateRiver();
        if (conf.EnableLakes) CreateLakes();
        CreateComplexFrame();
        SingleTileCheckPF(TileId.Plains, TileId.Forest, 2);
        CreateBeaches();
        //CreateStreams();
        WaterDepth();
        FrameMap(TileId.Border);
        RemoveSeperatedOceanTiles();
        if (conf.GenerateAnimals)
        {
            InitializeSpecies(3, 6, new Crab(0, 0, seed));
            InitializeSpecies(2, 4, new Turtle(0, 0, seed));
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
        ReplaceBiome(TileId.Mountain, TileId.Plains);
        GUI.WriteLine(6);
        if (conf.EnableMountainRanges) CreateMountains();
        GUI.WriteLine(7);
        if (conf.EnableRivers) CreateRiver();
        GUI.WriteLine(8);
        if (conf.EnableLakes) CreateLakes();
        GUI.WriteLine(9);
        CreateComplexFrame();
        GUI.WriteLine(10);
        SingleTileCheckPF(TileId.Plains, TileId.Forest, 2);
        GUI.WriteLine(11);
        CreateBeaches();
        GUI.WriteLine(12);
        //CreateStreams();
        WaterDepth();
        GUI.WriteLine(13);
        FrameMap(TileId.Border);
        GUI.WriteLine(14);
        RemoveSeperatedOceanTiles();
        if (conf.GenerateAnimals)
        {
            InitializeSpecies(3, 6, new Crab(0, 0, seed));
            GUI.WriteLine(15);
            InitializeSpecies(2, 4, new Turtle(0, 0, seed));
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
        FrameMap(TileId.Border);
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
                mapData[x, y] = TileId.Plains;
                overlayData[x, y] = EntityId.None; // No overlay
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
                previousMapData[x, y] = TileId.Plains;
                previousOverlayData[x, y] = EntityId.None;
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
                    mapData[x, y] = TileId.Forest;
                }
                else if (noise[x, y] < conf.PlainsHeightThreshold)
                {
                    mapData[x, y] = TileId.Plains;
                }
                else if (noise[x, y] < conf.MountainHeightThreshold)
                {
                    mapData[x, y] = TileId.Mountain;
                }
                else
                {
                    mapData[x, y] = TileId.Empty;
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
                
                TileId biomeType = mapData[x, y];
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
    public void CheckAndReplaceBiomes(TileId selectedBiome, int minSameBiome)
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
                        TileId mostSurroundedBiome = GetMostSurroundedBiome(x, y);
                        mapData[x, y] = mostSurroundedBiome;
                    }
                }
            }
        }
    }
    private TileId GetMostSurroundedBiome(int x, int y)
    {
        Dictionary<TileId, int> biomeCounts = new Dictionary<TileId, int>();
        foreach ((int nx, int ny) in GetNeighbors(x, y))
        {
            if (nx >= 0 && nx < width && ny >= 0 && ny < height)
            {
                TileId neighborBiome = mapData[nx, ny];
                if (biomeCounts.ContainsKey(neighborBiome))
                    biomeCounts[neighborBiome]++;
                else
                    biomeCounts[neighborBiome] = 1;
            }
        }

        return biomeCounts.OrderByDescending(b => b.Value).First().Key;
    }
    public void SingleTileCheckPF(TileId selectedBiome1, TileId selectedBiome2, int minSameBiome)
    {
        CheckAndReplaceBiomes(selectedBiome1, minSameBiome);
        CheckAndReplaceBiomes(selectedBiome2, minSameBiome);
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