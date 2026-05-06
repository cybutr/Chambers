using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
using Internal;
using static Internal.GUI;
public partial class Map
{
    private static bool isLinux { get; set; } = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    public static List<string> outputBuffer { get; set; } = [];
    public List<string> actualOutputBuffer { get; set; } = [];
    public static List<string> eventBuffer { get; set; } = [];
    public int cloudShadowOffsetX { get; set; }
    public int cloudShadowOffsetY { get; set; }            
    public DayNightCycle dayNight { get; set; } = new();
    public int DayCount { get => dayNight.DayCount; set => dayNight.DayCount = value; }
    public double time { get => dayNight.TimeOfDay; set => dayNight.TimeOfDay = value; }
    public GradientDirection CurrentGradientDirection { get => dayNight.CurrentGradientDirection; set => dayNight.CurrentGradientDirection = value; }
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
    [JsonIgnore] public int[,] darknessData { get; set; }
    [JsonIgnore] public double[,] shadowData { get; set; }
    [JsonIgnore] public double[,] waveIntensityData { get; set; }
    public void ReinitializeTransientData()
    {
        darknessData      = new int[width, height];
        shadowData        = new double[width, height];
        waveIntensityData = new double[width, height];
        _cloudDataSwap    = new CloudType[cloudDataWidth, cloudDataHeight];
        _cloudDepthSwap   = new int[cloudDataWidth, cloudDataHeight];
    }
    [JsonIgnore] private Framebuffer _fb { get; set; } = new();
    [JsonIgnore] public GuiBuffer _guiBuf { get; set; } = new();
    [JsonIgnore] public ulong tick { get; set; }
    [JsonIgnore] private double _cloudAccumX { get; set; }
    [JsonIgnore] private double _cloudAccumY { get; set; }
    [JsonIgnore] private CloudType[,] _cloudDataSwap { get; set; }
    [JsonIgnore] private int[,] _cloudDepthSwap { get; set; }
    [JsonIgnore] private int[,] _radarCache { get; set; } = new int[0, 0];
    [JsonIgnore] private ulong _radarCacheTick { get; set; }
    public Camera? camera { get; set; }
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
    private int numberOfWaves { get; set; }
    public int SavedConsoleWidth { get; set; }
    public int SavedConsoleHeight { get; set; }

    public Map()
    {
        try
        {
            SavedConsoleWidth  = Console.WindowWidth;
            SavedConsoleHeight = Console.WindowHeight;
        }
        catch
        {
            // Fallback to reasonable defaults if console is not available
            SavedConsoleWidth  = 80;
            SavedConsoleHeight = 25;
        }

        Random r = new();
        conf = new Config(200, 100, 10.0, Math.Round(r.Next() * ((r.NextDouble() - 0.5) * 2)).ToString());
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
        overlayData = new EntityId[width, height];
        cloudData = new CloudType[cloudDataWidth, cloudDataHeight];
        cloudDepthData = new int[cloudDataWidth, cloudDataHeight];
        _cloudDataSwap = new CloudType[cloudDataWidth, cloudDataHeight];
        _cloudDepthSwap = new int[cloudDataWidth, cloudDataHeight];
        darknessData = new int[width, height];
        shadowData = new double[width, height];
        waveIntensityData = new double[width, height];
        precipitationData = new double[cloudDataWidth, cloudDataHeight];
        previousPrecipitationData = new double[cloudDataWidth, cloudDataHeight];
        temperatureData = new int[width, height];
        humidityData = new int[width, height];
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

        if (conf.GenType == 2)
        {
            GenerationContext ctx = new()
            {
                width           = width,
                height          = height,
                mapData         = new TileId[width, height],
                elevation       = new double[width, height],
                temperatureData = new int[width, height],
                humidityData    = new int[width, height],
                noise           = new double[width, height],
                tempatureNoise  = new double[width, height],
                humidityNoise   = new double[width, height],
                rng             = rng,
                conf            = conf,
            };

            GenerationPipeline pipeline = new();
            pipeline.AddPass(new ElevationPass());
            pipeline.AddPass(new ClimatePass());
            pipeline.AddPass(new BiomePass());
            pipeline.AddPass(new MountainPass());
            pipeline.AddPass(new HydroPass());
            pipeline.AddPass(new FeaturePass());
            pipeline.AddPass(new FramePass());
            pipeline.AddPass(new EntityPass());
            pipeline.Run(ctx);

            mapData         = ctx.mapData;
            temperatureData = ctx.temperatureData;
            humidityData    = ctx.humidityData;

            double tempSum = 0, humSum = 0;
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    tempSum += temperatureData[x, y];
                    humSum  += humidityData[x, y];
                }
            double tileCount = width * height;
            avarageTempature = (tempSum / tileCount - 1.0) / 4.0;
            avarageHumidity  = (humSum  / tileCount - 1.0) / 4.0;
        }
        else
        {
            noise          = Perlin.GeneratePerlinNoise(width, height, conf.NoiseScale, rng.Next());
            tempatureNoise = Perlin.GeneratePerlinNoise(width, height, conf.NoiseScale * 25, rng.Next());
            humidityNoise  = Perlin.GeneratePerlinNoise(width, height, conf.NoiseScale * 12, rng.Next());
            if (debug) HandleDebugGen();
            else HandleGen();
        }

        InitializeCamera();
        InitializeFramebuffer();
        EventBus.Emit(new MapGeneratedEvent(seed, width, height));
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
            InitializeAllEntities();
        if (conf.DisplayWaves) InitializeWaves();
        if (conf.DoWeatherCycle) InitializeWeather();
        if (conf.DoWeatherCycle) InitializeClouds();
    }
    public void HandleDebugGen()
    {
        SetAvarageTempatureHumidity();
        WriteLine(1);
        AssignTempAndHumData();
        WriteLine(2);
        SmoothOutTempatureHumidity();
        WriteLine(3);
        AssignBiomes(noise);
        WriteLine(4);
        EnsureMinimumBiomeSize();
        WriteLine(5);
        ReplaceBiome(TileId.Mountain, TileId.Plains);
        WriteLine(6);
        if (conf.EnableMountainRanges) CreateMountains();
        WriteLine(7);
        if (conf.EnableRivers) CreateRiver();
        WriteLine(8);
        if (conf.EnableLakes) CreateLakes();
        WriteLine(9);
        CreateComplexFrame();
        WriteLine(10);
        SingleTileCheckPF(TileId.Plains, TileId.Forest, 2);
        WriteLine(11);
        CreateBeaches();
        WriteLine(12);
        //CreateStreams();
        WaterDepth();
        WriteLine(13);
        FrameMap(TileId.Border);
        WriteLine(14);
        RemoveSeperatedOceanTiles();
        if (conf.GenerateAnimals)
        {
            InitializeAllEntities();
            WriteLine(15);
        }
        if (conf.DisplayWaves) InitializeWaves();
        WriteLine(19);
        if (conf.DoWeatherCycle) InitializeWeather();
        WriteLine(20);
        if (conf.DoWeatherCycle) InitializeClouds();
    }
    public void HandleTestGen()
    {
        seed = new Random().Next();
        rng = new Random(seed);
        noise = Perlin.GeneratePerlinNoise(width, height, conf.NoiseScale, rng.Next());
        //tempatureNoise = Perlin.GeneratePerlinNoise(width, height, conf.NoiseScale * 25, rng.Next());
        //humidityNoise = Perlin.GeneratePerlinNoise(width, height, conf.NoiseScale * 12, rng.Next());
        avarageHumidity = 0.5f;
        avarageTempature = 0.5f;
        if (debug) WriteLine("1");
        //GeneratePlainsOnly();
        if (debug) WriteLine("2");
        AssignBiomes(noise);
        ReplaceBiome(TileId.Mountain, TileId.Plains);
        EnsureMinimumBiomeSize();
        SingleTileCheckPF(TileId.Forest, TileId.Plains, 3);
        CreateMountains();
        if (debug) WriteLine("3");
        CreateLakes();
        if (debug) WriteLine("4");
        WaterDepth();
        if (debug) WriteLine("5");
        //FillCircle(GetMapCenter().Item1, GetMapCenter().Item2, 'M', 16, 20);
        FrameMap(TileId.Border);
        if (debug) WriteLine("6");
        CreateStreams();
        if (debug) WriteLine("7");
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

                // Neutral noise values
                noise[x, y] = 0.5;
                tempatureNoise[x, y] = 0.5;
                humidityNoise[x, y] = 0.5;
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
                if (noise[x, y] < conf.ForestHeightThreshold) mapData[x, y] = TileId.Forest;
                else if (noise[x, y] < conf.PlainsHeightThreshold) mapData[x, y] = TileId.Plains;
                else if (noise[x, y] < conf.MountainHeightThreshold) mapData[x, y] = TileId.Mountain;
                else mapData[x, y] = TileId.Empty;
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
                List<(int x, int y)> biomeRegion = [];

                // Find all connected tiles of this biome type
                FloodFillRegion(x, y, biomeType, visited, biomeRegion);

                // If region is too small, expand it
                if (biomeRegion.Count < minSize) ExpandSmallBiome(biomeRegion, biomeType);
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
        Dictionary<TileId, int> biomeCounts = new();
        foreach ((int nx, int ny) in GetNeighbors(x, y))
        {
            if (nx >= 0 && nx < width && ny >= 0 && ny < height)
            {
                TileId neighborBiome = mapData[nx, ny];
                if (biomeCounts.ContainsKey(neighborBiome)) biomeCounts[neighborBiome]++;
                else biomeCounts[neighborBiome] = 1;
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
        dayNight.UpdateGradientDirection();
        if (conf.EnableAnimalMovement)
            _species.UpdateAll(mapData, overlayData, BuildSimContext(), IsTileUnderCloud);
    }

    private SimContext BuildSimContext() => new()
    {
        IsNight             = dayNight.TimeOfDay < dayNight.SunriseTime || dayNight.TimeOfDay > dayNight.SunsetTime,
        Time                = dayNight.TimeOfDay,
        SunriseTime         = dayNight.SunriseTime,
        SunsetTime          = dayNight.SunsetTime,
        Season              = (int)dayNight.Season,
        FearMap             = _species.FearMap,
        GetSpeciesAt        = _species.GetAt,
        EnablePredators     = conf.EnablePredators,
        EnableAnimalHunting = conf.EnableAnimalHunting,
        EnableAnimalDeath   = conf.EnableAnimalDeath,
        EnableAnimalBreeding = conf.EnableAnimalBreeding,
    };
}