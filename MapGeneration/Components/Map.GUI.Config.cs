using System;
using System.Collections.Generic;
using System.Linq;
using Internal;
using static Internal.GUI;

public partial class Map
{
    #region map config GUI
    public bool GetConfig()
    {
        DisplayMapConfig();
        SetCursorPosition(TerminalCentre.x - SaveWidth / 2 + SaveWidth / 2 - 2 , TerminalCentre.y + GamerulesHeight + StructuresHeight - MapConfigHeight + HeightOffset + BottomHeight + 1);
        Write(
            SetBackgroundColor(SelectColor.r, SelectColor.g, SelectColor.b) +
            SetForegroundColor(ColorSpectrum.BLACK.r, ColorSpectrum.BLACK.g, ColorSpectrum.BLACK.b) +
            $"SAVE{ResetColor()}"
        );
        ManageParamNavigation();
        numberOfWaves = conf.NumberOfWaves;
        Clear();
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
    public class ParamCoordinate(string propertyName, SettingType settingType, ParamType type, int x, int y)
    {
        public string PropertyName { get; set; } = propertyName;
        public SettingType SettingType { get; set; } = settingType;
        public ParamType ParamType { get; set; } = type;
        public int X { get; set; } = x;
        public int Y { get; set; } = y;
    }
    public static List<ParamCoordinate> MapConfigParams {get; set;} =
    [
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
    ];
    public static List<ParamCoordinate> GameruleParams {get; set;} =
    [
        new ParamCoordinate("EnableWildfires", SettingType.Gamerules, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableSecrets", SettingType.Gamerules, ParamType.Bool, 0, 0),
        new ParamCoordinate("DoTimeCycle", SettingType.Gamerules, ParamType.Bool, 0, 0),
        new ParamCoordinate("DoWeatherCycle", SettingType.Gamerules, ParamType.Bool, 0, 0)
    ];
    public static List<ParamCoordinate> StructureParams {get; set;} =
    [
        new ParamCoordinate("GenerateStructrs", SettingType.Structures, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableVillages", SettingType.Structures, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableCities", SettingType.Structures, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableDungeons", SettingType.Structures, ParamType.Bool, 0, 0)
    ];
    public static List<ParamCoordinate> EconomyParams {get; set;} =
    [
        new ParamCoordinate("EnableTrades", SettingType.Economy, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableTrades", SettingType.Economy, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableCurrency", SettingType.Economy, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableTaxes", SettingType.Economy, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableBanks", SettingType.Economy, ParamType.Bool, 0, 0)
    ];
    public static List<ParamCoordinate> AnimalParams {get; set;} =
    [
        new ParamCoordinate("GenerateAnimals", SettingType.Animals, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnablePredators", SettingType.Animals, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableAnimalMovement", SettingType.Animals, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableAnimalBreeding", SettingType.Animals, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableAnimalDeath", SettingType.Animals, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableAnimalExtinction", SettingType.Animals, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableAnimalMigration", SettingType.Animals, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableAnimalHunting", SettingType.Animals, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableAnimalDomestication", SettingType.Animals, ParamType.Bool, 0, 0)
    ];
    public static List<ParamCoordinate> DisasterParams {get; set;} =
    [
        new ParamCoordinate("EnableTornadoes", SettingType.Disasters, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableEarthquakes", SettingType.Disasters, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableVolcanoes", SettingType.Disasters, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableFloods", SettingType.Disasters, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableMeteors", SettingType.Disasters, ParamType.Bool, 0, 0)
    ];
    public static List<ParamCoordinate> EventParams {get; set;} =
    [
        new ParamCoordinate("EnableRobberies", SettingType.Events, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableMurders", SettingType.Events, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableRiots", SettingType.Events, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnablePlagues", SettingType.Events, ParamType.Bool, 0, 0),
        new ParamCoordinate("EnableWars", SettingType.Events, ParamType.Bool, 0, 0)
    ];
    public static List<ParamCoordinate> VisualParams {get; set;} =
    [
        new ParamCoordinate("DisplayShadows", SettingType.Visuals, ParamType.Bool, 0, 0),
        new ParamCoordinate("DisplayWaves", SettingType.Visuals, ParamType.Bool, 0, 0),
        new ParamCoordinate("NumberOfWaves", SettingType.Visuals, ParamType.Int, 0, 0),
        new ParamCoordinate("CloudMorphInterval", SettingType.Visuals, ParamType.Int, 0, 0)
    ];
    public static List<ParamCoordinate> NullParams {get; set;} = [];
    public static List<ParamCoordinate>[,] AllParams {get; set;} = new List<ParamCoordinate>[4, 3]
    {
        { GameruleParams, MapConfigParams, NullParams },
        { StructureParams, EventParams, NullParams },
        { EconomyParams, AnimalParams, DisasterParams },
        { NullParams, NullParams, VisualParams }
    };
    #endregion
    public static string Title {get; set;} = @"
   __  _ __  _   _   __ ___   ___  ___    ___
 ,'_/ /// /.' \ / \,' // o.) / _/ / o | ,' _/
/ /_ / ` // o // \,' // o \ / _/ /  ,' _\ `. 
|__//_n_//_n_//_/ /_//___,'/___//_/`_\/___,' 
";
    private int GetConfWindowWidth(List<ParamCoordinate> paramList)
    {
        return paramList switch
        {
            List<ParamCoordinate> _ when paramList == MapConfigParams => ConfigWidth - MapConfigOffset,
            List<ParamCoordinate> _ when paramList == GameruleParams => MapConfigOffset - 1,
            List<ParamCoordinate> _ when paramList == StructureParams => MapConfigOffset - 1,
            List<ParamCoordinate> _ when paramList == EconomyParams => MapConfigOffset - 1,
            List<ParamCoordinate> _ when paramList == AnimalParams => (ConfigWidth - MapConfigOffset) / 2 - 1,
            List<ParamCoordinate> _ when paramList == DisasterParams => (ConfigWidth - MapConfigOffset) / 2,
            List<ParamCoordinate> _ when paramList == VisualParams => (ConfigWidth - MapConfigOffset) / 2,
            List<ParamCoordinate> _ when paramList == EventParams => ConfigWidth - MapConfigOffset,
            _ => 20,
        };
    }
    public static int CurrentParamX {get; set;}
    public static int CurrentParamY {get; set;}
    public static int ConfigWidth {get; set;} = 95;
    public static int ConfigHeight {get; set;} = 62;
    public static int MapConfigOffset {get; set;} = 25;
    public static int MapConfigHeight {get; set;} = ConfigHeight / 2 - 10;
    public static int GamerulesHeight {get; set;} = ConfigHeight / 2 - 20;
    public static int StructuresHeight {get; set;} = 15;
    public static int BottomHeight {get; set;} = ConfigHeight / 2 - 16;
    public static int SaveWidth {get; set;} = 10;
    public static int HeightOffset {get; set;} = Math.Max(0, (Console.WindowHeight - (10 + GamerulesHeight + StructuresHeight + BottomHeight)) / 6);
    public (int r, int g, int b) SelectColor {get; set;} = ColorSpectrum.SILVER;
    public static (int x, int y) TerminalCentre {get; set;} = (Console.WindowWidth / 2, Console.WindowHeight / 2);
    public void CalculateParamCoordinates()
    {
        int centerX = Console.WindowWidth / 2;
        int startY =  TerminalCentre.y - ConfigHeight / 2 + HeightOffset;
    
        // Calculate the starting X position based on configWidth to center the window
        int startX = centerX - (ConfigWidth / 2);
    
        startY += 11;
        // Assign coordinates for Gamerules
        foreach (var param in GameruleParams)
        {
            param.X = startX + 2;
            param.Y = startY++;
        }
    
        startY += GamerulesHeight - GameruleParams.Count(); // Add spacing between sections
    
        // Assign coordinates for Structures
        foreach (var param in StructureParams)
        {
            param.X = startX + 2;
            param.Y = startY++;
        }
    
        startY -= StructureParams.Count() + 11; // Add spacing between sections
    
        // Assign coordinates for Map Config
        foreach (var param in MapConfigParams)
        {
            param.X = startX + MapConfigOffset + 2;
            param.Y = startY++;
        }
    
        startY += StructuresHeight - StructureParams.Count() * 2; // Add spacing between sections
    
        // Assign coordinates for Economy
        foreach (var param in EconomyParams)
        {
            param.X = startX + 2;
            param.Y = startY++;
        }
    
        startY -= EconomyParams.Count(); // Add spacing between sections
    
        // Assign coordinates for Animals
        foreach (var param in AnimalParams)
        {
            param.X = startX + MapConfigOffset + 2;
            param.Y = startY++;
        }
    
        startY -= AnimalParams.Count(); // Add spacing between sections
    
        // Assign coordinates for Disasters
        foreach (var param in DisasterParams)
        {
            param.X = startX + MapConfigOffset + ((ConfigWidth - MapConfigOffset) / 2) + 2;
            param.Y = startY++;
        }
    
        startY += 2; // Add spacing between sections
    
        // Assign coordinates for Visuals
        foreach (var param in VisualParams)
        {
            param.X = startX + MapConfigOffset + ((ConfigWidth - MapConfigOffset) / 2) + 2;
            param.Y = startY++;
        }
    
        startY -= AnimalParams.Count() * 2 - 2; // Add spacing between sections
    
        // Assign coordinates for Events
        for (int i = 0; i < EventParams.Count(); i++)
        {
            if (i < GamerulesHeight + StructuresHeight - MapConfigHeight - 2)
            {
                EventParams[i].X = startX + MapConfigOffset + 2;
                EventParams[i].Y = startY++;
            }
            else
            {
                EventParams[i].X = startX + ConfigWidth - EventParams[i].PropertyName.Length - 6;
                EventParams[i].Y = startY++ - GamerulesHeight - StructuresHeight + MapConfigHeight + 2;
            }
        }
    
        // Ensure all parameters have valid coordinates
        foreach (var list in new List<List<ParamCoordinate>> 
        { 
            MapConfigParams, 
            GameruleParams, 
            StructureParams, 
            EconomyParams, 
            AnimalParams, 
            DisasterParams, 
            VisualParams, 
            EventParams 
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
        foreach (var param in MapConfigParams)
        {
            SetCursorPosition(param.X, param.Y);
            Write($"{param.PropertyName} - {GetParamValue(param)}");
        }
        foreach (var param in GameruleParams)
        {
            SetCursorPosition(param.X, param.Y);
            Write($"{param.PropertyName} - {GetParamValue(param)}");
        }
        foreach (var param in StructureParams)
        {
            SetCursorPosition(param.X, param.Y);
            Write($"{param.PropertyName} - {GetParamValue(param)}");
        }
        foreach (var param in EconomyParams)
        {
            SetCursorPosition(param.X, param.Y);
            Write($"{param.PropertyName} - {GetParamValue(param)}");
        }
        foreach (var param in AnimalParams)
        {
            SetCursorPosition(param.X, param.Y);
            Write($"{param.PropertyName} - {GetParamValue(param)}");
        }
        foreach (var param in DisasterParams)
        {
            SetCursorPosition(param.X, param.Y);
            Write($"{param.PropertyName} - {GetParamValue(param)}");
        }
        foreach (var param in VisualParams)
        {
            SetCursorPosition(param.X, param.Y);
            Write($"{param.PropertyName} - {GetParamValue(param)}");
        }
        foreach (var param in EventParams)
        {
            SetCursorPosition(param.X, param.Y);
            Write($"{param.PropertyName} - {GetParamValue(param)}");
        }
    }
    public string GetParamValue(ParamCoordinate param)
    {
        return param.ParamType switch
        {
            ParamType.Bool => GetParamBool(param),
            ParamType.Int => GetParamInt(param).ToString(),
            ParamType.Double => GetParamDouble(param).ToString(),
            ParamType.String => GetParamString(param),
            _ => "",
        };
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
            SetCursorPosition(param.X, param.Y);
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
            if (tempStringValue != null && (param.ParamType == ParamType.String || param.ParamType == ParamType.Int || param.ParamType == ParamType.Double)) value = tempStringValue;

            SetCursorPosition(param.X, param.Y);
            if (isSelected)
            {
                if (param.ParamType != ParamType.Bool)
                {
                    Write(
                        SetBackgroundColor(SelectColor.r, SelectColor.g, SelectColor.b) +
                        SetForegroundColor(ColorSpectrum.BLACK.r, ColorSpectrum.BLACK.g, ColorSpectrum.BLACK.b) +
                        $"{param.PropertyName} - {value}{ResetColor()}{new string(' ', Math.Max(redrawDistance, 0))}"
                    );
                }
                else
                {
                    Write(
                        SetBackgroundColor(SelectColor.r, SelectColor.g, SelectColor.b) +
                        SetForegroundColor(ColorSpectrum.BLACK.r, ColorSpectrum.BLACK.g, ColorSpectrum.BLACK.b) +
                        $"{param.PropertyName} - {value}{ResetColor()}"
                    );
                }
            }
            else
            {
                if (param.ParamType != ParamType.Bool)
                    Write($"{param.PropertyName} - {value}{new string(' ', redrawDistance)}");
                else
                    Write($"{param.PropertyName} - {value}");
            }
        }

        CurrentParamX = 2;
        CurrentParamY = 1;
        List<ParamCoordinate> currentList = AllParams[CurrentParamX, CurrentParamY];
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
                int oldX = CurrentParamX;
                int oldY = CurrentParamY;
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
                        if (CurrentParamX == 1 && CurrentParamY == 1 && currentList.Count > 1)
                        {
                            int halfCount = (int)Math.Ceiling(currentList.Count / 2.0);
                            bool isLeftSide = listParamIndex < halfCount;
                            if (isLeftSide)
                            {
                                // If not at top, move up; else go x-1
                                if (listParamIndex > 0) listParamIndex--;
                                else
                                {
                                    CurrentParamX--;
                                    currentList = AllParams[CurrentParamX, CurrentParamY];
                                    listParamIndex = currentList.Count - 1;
                                }
                            }
                            else
                            {
                                // If not at top of right, move up; else go x-1
                                if (listParamIndex > halfCount) listParamIndex--;
                                else
                                {
                                    CurrentParamX--;
                                    currentList = AllParams[CurrentParamX, CurrentParamY];
                                    listParamIndex = currentList.Count - 1;
                                }
                            }
                        }
                        else
                        {
                            if (listParamIndex > 0 && !isSave) listParamIndex--;
                            else if (CurrentParamX == 2 && CurrentParamY == 1 & !isSave)
                            {
                                CurrentParamX--;
                                currentList = EventParams;
                                listParamIndex = (int)Math.Ceiling(currentList.Count / 2.0) - 1;
                            }
                            else if (CurrentParamX == 2 && CurrentParamY == 2 && !isSave)
                            {
                                CurrentParamX--;
                                CurrentParamY--;
                                currentList = EventParams;
                                listParamIndex = currentList.Count - 1;
                            }
                            else if (!isSave)
                            {
                                if (CurrentParamX > 0
                                    && AllParams[CurrentParamX - 1, CurrentParamY] != null
                                    && AllParams[CurrentParamX - 1, CurrentParamY].Count > 0)
                                {
                                    CurrentParamX--;
                                    currentList = AllParams[CurrentParamX, CurrentParamY];
                                    listParamIndex = currentList.Count - 1;
                                }
                            }
                            else isSave = false;
                        }
                        break;

                    case ConsoleKey.DownArrow:
                    case ConsoleKey.S:
                        // Special 2-column logic for x=1, y=1
                        if (CurrentParamX == 1 && CurrentParamY == 1 && currentList.Count > 1)
                        {
                            int halfCount = (int)Math.Ceiling(currentList.Count / 2.0);
                            bool isLeftSide = listParamIndex < halfCount;
                            if (isLeftSide)
                            {
                                // Go down if not bottom; else x + 1
                                if (listParamIndex < halfCount - 1) listParamIndex++;
                                else
                                {
                                    CurrentParamX++;
                                    currentList = AllParams[CurrentParamX, CurrentParamY];
                                    listParamIndex = 0;
                                }
                            }
                            else
                            {
                                // Go down if not bottom; else x+1
                                if (listParamIndex < currentList.Count - 1) listParamIndex++;
                                else
                                {
                                    CurrentParamX++;
                                    CurrentParamY++;
                                    currentList = AllParams[CurrentParamX, CurrentParamY];
                                    listParamIndex = 0;
                                }
                            }
                        }
                        else
                        {
                            if (listParamIndex == currentList.Count - 1 && CurrentParamY == 1 && CurrentParamX == 2 && !isSave)
                            {
                                isSave = true;
                                DrawParam(currentList[listParamIndex], redrawDistance, false);
                            }
                            else if (listParamIndex < currentList.Count - 1 && listParamIndex >= 0 && !isSave) listParamIndex++;
                            else if (!isSave)
                            {
                                if (CurrentParamX < AllParams.GetLength(0) - 1
                                    && AllParams[CurrentParamX + 1, CurrentParamY] != null
                                    && AllParams[CurrentParamX + 1, CurrentParamY].Count > 0)
                                {
                                    CurrentParamX++;
                                    currentList = AllParams[CurrentParamX, CurrentParamY];
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
                        if (CurrentParamX == 1 && CurrentParamY == 1 && currentList.Count > 1)
                        {
                            int halfCount = (int)Math.Ceiling(currentList.Count / 2.0);
                            bool isLeftSide = listParamIndex < halfCount;
                            if (!isLeftSide) listParamIndex = 0;
                            else
                            {
                                // Regular behavior if on left already
                                if (CurrentParamY > 0
                                    && AllParams[CurrentParamX, CurrentParamY - 1] != null
                                    && AllParams[CurrentParamX, CurrentParamY - 1].Count > 0)
                                {
                                    CurrentParamY--;
                                    currentList = AllParams[CurrentParamX, CurrentParamY];
                                    listParamIndex = 0;
                                }
                            }
                        }
                        else
                        {
                            if (CurrentParamY == 2 && CurrentParamX == 3 && !isSave)
                            {
                                CurrentParamY--;
                                CurrentParamX--;
                                currentList = AllParams[CurrentParamX, CurrentParamY];
                                listParamIndex = currentList.Count - 1;
                            }
                            else if (CurrentParamY > 0
                                && AllParams[CurrentParamX, CurrentParamY - 1] != null
                                && AllParams[CurrentParamX, CurrentParamY - 1].Count > 0
                                && !isSave)
                            {
                                CurrentParamY--;
                                currentList = AllParams[CurrentParamX, CurrentParamY];
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
                        if (CurrentParamX == 1 && CurrentParamY == 1 && currentList.Count > 1)
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
                            if (CurrentParamX == 2 && CurrentParamY == 1 && listParamIndex > (int)Math.Ceiling(currentList.Count / 2.0) - 1 && !isSave)
                            {
                                CurrentParamX++;
                                CurrentParamY++;
                                currentList = AllParams[CurrentParamX, CurrentParamY];
                                listParamIndex = 0;
                            }
                            else if (CurrentParamY < AllParams.GetLength(1) - 1
                                && AllParams[CurrentParamX, CurrentParamY + 1] != null
                                && AllParams[CurrentParamX, CurrentParamY + 1].Count > 0
                                && !isSave)
                            {
                                CurrentParamY++;
                                currentList = AllParams[CurrentParamX, CurrentParamY];
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

                if (oldIndex >= 0 && oldParam != null && oldIndex < (AllParams[oldX, oldY] ?? []).Count && !isSave) DrawParam(AllParams[oldX, oldY][oldIndex], oldRedrawDistance, false);
                if (listParamIndex >= 0 && listParamIndex < currentList.Count && !isSave)
                {
                    redrawDistance = GetRedrawDistance(currentList, currentList[listParamIndex]);
                    DrawParam(currentList[listParamIndex], redrawDistance, true);
                }
                else if (isSave)
                {
                    SetCursorPosition(TerminalCentre.x - SaveWidth / 2 + SaveWidth / 2 - 2 , TerminalCentre.y + GamerulesHeight + StructuresHeight - MapConfigHeight + HeightOffset + BottomHeight + 1);
                    Write(
                        SetBackgroundColor(SelectColor.r, SelectColor.g, SelectColor.b) +
                        SetForegroundColor(ColorSpectrum.BLACK.r, ColorSpectrum.BLACK.g, ColorSpectrum.BLACK.b) +
                        $"SAVE{ResetColor()}"
                    );
                }
                if (!isSave)
                {
                    SetCursorPosition(TerminalCentre.x - SaveWidth / 2 + SaveWidth / 2 - 2 , TerminalCentre.y + GamerulesHeight + StructuresHeight - MapConfigHeight + HeightOffset + BottomHeight + 1);
                    Write("SAVE");
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
                                if (int.TryParse(tempStringValue, out int intVal)) parsedValue = intVal;
                                else
                                {
                                    // Invalid input, revert to original value
                                    tempStringValue = GetParamInt(param).ToString();
                                    DrawParam(param, redrawDistance, true);
                                    continue;
                                }
                                break;
                            case ParamType.Double:
                                if (double.TryParse(tempStringValue, out double doubleVal)) parsedValue = Math.Round(doubleVal, 2);
                                else
                                {
                                    // Invalid input, revert to original value
                                    tempStringValue = GetParamDouble(param).ToString("0.##");
                                    DrawParam(param, redrawDistance, true);
                                    continue;
                                }
                                break;
                        }
                        
                        if (parsedValue != null) SetParamValue(param, parsedValue, conf);
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
                            if (param.ParamType == ParamType.Double && key.KeyChar == '.' && tempStringValue.Contains('.')) return;

                            tempStringValue += key.KeyChar;
                            DrawParam(param, redrawDistance, true, tempStringValue);
                        }
                    }
                }
            }
            // SetCursorPosition(0, 0);
            // Write(isSave);
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
            if (param.PropertyName.Contains("Size") || param.PropertyName.Contains("Width")) newVal = Math.Max(1, newVal); // Minimum size of 1
            else if (param.PropertyName == "NumberOfWaves") newVal = Math.Max(0, Math.Min(100, newVal)); // Waves between 0-100
            else if (param.PropertyName == "CloudMorphInterval") newVal = Math.Max(1, Math.Min(50, newVal)); // 1–50 ticks
            
            SetParamValue(param, newVal, conf);
        }
        else if (param.ParamType == ParamType.Double)
        {
            double val = GetParamDouble(param);
            double increment = increase ? 0.1 : -0.1;
            
            // Use different increment for scale values
            if (param.PropertyName.Contains("Scale")) increment = increase ? 1.0 : -1.0;
            
            double newVal = val + increment;
            
            // Add reasonable constraints
            if (param.PropertyName.Contains("Scale")) newVal = Math.Max(0.1, newVal); // Minimum scale
            else if (param.PropertyName.Contains("Factor")) newVal = Math.Max(0, newVal); // Non-negative factors
            
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
                WriteLine($"Error setting property {param.PropertyName}: {ex.Message}");
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
        SetCursorPosition(param.X, param.Y);
        Write(
            $"{SetBackgroundColor(SelectColor.r, SelectColor.g, SelectColor.g)}" +
            $"{SetForegroundColor(ColorSpectrum.BLACK.r, ColorSpectrum.BLACK.g, ColorSpectrum.BLACK.b)}" +
            $"{param.PropertyName} - {value}{ResetColor()}"
        );
    }
    public void DisplayMapConfig()
    {
        HeightOffset = Math.Max(0, (Console.WindowHeight - (10 + GamerulesHeight + StructuresHeight + BottomHeight)) / 6);
        TerminalCentre = (Console.WindowWidth / 2, Console.WindowHeight / 2);
        Clear();
        (int r, int g, int b) tColor = ColorSpectrum.CYAN;
        DrawColoredBox(TerminalCentre.x - ConfigWidth / 2, TerminalCentre.y - ConfigHeight / 2 + HeightOffset, ConfigWidth, 10, "", ColorSpectrum.LIGHT_CYAN); // Title
        DrawColoredBox(TerminalCentre.x - ConfigWidth / 2 + MapConfigOffset, TerminalCentre.y - ConfigHeight / 2 + 10 + HeightOffset, ConfigWidth - MapConfigOffset, MapConfigHeight, "Map Config", ColorSpectrum.BURNT_ORANGE); // Map Config
        DrawColoredBox(TerminalCentre.x - ConfigWidth / 2 + MapConfigOffset, TerminalCentre.y - ConfigHeight / 2 + 10 + ConfigHeight / 2 - 10 + HeightOffset, ConfigWidth - MapConfigOffset, GamerulesHeight + StructuresHeight - MapConfigHeight, "Events", ColorSpectrum.YELLOW); // Events
        DrawColoredBox(TerminalCentre.x - ConfigWidth / 2, TerminalCentre.y - ConfigHeight / 2 + 10 + HeightOffset, MapConfigOffset - 1, GamerulesHeight, "Gamerules", ColorSpectrum.LIGHT_CORAL); // Gamerules
        DrawColoredBox(TerminalCentre.x - ConfigWidth / 2, TerminalCentre.y - ConfigHeight / 2 + 10 + ConfigHeight / 2 - 20 + HeightOffset, MapConfigOffset - 1, StructuresHeight, "Structures", ColorSpectrum.BROWN); // Structures
        DrawColoredBox(TerminalCentre.x - ConfigWidth / 2, TerminalCentre.y - ConfigHeight / 2 + 10 + ConfigHeight / 2 - 20 + StructuresHeight + HeightOffset, MapConfigOffset - 1, BottomHeight, "Economy", ColorSpectrum.GREEN); // Economy
        DrawColoredBox(TerminalCentre.x - ConfigWidth / 2 + MapConfigOffset, TerminalCentre.y + GamerulesHeight + StructuresHeight - MapConfigHeight + HeightOffset, (ConfigWidth - MapConfigOffset) / 2 - 1, BottomHeight, "Animals", ColorSpectrum.PALE_TURQUOISE); // Animals
        DrawColoredBox(TerminalCentre.x - ConfigWidth / 2 + MapConfigOffset + (ConfigWidth - MapConfigOffset) / 2, TerminalCentre.y + GamerulesHeight + StructuresHeight - MapConfigHeight + HeightOffset, (ConfigWidth - MapConfigOffset) / 2, BottomHeight / 2, "Disasters", ColorSpectrum.INDIAN_RED); // Disasters
        DrawColoredBox(TerminalCentre.x - ConfigWidth / 2 + MapConfigOffset + (ConfigWidth - MapConfigOffset) / 2, TerminalCentre.y + GamerulesHeight + StructuresHeight - MapConfigHeight + BottomHeight / 2 + HeightOffset, (ConfigWidth - MapConfigOffset) / 2, BottomHeight % 2 == 0 ? BottomHeight / 2 : BottomHeight / 2 + 1, "Visuals", ColorSpectrum.LIGHT_STEEL_BLUE);  // Visuals
        DrawColoredBox(TerminalCentre.x - SaveWidth / 2, TerminalCentre.y + GamerulesHeight + StructuresHeight - MapConfigHeight + HeightOffset + BottomHeight, SaveWidth, 3, "", ColorSpectrum.LIGHT_CYAN); // Bottom
        DisplayCenteredTextAtCords(Title, TerminalCentre.x, TerminalCentre.y - ConfigHeight / 2 + HeightOffset + 5, tColor);
        SetCursorPosition(TerminalCentre.x - SaveWidth / 2 + SaveWidth / 2 - 2 , TerminalCentre.y + GamerulesHeight + StructuresHeight - MapConfigHeight + HeightOffset + BottomHeight + 1);
        Write("SAVE");
        CalculateParamCoordinates();
        DrawAllParams();
    }
    #endregion
}
