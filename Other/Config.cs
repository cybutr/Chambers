using System;
using System.Collections.Generic;
using Internal;
public class Config
{
    // Map Configuration
    public int Width { get; set; }
    public int Height { get; set; }
    public double NoiseScale { get; set; }
    public double ErosionFactor { get; set; }
    public int MinBiomeSize { get; set; }
    public int MinLakeSize { get; set; }
    public int MinRiverWidth { get; set; }
    public int MaxRiverWidth { get; set; }
    public int MinMountainWidth {get; set; } = 3;
    public int MaxMountainWidth {get; set; } = 6;
    public double RiverFlowChance { get; set; }
    public double PlainsHeightThreshold { get; set; }
    public double ForestHeightThreshold { get; set; }
    public double MountainHeightThreshold { get; set; }
    public bool EnableRivers { get; set; }
    public bool EnableLakes { get; set; }
    public bool EnableMountainRanges { get; set; }
    public bool EnableTempatureBiomeChanges { get; set; }
    public bool EnableHumidityBiomeChanges { get; set; }
    public int BiomeBlend { get; set; }

    // Gamerules
    public bool EnableWildfires { get; set; }
    public bool EnableSecrets { get; set; }
    public bool DoTimeCycle { get; set; }
    public bool DoWeatherCycle { get; set; }

    // Structures
    public bool GenerateStructures { get; set; }
    public bool EnableVillages { get; set; }
    public bool EnableCities { get; set; }
    public bool EnableDungeons { get; set; }
    
    // Animals
    public bool GenerateAnimals { get; set; }
    public bool EnableAnimalMovement { get; set; }
    public bool EnableAnimalBreeding { get; set; }
    public bool EnableAnimalDeath { get; set; }
    public bool EnableAnimalExtinction { get; set; }
    public bool EnableAnimalMigration { get; set; }
    public bool EnableAnimalHunting { get; set; }
    public bool EnableAnimalDomestication { get; set; }

    // Disasters
    public bool EnableTornadoes { get; set; }
    public bool EnableEarthquakes { get; set; }
    public bool EnableVolcanoes { get; set; }
    public bool EnableFloods { get; set; }
    public bool EnableMeteors { get; set; }

    // Events
    public bool EnableRobberies { get; set; }
    public bool EnableKidnappings { get; set; }
    public bool EnableMurders { get; set; }
    public bool EnableRiots { get; set; }
    public bool EnablePlagues { get; set; }
    public bool EnableInvasions { get; set; }
    public bool EnableWars { get; set; }

    // Economy
    public bool EnableTrade { get; set; }
    public bool EnableBartering { get; set; }
    public bool EnableCurrency { get; set; }
    public bool EnableTaxes { get; set; }
    public bool EnableBanks { get; set; }

    // Visuals
    public bool DisplayShadows { get; set; }
    public bool DisplayWaves { get; set; }
    public int NumberOfWaves { get; set; }

    public int Seed { get; set; }

    public Config(int mapWidth, int mapHeight, double noiseScale, double erosionFactor, int minBiomeSize, int minLakeSize, int minRiverWidth, int maxRiverWidth, double riverFlowChance, double plainsHeightThreshold, double forestHeightThreshold, double mountainHeightThreshold, string seed)
    {
        Width = mapWidth;
        Height = mapHeight;
        NoiseScale = noiseScale;
        ErosionFactor = erosionFactor;
        MinBiomeSize = minBiomeSize;
        MinLakeSize = minLakeSize;
        MinRiverWidth = minRiverWidth;
        MaxRiverWidth = maxRiverWidth;
        RiverFlowChance = riverFlowChance;
        PlainsHeightThreshold = plainsHeightThreshold;
        ForestHeightThreshold = forestHeightThreshold;
        MountainHeightThreshold = mountainHeightThreshold;
        Seed = Program.ConvertStringToNumbers(seed);
    }
}
public class GUIConfig
{
    // Padding properties
    public static int LeftPadding { get; set; } = 0;
    public static int TopPadding { get; set; } = 11;
    public static int RightPadding { get; set; } = 0;
    public static int BottomPadding { get; set; } = 5;
    public int MinConsoleWidth { get; set; } = 100;
    public int MinConsoleHeight { get; set; } = 50;

    public int RadarWidth { get; set; }
    public int RadarHeight { get; set; }
    public string Title { get; set; }
    public int TitleWidth { get; set; }
    public int TitleHeight { get; set; }
    public int StatsWidth { get; set; }
    public int StatsHeight { get; set; }
    public int TimeWidth { get; set; }
    public int TimeHeight { get; set; }
    public int HelpWidth { get; set; }
    public int HelpHeight { get; set; }
    public int TileWidth { get; set; }
    public int TileHeight { get; set; }
    public int ThanksWidth { get; set; }
    public int ThanksHeight { get; set; }
    public int OutputWidth { get; set; }
    public int OutputHeight { get; set; }
    public GUIConfig(int consoleWidth, int consoleHeight)
    {

        // Weather Radar
        RadarWidth = TopPadding * 2;
        RadarHeight = TopPadding;

        // Title And Signature
        Title = "Chambers";
        TitleWidth = 50;
        TitleHeight = TopPadding - 2;

        // Weather Stats
        StatsWidth = ((consoleWidth / 2 - RadarWidth - TitleWidth / 2) / 2) + 1;
        StatsHeight = TopPadding - 2;

        // Time Info
        if (consoleWidth % 2 == 0)
        {
            TimeWidth = StatsWidth + 2;
        }
        else
        {
            TimeWidth = StatsWidth + 1;
        }
        TimeHeight = TopPadding - 2;

        // Help Menu
        HelpWidth = RightPadding * 2;
        HelpHeight = (consoleHeight - TopPadding - BottomPadding) / 3 * 2 + 4;

        // Tile Info
        TileWidth = RightPadding * 2;
        TileHeight = (consoleHeight - TopPadding - BottomPadding) / 3 - 5;

        // Thanks Info
        if (consoleWidth > 100)
        {
            ThanksWidth = RightPadding * 2;
            ThanksHeight = TopPadding;
        }
        else
        {
            ThanksWidth = 0;
            ThanksHeight = 0;
        }

        // Output Log
        if (consoleWidth > 100)
        {
            OutputWidth = (consoleWidth) / 2 - (TitleWidth / 2) - ThanksWidth + 1;
        }
        else
        {
            OutputWidth = 0;
        }
        OutputHeight = TopPadding + 1;
    }
}
