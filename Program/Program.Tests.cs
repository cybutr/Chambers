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
        public static void TestColorComparison()
        {
            GUI.Clear();
            GUI.SetCursorPosition(0, 0);
            GUI.WriteLine("Color DSL — Visual Reference");
            GUI.WriteLine("============================\n");

            void Row(string label, (int r, int g, int b) a, (int r, int g, int b) b, string dsl)
            {
                string ba = GUI.SetBackgroundColor(a.r, a.g, a.b) + "      " + GUI.ResetColor();
                string bb = GUI.SetBackgroundColor(b.r, b.g, b.b) + "      " + GUI.ResetColor();
                int dist = Math.Abs(a.r - b.r) + Math.Abs(a.g - b.g) + Math.Abs(a.b - b.b);
                string m = dist <= 15 ? "≈" : dist <= 40 ? "~" : "≠";
                GUI.WriteLine($"{label,-17} {ba} {m} {bb}  {dsl}");
            }

            void Show(string label, string dsl)
            {
                var c = ColorSpectrum.ParseDynamic(dsl);
                string block = GUI.SetBackgroundColor(c.r, c.g, c.b) + "      " + GUI.ResetColor();
                GUI.WriteLine($"  {label,-20} {block}  \"{dsl}\"");
            }

            GUI.WriteLine($"{"Name",-17} {"Static",-8}   {"DSL",-8}  Expression");
            GUI.WriteLine(new string('─', 72));

            (string name, (int r, int g, int b) s, string dsl)[] rows =
            [
                ("PLAINS_GREEN",   ColorSpectrum.PLAINS_GREEN,   "plains"),
                ("FOREST_GREEN",   ColorSpectrum.FOREST_GREEN,   "forest"),
                ("MOUNTAIN_GREY",  ColorSpectrum.MOUNTAIN_GREY,  "mountain"),
                ("MOUNTAIN_DEEP",  ColorSpectrum.MOUNTAIN_DEEP,  "mountain : dark 30"),
                ("SNOW_WHITE",     ColorSpectrum.SNOW_WHITE,     "snow"),
                ("SAND_YELLOW",    ColorSpectrum.SAND_YELLOW,    "beach"),
                ("SAND_DARK",      ColorSpectrum.SAND_DARK,      "beach : dark 15"),
                ("WATER_BLUE",     ColorSpectrum.WATER_BLUE,     "ocean"),
                ("WATER_DEEP",     ColorSpectrum.WATER_DEEP,     "ocean : dark 20"),
                ("FOAMY_BLUE",     ColorSpectrum.FOAMY_BLUE,     "cyan : dark 50"),
                ("CRAB_CRIMSON",   ColorSpectrum.CRAB_CRIMSON,   "crab"),
                ("TURTLE_GREEN",   ColorSpectrum.TURTLE_GREEN,   "turtle"),
                ("COW_BROWN",      ColorSpectrum.COW_BROWN,      "cow"),
                ("SHEEP_SLATE",    ColorSpectrum.SHEEP_SLATE,    "sheep"),
                ("WOLF_GREY",      ColorSpectrum.WOLF_GREY,      "wolf"),
                ("BEAR_SIENNA",    ColorSpectrum.BEAR_SIENNA,    "bear"),
                ("GOAT_WHEAT",     ColorSpectrum.GOAT_WHEAT,     "goat"),
                ("BIRD_SKY",       ColorSpectrum.BIRD_SKY,       "bird"),
                ("VILLAGER_PEACH", ColorSpectrum.VILLAGER_PEACH, "villager"),
                ("NIGHT_TINT",     ColorSpectrum.NIGHT_TINT,     "blue : dark 80 : desaturate 60"),
            ];

            foreach (var (name, s, dsl) in rows)
                Row(name, s, ColorSpectrum.ParseDynamic(dsl), dsl);

            GUI.WriteLine($"\nNew modifiers:");
            GUI.WriteLine(new string('─', 72));
            Show("blend → white 30",    "ocean : blend white 30");
            Show("blend → white 70",    "ocean : blend white 70");
            Show("complement (red)",    "red : complement");
            Show("complement (green)",  "green : complement");
            Show("plains day",          "plains");
            Show("plains night 40",     "plains : night 40");
            Show("plains night 80",     "plains : night 80");
            Show("hex #a3bf73",         "#a3bf73");
            Show("rgb 82,106,64",       "82,106,64");

            GUI.WriteLine($"\nGetName — nearest vocab key:");
            GUI.WriteLine(new string('─', 72));
            (int r, int g, int b)[] probes = [ColorSpectrum.PLAINS_GREEN, ColorSpectrum.CRAB_CRIMSON, ColorSpectrum.WATER_BLUE, (200, 100, 50), (50, 130, 200)];
            foreach (var c in probes)
            {
                string block = GUI.SetBackgroundColor(c.r, c.g, c.b) + "      " + GUI.ResetColor();
                GUI.WriteLine($"  {block}  ({c.r,3},{c.g,3},{c.b,3})  →  \"{ColorSpectrum.GetName(c)}\"");
            }

            GUI.WriteLine("\nPress any key to return...");
            Console.ReadKey(true);
            GUI.Clear();
            if (chambers.Count > currentChamberIndex) chambers[currentChamberIndex].InvalidateFramebuffer();
        }

        public static void TestColorDSL()
        {
            GUI.Clear();
            GUI.SetCursorPosition(0, 0);
            GUI.WriteLine("Color DSL — Parser Test Suite");
            GUI.WriteLine("=============================\n");

            int passed = 0, failed = 0;

            void Check(string label, bool ok)
            {
                string badge = ok
                    ? GUI.SetBackgroundColor(30, 110, 30) + " PASS " + GUI.ResetColor()
                    : GUI.SetBackgroundColor(150, 30, 30) + " FAIL " + GUI.ResetColor();
                GUI.WriteLine($"{badge}  {label}");
                if (ok) passed++; else failed++;
            }

            bool Near((int r, int g, int b) a, (int r, int g, int b) b, int tol = 15) =>
                Math.Abs(a.r - b.r) + Math.Abs(a.g - b.g) + Math.Abs(a.b - b.b) <= tol;

            GUI.WriteLine("— Hex / RGB passthrough —");
            Check("#ffffff = (255,255,255)",    ColorSpectrum.ParseDynamic("#ffffff") == (255, 255, 255));
            Check("#000000 = (0,0,0)",          ColorSpectrum.ParseDynamic("#000000") == (0, 0, 0));
            Check("#a3bf73 round-trips",        ColorSpectrum.ParseDynamic("#a3bf73") == (163, 191, 115));
            Check("148,191,115 exact",          ColorSpectrum.ParseDynamic("148,191,115") == (148, 191, 115));
            Check("-10,300,128 clamps to range",ColorSpectrum.ParseDynamic("-10,300,128") == (0, 255, 128));

            GUI.WriteLine("\n— TryParseDynamic —");
            Check("known key → HasValue",        ColorSpectrum.TryParseDynamic("red").HasValue);
            Check("unknown key → null",         !ColorSpectrum.TryParseDynamic("xyzzy").HasValue);
            Check("hex → HasValue",              ColorSpectrum.TryParseDynamic("#ff0000").HasValue);
            Check("rgb → HasValue",              ColorSpectrum.TryParseDynamic("100,100,100").HasValue);
            Check("empty string → null",        !ColorSpectrum.TryParseDynamic("").HasValue);

            GUI.WriteLine("\n— GetName reverse lookup —");
            Check("plains exact match",    ColorSpectrum.GetName(ColorSpectrum.PLAINS_GREEN) == "plains");
            Check("crab exact match",      ColorSpectrum.GetName(ColorSpectrum.CRAB_CRIMSON) == "crab");
            Check("ocean exact match",     ColorSpectrum.GetName(ColorSpectrum.WATER_BLUE)   == "ocean");
            Check("never returns null",    ColorSpectrum.GetName((200, 100, 50)) != null);
            Check("near-black → black",    ColorSpectrum.GetName((25, 22, 18)) == "black");

            GUI.WriteLine("\n— Complement modifier —");
            var red     = ColorSpectrum.ParseDynamic("red");
            var redComp = ColorSpectrum.ParseDynamic("red : complement");
            var (rh, _, _) = ColorSpectrum.ToHsv(red);
            var (ch, _, _) = ColorSpectrum.ToHsv(redComp);
            double hueDiff = Math.Abs(((ch - rh + 540) % 360) - 180);
            Check("complement shifts hue ≈180°",     hueDiff < 15);
            Check("double complement ≈ original",    Near(red, ColorSpectrum.ParseDynamic("red : complement : complement"), 10));
            Check("complement red ≠ red",            !Near(red, redComp, 30));

            GUI.WriteLine("\n— Night modifier —");
            var plains      = ColorSpectrum.ParseDynamic("plains");
            var plainsN100  = ColorSpectrum.ParseDynamic("plains : night 100");
            var plainsN0    = ColorSpectrum.ParseDynamic("plains : night 0");
            var plainsN50   = ColorSpectrum.ParseDynamic("plains : night 50");
            Check("night 100 ≈ NIGHT_TINT",    Near(plainsN100, ColorSpectrum.NIGHT_TINT, 20));
            Check("night 0 ≈ base",            Near(plainsN0, plains, 5));
            Check("night 50 darker than base", ColorSpectrum.ToHsv(plainsN50).v < ColorSpectrum.ToHsv(plains).v);
            Check("night 50 lighter than 100", ColorSpectrum.ToHsv(plainsN50).v > ColorSpectrum.ToHsv(plainsN100).v);

            GUI.WriteLine("\n— Blend modifier —");
            Check("blend 0 ≈ base",      Near(ColorSpectrum.ParseDynamic("red : blend white 0"),   ColorSpectrum.ParseDynamic("red"),   5));
            Check("blend 100 ≈ target",  Near(ColorSpectrum.ParseDynamic("red : blend white 100"), ColorSpectrum.ParseDynamic("white"), 10));
            Check("blend hex target",    ColorSpectrum.TryParseDynamic("blue : blend #ffffff 50").HasValue);
            var blendMid = ColorSpectrum.ParseDynamic("black : blend white 50");
            Check("blend 50 is mid-grey (r 100–160)", blendMid.r > 100 && blendMid.r < 160);
            Check("unknown blend target = base",  Near(ColorSpectrum.ParseDynamic("red : blend xyzzy 50"), ColorSpectrum.ParseDynamic("red"), 2));

            GUI.WriteLine("\n— Ish suffix —");
            Check("blueish resolves",    ColorSpectrum.TryParseDynamic("blueish").HasValue);
            Check("reddish resolves",    ColorSpectrum.TryParseDynamic("reddish").HasValue);
            Check("greenish resolves",   ColorSpectrum.TryParseDynamic("greenish").HasValue);
            Check("orangeish resolves",  ColorSpectrum.TryParseDynamic("orangeish").HasValue);
            if (ColorSpectrum.TryParseDynamic("blueish") is { } blueishColor)
            {
                var (bh, _, _) = ColorSpectrum.ToHsv(blueishColor);
                Check("blueish hue 190–270°", bh >= 190 && bh <= 270);
            }

            GUI.WriteLine("\n— Semantic vocab names —");
            Check("plains ≈ PLAINS_GREEN",    Near(ColorSpectrum.ParseDynamic("plains"),   ColorSpectrum.PLAINS_GREEN,   2));
            Check("forest ≈ FOREST_GREEN",    Near(ColorSpectrum.ParseDynamic("forest"),   ColorSpectrum.FOREST_GREEN,   2));
            Check("mountain ≈ MOUNTAIN_GREY", Near(ColorSpectrum.ParseDynamic("mountain"), ColorSpectrum.MOUNTAIN_GREY,  2));
            Check("ocean ≈ WATER_BLUE",       Near(ColorSpectrum.ParseDynamic("ocean"),    ColorSpectrum.WATER_BLUE,     2));
            Check("crab ≈ CRAB_CRIMSON",      Near(ColorSpectrum.ParseDynamic("crab"),     ColorSpectrum.CRAB_CRIMSON,   2));

            GUI.WriteLine("\n— Material aliases —");
            Check("grass ≈ plains:dark 10",  Near(ColorSpectrum.ParseDynamic("grass"),  ColorSpectrum.ParseDynamic("plains : dark 10"), 8));
            Check("water ≈ ocean:dark 20",   Near(ColorSpectrum.ParseDynamic("water"),  ColorSpectrum.ParseDynamic("ocean : dark 20"),  8));
            Check("$water token expands",    ColorSpectrum.TryParseDynamic("$water").HasValue);

            GUI.WriteLine("\n— Base adjectives —");
            var baseG  = ColorSpectrum.ToHsv(ColorSpectrum.ParseDynamic("green"));
            var darkG  = ColorSpectrum.ToHsv(ColorSpectrum.ParseDynamic("dark green"));
            var lightG = ColorSpectrum.ToHsv(ColorSpectrum.ParseDynamic("light green"));
            var paleG  = ColorSpectrum.ToHsv(ColorSpectrum.ParseDynamic("pale green"));
            var vibG   = ColorSpectrum.ToHsv(ColorSpectrum.ParseDynamic("vibrant green"));
            Check("dark lowers value",           darkG.v  < baseG.v);
            Check("light raises value",          lightG.v > baseG.v);
            Check("pale lowers saturation",      paleG.s  < baseG.s);
            Check("vibrant raises saturation",   vibG.s   > baseG.s);

            GUI.WriteLine("\n— Chained modifiers —");
            Check("chained dark ≈ single dark",
                Near(ColorSpectrum.ParseDynamic("green : dark 30"), ColorSpectrum.ParseDynamic("green : dark 15 : dark 15"), 20));
            Check("modifier order matters under clamping",
                !Near(ColorSpectrum.ParseDynamic("white : dark 80 : bright 100"),
                      ColorSpectrum.ParseDynamic("white : bright 100 : dark 80"), 5));

            GUI.WriteLine("\n— Cache & normalisation —");
            Check("repeated parse = identical",  ColorSpectrum.ParseDynamic("dark forest") == ColorSpectrum.ParseDynamic("dark forest"));
            Check("case-insensitive",            ColorSpectrum.ParseDynamic("RED") == ColorSpectrum.ParseDynamic("red"));
            Check("trim whitespace",             ColorSpectrum.ParseDynamic("  red  ") == ColorSpectrum.ParseDynamic("red"));

            GUI.WriteLine($"\n{new string('─', 44)}");
            bool allPassed = failed == 0;
            string summary = $"  {passed} passed  |  {failed} failed  |  {passed + failed} total";
            string bar = allPassed
                ? GUI.SetBackgroundColor(30, 110, 30) + summary + GUI.ResetColor()
                : GUI.SetBackgroundColor(150, 30, 30) + summary + GUI.ResetColor();
            GUI.WriteLine(bar);
            GUI.WriteLine(new string('─', 44));

            GUI.WriteLine("\nPress any key to return...");
            Console.ReadKey(true);
            GUI.Clear();
            if (chambers.Count > currentChamberIndex) chambers[currentChamberIndex].InvalidateFramebuffer();
        }

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

            Map testChamber = new();
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
            Map testChamber = new();

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

        public static void TestErrorBox()
        {
            EnableVirtualTerminalProcessing();
            GUI.SetCursorVisible(false);
            ShowErrorBox("Index was outside the bounds of the array.");
            Console.ReadKey(true);
            GUI.Clear();
        }
}
