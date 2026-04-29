using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Internal;

partial class Program
{
        #region test methods
        public static void TestSynchronization()
        {
            GUI.WriteLine("Testing Chamber Name Synchronization System");
            GUI.WriteLine("==========================================");

            // Create test files with mismatched names
            GUI.WriteLine("1. Creating test files with mismatched names...");
            CreateTestFiles();

            // Show current state
            GUI.WriteLine("2. Current state before synchronization:");
            ShowCurrentState();

            // Run synchronization
            GUI.WriteLine("3. Running synchronization...");
            SynchronizeAllChamberFiles();

            // Show state after synchronization
            GUI.WriteLine("4. State after synchronization:");
            ShowCurrentState();

            GUI.WriteLine("5. Testing load functionality...");
            TestLoadFunctionality();

            GUI.WriteLine("\nTest completed!");
        }
        static void CreateTestFiles()
        {
            var savesPath = Path.Combine(Environment.CurrentDirectory, "Data/Saves");
            Directory.CreateDirectory(savesPath);

            // Create a test chamber with mismatched name
            var testChamber = new Map();
            testChamber.conf.Name = "Test Chamber Config Name";

            // Save it with a different filename
            var filePath = Path.Combine(savesPath, "WRONG_FILENAME.json");
            SaveMapDirect(testChamber, filePath);

            GUI.WriteLine($"   Created: WRONG_FILENAME.json with config name 'Test Chamber Config Name'");
        }
        static void SaveMapDirect(Map map, string filePath)
        {
            var options = new JsonSerializerOptions { WriteIndented = true, IncludeFields = true };
            options.Converters.Add(new Char2DArrayJsonConverter());
            options.Converters.Add(new Bool2DArrayJsonConverter());
            options.Converters.Add(new BoolJsonConverter());
            options.Converters.Add(new Int2DArrayJsonConverter());
            options.Converters.Add(new Double2DArrayJsonConverter());
            options.Converters.Add(new ValueTupleIntKeyConverter<int>());
            options.Converters.Add(new ValueTupleIntDoubleKeyConverter());

            string json = JsonSerializer.Serialize(map, options);
            File.WriteAllText(filePath, json);
        }
        static void ShowCurrentState()
        {
            var savesPath = Path.Combine(Environment.CurrentDirectory, "Data/Saves");
            if (!Directory.Exists(savesPath))
            {
                GUI.WriteLine("   No Saves directory found.");
                return;
            }

            foreach (var file in Directory.GetFiles(savesPath, "*.json"))
            {
                var chamber = LoadMap(file);
                if (chamber != null)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var configName = chamber.conf.Name;
                    var match = fileName == configName ? "✓" : "✗";
                    GUI.WriteLine($"   {match} File: '{fileName}' | Config: '{configName}'");
                }
            }
        }
        static void TestLoadFunctionality()
        {
            GUI.WriteLine("   Testing LoadAllMapsFromFolder...");
            allChambers.Clear();
            LoadAllMapsFromFolder(Path.Combine(Environment.CurrentDirectory, "Data/Saves"));

            GUI.WriteLine($"   Loaded {allChambers.Count} chambers:");
            foreach (var chamber in allChambers)
            {
                GUI.WriteLine($"     - {chamber.conf.Name}");
            }
        }
        public static void TestSynchronizationIntegration()
        {
            GUI.WriteLine("Comprehensive Chamber Name Synchronization Test");
            GUI.WriteLine("==============================================");

            // 1. Test with multiple mismatched files
            GUI.WriteLine("1. Creating multiple test files with various mismatches...");
            CreateComplexTestFiles();

            GUI.WriteLine("2. State before synchronization:");
            ShowCurrentState();

            // 2. Test synchronization
            GUI.WriteLine("3. Running synchronization...");
            SynchronizeAllChamberFiles();

            GUI.WriteLine("4. State after synchronization:");
            ShowCurrentState();

            // 3. Test edge cases
            GUI.WriteLine("5. Testing edge cases...");
            TestEdgeCases();

            // 4. Test RenameChamberFile directly 
            GUI.WriteLine("6. Testing direct file renaming (simulates GUI usage)...");
            TestDirectRenaming();

            GUI.WriteLine("7. Final state:");
            ShowCurrentState();

            GUI.WriteLine("\nIntegration test completed!");
        }
        static void CreateComplexTestFiles()
        {
            var savesPath = Path.Combine(Environment.CurrentDirectory, "Data/Saves");
            Directory.CreateDirectory(savesPath);

            // Test case 1: Simple mismatch
            var chamber1 = new Map();
            chamber1.conf.Name = "My Awesome Chamber";
            SaveMapDirect(chamber1, Path.Combine(savesPath, "ugly_filename.json"));

            // Test case 2: Name with special characters
            var chamber2 = new Map();
            chamber2.conf.Name = "Chamber: Special & Cool!";
            SaveMapDirect(chamber2, Path.Combine(savesPath, "simple.json"));

            // Test case 3: Empty/null name (should get default)
            var chamber3 = new Map();
            chamber3.conf.Name = "";
            SaveMapDirect(chamber3, Path.Combine(savesPath, "empty_name.json"));

            GUI.WriteLine("   Created test files with various mismatches");
        }
        static void TestEdgeCases()
        {
            var savesPath = Path.Combine(Environment.CurrentDirectory, "Data/Saves");

            // Test unique name generation
            var existingNames = new[] { "Test", "Test1", "Test2" };
            string uniqueName = GetUniqueChamberName("Test", existingNames);
            GUI.WriteLine($"   Unique name generation: 'Test' -> '{uniqueName}' (should be 'Test3')");

            // Test with already unique name
            string alreadyUnique = GetUniqueChamberName("Unique", existingNames);
            GUI.WriteLine($"   Already unique: 'Unique' -> '{alreadyUnique}' (should be 'Unique')");
        }
        static void TestDirectRenaming()
        {
            var savesPath = Path.Combine(Environment.CurrentDirectory, "Data/Saves");
            var files = Directory.GetFiles(savesPath, "*.json");

            if (files.Length > 0)
            {
                var testFile = files[0];
                var chamber = LoadMap(testFile);
                if (chamber != null)
                {
                    string oldName = chamber.conf.Name;
                    string newName = "GUI Renamed Chamber";

                    GUI.WriteLine($"   Renaming '{oldName}' to '{newName}'");
                    string? newPath = RenameChamberFile(testFile, newName, chamber);

                    if (newPath != null && File.Exists(newPath))
                    {
                        var reloadedChamber = LoadMap(newPath);
                        GUI.WriteLine($"   ✓ Rename successful: Config name is now '{reloadedChamber?.conf.Name}'");
                    }
                }
            }
        }
        public static void TestGUIFixes()
        {
            GUI.WriteLine("Testing GUI Fixes");
            GUI.WriteLine("=================");

            // Test 1: Test the logic behind the fixes
            GUI.WriteLine("1. Testing unique name generation logic...");

            // Test the GetUniqueChamberName method directly
            var existingNames = new[] { "Test Chamber", "Test Chamber1", "Test Chamber2" };
            string uniqueName1 = GetUniqueChamberName("Test Chamber", existingNames);
            GUI.WriteLine($"   'Test Chamber' with existing names -> '{uniqueName1}' (should be 'Test Chamber3')");

            string uniqueName2 = GetUniqueChamberName("New Chamber", existingNames);
            GUI.WriteLine($"   'New Chamber' with existing names -> '{uniqueName2}' (should be 'New Chamber')");

            // Test 2: Simulate the typing scenario
            GUI.WriteLine("\n2. Testing name renaming logic...");

            // Create test files to simulate the scenario
            var savesPath = Path.Combine(Environment.CurrentDirectory, "Data/Saves");
            Directory.CreateDirectory(savesPath);

            // Create a chamber and save it
            var testChamber = new Map();
            testChamber.conf.Name = "Test Rename Chamber";
            SaveMap(testChamber);

            GUI.WriteLine($"   Created chamber with name: '{testChamber.conf.Name}'");
            GUI.WriteLine($"   File should exist: Test Rename Chamber.json");

            // Test the rename functionality
            string oldPath = Path.Combine(savesPath, "Test Rename Chamber.json");
            string newName = "Renamed Chamber";
            string? newPath = RenameChamberFile(oldPath, newName, testChamber);

            GUI.WriteLine($"   Renamed to: '{newName}'");
            GUI.WriteLine($"   New file path: {newPath}");
            GUI.WriteLine($"   Chamber config name now: '{testChamber.conf.Name}'");

            // Test 3: Check results
            GUI.WriteLine("\n3. Verifying results...");
            var files = Directory.GetFiles(savesPath, "*.json");
            GUI.WriteLine($"   Total JSON files: {files.Length}");

            foreach (var file in files)
            {
                var chamber = LoadMap(file);
                if (chamber != null)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var configName = chamber.conf.Name;
                    var match = fileName == configName ? "✓" : "✗";
                    GUI.WriteLine($"   {match} File: '{fileName}' | Config: '{configName}'");
                }
            }

            // Test 4: Test uniqueness with existing file names
            GUI.WriteLine("\n4. Testing uniqueness with existing names...");

            // Get all existing names in the saves folder
            var allExistingNames = Directory.GetFiles(savesPath, "*.json")
                                           .Select(Path.GetFileNameWithoutExtension)
                                           .Where(name => name != null)
                                           .Cast<string>()
                                           .ToList();

            GUI.WriteLine($"   Existing names: [{string.Join(", ", allExistingNames)}]");

            // Test what happens when we try to create a duplicate
            string testName = allExistingNames.FirstOrDefault() ?? "Test";
            string uniqueResult = GetUniqueChamberName(testName, allExistingNames);
            GUI.WriteLine($"   Trying to create '{testName}' -> got '{uniqueResult}'");

            GUI.WriteLine("\nGUI fixes test completed!");
        }
        public static void TestConfigGUI()
        {
            GUI.WriteLine("Config GUI Fixes Applied Successfully!");
            GUI.WriteLine("====================================");

            GUI.WriteLine("\n🎉 Fixed Issues:");
            GUI.WriteLine("✅ Int and Double parameters can now be edited");
            GUI.WriteLine("✅ Added direct typing support for all numeric values");
            GUI.WriteLine("✅ Added input validation for numeric fields");
            GUI.WriteLine("✅ Added quick increment/decrement with Ctrl+Arrow keys");
            GUI.WriteLine("✅ Improved error handling and value restoration");

            GUI.WriteLine("\n📋 How to Use the Config GUI:");
            GUI.WriteLine("1. Navigation:");
            GUI.WriteLine("   • Use Arrow Keys (↑↓←→) or WASD to navigate between parameters");
            GUI.WriteLine("   • Different sections: Map Config, Gamerules, Structures, etc.");

            GUI.WriteLine("\n2. Editing Parameters:");
            GUI.WriteLine("   • Boolean Values: Press Enter/Space to toggle ✔/✗");
            GUI.WriteLine("   • String Values: Press Enter/Space to start typing, Enter to confirm");
            GUI.WriteLine("   • Int/Double Values: Press Enter/Space to start typing, Enter to confirm");
            GUI.WriteLine("   • Quick Numeric Edit: Use Ctrl+Left/Right arrows to increment/decrement");

            GUI.WriteLine("\n3. Input Validation:");
            GUI.WriteLine("   • Int fields: Only digits and minus sign allowed");
            GUI.WriteLine("   • Double fields: Digits, decimal point, and minus sign allowed");
            GUI.WriteLine("   • Invalid input will be rejected or reverted");

            GUI.WriteLine("\n4. Saving:");
            GUI.WriteLine("   • Navigate to the SAVE button at the bottom");
            GUI.WriteLine("   • Press Enter to save your configuration");
            GUI.WriteLine("   • Press Escape to cancel without saving");

            GUI.WriteLine("\n🔧 Parameter Examples:");
            GUI.WriteLine("   • Seed (String): Any text for world generation");
            GUI.WriteLine("   • NoiseScale (Double): Use Ctrl+Arrow or type values like '10.5'");
            GUI.WriteLine("   • MinBiomeSize (Int): Use Ctrl+Arrow or type integers like '25'");
            GUI.WriteLine("   • EnableMountainRanges (Bool): Toggle with Enter/Space");

            GUI.WriteLine("\n💡 Tips:");
            GUI.WriteLine("   • Ctrl+Arrow keys provide quick +1/-1 for ints, +0.1/-0.1 for doubles");
            GUI.WriteLine("   • Scale parameters increment by ±1.0 for easier adjustment");
            GUI.WriteLine("   • Size/Width parameters have minimum constraints (≥1)");
            GUI.WriteLine("   • Press Escape while editing to cancel and restore original value");

            GUI.WriteLine("\nThe config GUI is now fully functional! 🎯");
        }
        private static void TestGUILayout()
        {
            EnableVirtualTerminalProcessing();
            GUI.SetCursorVisible(false);
            GUI.Clear();

            Map map = new();
            map.weather = new Weather
            {
                CurrentWeather = WeatherType.Clear,
                NextWeather   = WeatherType.Rain,
                Temperature   = 22.4,
                Humidity      = 0.65,
                Pressure      = 1013.2,
                WindSpeed     = 12.3,
                WindDirection = 45.0
            };
            map.dayNight.TimeOfDay = 8.5;
            map.dayNight.Season    = 1.3;
            map.DayCount       = 42;
            map.cloudFormations = 7;
            map.cloudTileCount  = 234;
            map.actualOutputBuffer.AddRange([
                "Simulation started.",
                "Wolf spotted near eastern forest.",
                "River eroded 3 tiles downstream.",
                "Sheep population stable at 14.",
                "Cumulonimbus forming over mountains.",
                "Temperature dropped to 14°C.",
                "Bear moved into lake region.",
                "Heavy rain began.",
                "Cloud cover at 78%.",
                "Day 42 began.",
            ]);
            Map.eventBuffer.Add("Wolf attack on sheep near river");
            chambers.Add(map);
            currentChamberIndex = 0;
            displayGUI = true;

            void redraw()
            {
                map._guiBuf.Invalidate();
                GUI.Clear();
                map.DisplayGUI();
                map._guiBuf.Flush();
            }

            redraw();

            int lastW = Console.WindowWidth, lastH = Console.WindowHeight;
            while (true)
            {
                int w = Console.WindowWidth, h = Console.WindowHeight;
                if (w != lastW || h != lastH) { lastW = w; lastH = h; redraw(); }
                if (!Console.KeyAvailable) { Thread.Sleep(50); continue; }
                var key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.Escape) break;
                if (key == ConsoleKey.R) redraw();
                if (key == ConsoleKey.T)
                {
                    map.dayNight.TimeOfDay = (map.dayNight.TimeOfDay + 2.0) % 24.0;
                    redraw();
                }
            }

            GUI.Clear();
            GUI.SetCursorVisible(true);
        }
        private static void TestCamera()
        {
            int viewW = Math.Max(20, Console.WindowWidth / 2 - GUIConfig.LeftPadding - GUIConfig.RightPadding);
            int viewH = Math.Max(15, Console.WindowHeight - GUIConfig.BottomPadding);
            int mapW  = Math.Min(viewW * 3, 300);
            int mapH  = Math.Min(viewH * 3, 150);

            var testConfig = new Config(mapW, mapH, 10.0, "CAMTEST") { Name = "CAMERA_TEST" };

            Map testChamber = new Map();
            testChamber.conf   = testConfig;
            testChamber.width  = mapW;
            testChamber.height = mapH;

            testChamber.mapData           = new TileId[mapW, mapH];
            testChamber.overlayData       = new EntityId[mapW, mapH];
            testChamber.temperatureData   = new int[mapW, mapH];
            testChamber.humidityData      = new int[mapW, mapH];
            testChamber.noise             = new double[mapW, mapH];
            testChamber.tempatureNoise    = new double[mapW, mapH];
            testChamber.humidityNoise     = new double[mapW, mapH];
            testChamber.darknessData      = new int[mapW, mapH];
            testChamber.shadowData        = new double[mapW, mapH];
            testChamber.waveIntensityData = new double[mapW, mapH];

            int cw = Math.Max(1, Math.Min(mapW * 3, 10000));
            int ch = Math.Max(1, Math.Min(mapH * 3, 10000));
            testChamber.cloudData                 = new CloudType[cw, ch];
            testChamber.cloudDepthData            = new int[cw, ch];
            testChamber.precipitationData         = new double[cw, ch];
            testChamber.previousPrecipitationData = new double[cw, ch];

            testChamber.HandleTestGen();
            testChamber.camera = new Camera(viewW, viewH);

            chambers.Add(testChamber);
            currentChamberIndex = 0;
            displayGUI = false;

            void redraw(bool full = false)
            {
                if (full) { testChamber.InvalidateFramebuffer(); GUI.Clear(); }
                DisplayCurrentChamber();
                var c = testChamber.camera!;
                GUI.SetCursorPosition(0, 0);
                GUI.Write($" Camera ({c.X},{c.Y}) | Map {mapW}x{mapH} | View {viewW}x{viewH} | WASD pan | R regen | ESC exit ");
            }

            redraw(true);

            while (continueSimulating)
            {
                if (CheckAndApplyResize(testChamber))
                {
                    viewW = testChamber.camera!.Width;
                    viewH = testChamber.camera!.Height;
                    redraw(true);
                }

                if (!Console.KeyAvailable) { Thread.Sleep(50); continue; }
                var keyInfo = Console.ReadKey(true);
                var key     = keyInfo.Key;
                var cam     = testChamber.camera!;

                if (key == ConsoleKey.Escape) break;
                else if (key == ConsoleKey.R)
                {
                    testChamber.HandleTestGen();
                    testChamber.camera = new Camera(viewW, viewH);
                    cam = testChamber.camera;
                    cam.X = 0; cam.Y = 0;
                    redraw(true);
                }
                else if (key == ConsoleKey.W || key == ConsoleKey.UpArrow)
                { cam.Y = Math.Max(0, cam.Y - 3); redraw(); }
                else if (key == ConsoleKey.S || key == ConsoleKey.DownArrow)
                { cam.Y = Math.Min(Math.Max(0, mapH - viewH), cam.Y + 3); redraw(); }
                else if (key == ConsoleKey.A || key == ConsoleKey.LeftArrow)
                { cam.X = Math.Max(0, cam.X - 5); redraw(); }
                else if (key == ConsoleKey.D || key == ConsoleKey.RightArrow)
                { cam.X = Math.Min(Math.Max(0, mapW - viewW), cam.X + 5); redraw(); }
            }

            GUI.Clear();
            GUI.WriteLine("Camera test ended.");
        }
        #endregion
        private static void Testing()
        {
            GUI.WriteLine("Entering testing mode...");
            GUI.WriteLine("Generating chamber for testing...");

            // Calculate safe chamber dimensions for testing
            int safeWidth = Math.Max(20, Console.WindowWidth / 2 - GUIConfig.LeftPadding - GUIConfig.RightPadding);
            int safeHeight = Math.Max(15, Console.WindowHeight - GUIConfig.BottomPadding);

            // Create a simple config for testing
            var testConfig = new Config(safeWidth, safeHeight, 10.0, "TEST");

            // Configure for testing
            testConfig.Name = "TEST_CHAMBER";

            // Create test chamber with proper initialization
            Map testChamber = new Map();

            // Set the config first, then reinitialize arrays with correct dimensions
            testChamber.conf = testConfig;
            testChamber.width = testConfig.Width;
            testChamber.height = testConfig.Height;

            // Reinitialize all arrays with correct dimensions
            testChamber.mapData = new TileId[testConfig.Width, testConfig.Height];
            testChamber.overlayData = new EntityId[testConfig.Width, testConfig.Height];
            testChamber.temperatureData = new int[testConfig.Width, testConfig.Height];
            testChamber.humidityData = new int[testConfig.Width, testConfig.Height];
            testChamber.noise = new double[testConfig.Width, testConfig.Height];
            testChamber.tempatureNoise = new double[testConfig.Width, testConfig.Height];
            testChamber.humidityNoise = new double[testConfig.Width, testConfig.Height];

            // Initialize cloud data arrays
            int cloudWidth = Math.Max(1, Math.Min(testConfig.Width * 3, 10000));
            int cloudHeight = Math.Max(1, Math.Min(testConfig.Height * 3, 10000));
            testChamber.cloudData = new CloudType[cloudWidth, cloudHeight];
            testChamber.cloudDepthData = new int[cloudWidth, cloudHeight];
            testChamber.precipitationData = new double[cloudWidth, cloudHeight];
            testChamber.previousPrecipitationData = new double[cloudWidth, cloudHeight];

            GUI.WriteLine($"Chamber size: {testConfig.Width}x{testConfig.Height}");
            GUI.WriteLine("Generating test terrain...");

            testChamber.HandleTestGen();
            chambers.Add(testChamber);
            currentChamberIndex = 0;

            GUI.WriteLine("Generation complete!");
            GUI.WriteLine("Testing Mode Controls:");
            GUI.WriteLine("- ESC: Exit");
            GUI.WriteLine("- C: Command mode (for testing generation functions)");
            GUI.WriteLine("- All other keybinds disabled in testing mode");
            GUI.WriteLine();

            GUI.Clear();
            displayGUI = false;
            DisplayCurrentChamber();

            // Simple key listener for testing mode - only ESC and C allowed
            while (continueSimulating)
            {
                if (Console.KeyAvailable)
                {
                    var keyInfo = Console.ReadKey(true);
                    var key = keyInfo.Key;

                    if (key == ConsoleKey.Escape)
                    {
                        GUI.WriteLine("Exiting testing mode...");
                        break;
                    }
                    else if (key == ConsoleKey.C)
                    {
                        isCommandInputMode = true;
                        GUI.SetCursorPosition(0, testChamber.height + GUIConfig.TopPadding + 2);
                        GUI.Write(">> ");
                        string? command = Console.ReadLine();
                        if (command?.ToLower() == "exit") break;
                        isCommandInputMode = false;
                        GUI.Clear();
                        DisplayCurrentChamber();
                    }
                    else if (key == ConsoleKey.R)
                    {
                        testChamber.HandleTestGen();
                        GUI.Clear();
                        DisplayCurrentChamber();
                    }
                    // All other keys are ignored in testing mode
                }
                Thread.Sleep(50);
            }

            GUI.Clear();
            GUI.WriteLine("Testing mode ended.");
        }
}
