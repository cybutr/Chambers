using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography.X509Certificates;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Diagnostics;
using Microsoft.VisualBasic;
using System.Security.Authentication;
using System.Text.Json;
using System.Text.Json.Serialization;

#nullable enable
namespace Internal
{
    public class Program
    {
        #region map
        //public static int seed = DateTime.Now.Millisecond;
        public static string seedString = Math.Round(new Random().Next() * ((new Random().NextDouble() - 0.5) * 2)).ToString();
        public static int seed = ConvertStringToNumbers(seedString);
        public Random rng = new Random(seed);
        public static List<Map> chambers = new List<Map>();
        public static List<Map> allChambers = new List<Map>();
        public static int currentChamberIndex;
        public static bool continueSimulating = true;
        public static bool isUpdating = false;
        public static bool isCommandInputMode = false;
        public static bool isCloudsRendering = false;
        public static bool isCloudsShadowsRendering = true;
        public static bool IsHumidityRendering = false;
        public static bool IsTemperatureRendering = false;
        public static bool isConfiguring = false;
        #endregion
        private static bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        private static readonly object mapLock = new object();
        public static List<string> outputBuffer = new List<string>();
        public static List<string> eventBuffer = new List<string>();
        public static List<(Map chamber, string? name, bool isSelected, bool isTyping, bool isEmpty)> slots = new List<(Map chamber, string? name, bool isSelected, bool isTyping, bool isEmpty)>();
        public static Config config = new Config(Console.WindowWidth / 2 - GUIConfig.LeftPadding - GUIConfig.RightPadding, Console.WindowHeight - GUIConfig.BottomPadding - GUIConfig.TopPadding, 
        10.0, seedString);
        // Loading screen
        public static string asciiArt = @"
                        _,.---._        ,---.                       .=-.-.  .-._               _,---.                     
        _.-.       ,-.' , -  `.    .--.'  \       _,..---._     /==/_ / /==/ \  .-._    _.='.'-,  \                       
        .-,.'|      /==/_,  ,  - \   \==\-/\ \    /==/,   -  \   |==|, |  |==|, \/ /, /  /==.'-     /                     
        |==|, |     |==|   .=.     |  /==/-|_\ |   |==|   _   _\  |==|  |  |==|-  \|  |  /==/ -   .-'                     
        |==|- |     |==|_ : ;=:  - |  \==\,   - \  |==|  .=.   |  |==|- |  |==| ,  | -|  |==|_   /_,-.                    
        |==|, |     |==| , '='     |  /==/ -   ,|  |==|,|   | -|  |==| ,|  |==| -   _ |  |==|  , \_.' )                   
        |==|- `-._   \==\ -    ,_ /  /==/-  /\ - \ |==|  '='   /  |==|- |  |==|  /\ , |  \==\-  ,    (   .=.   .=.   .=.  
        /==/ - , ,/   '.='. -   .'   \==\ _.\=\.-' |==|-,   _`/   /==/. /  /==/, | |- |   /==/ _  ,  /  :=; : :=; : :=; : 
        `--`-----'      `--`--''      `--`         `-.`.____.'    `--`-`   `--`./  `--`   `--`------'    `=`   `=`   `=` ";

        public static void Main(string[] args)
        {
            EnableVirtualTerminalProcessing();
            currentChamberIndex = 0;
            Console.ResetColor();
            Console.Clear();
            LoadAllMapsFromFolder(Path.Combine(Environment.CurrentDirectory, "Saves"));

            eventBuffer.Add("None");
            Console.CursorVisible = false;

            if (Console.WindowHeight < 60 || Console.WindowWidth < 100)
            {
                DisplayCenteredText("Please resize the console window to at least 50 lines.");
                return;
            }

            for (int i = 0; i < numberOfRows; i++)
            {
                // Use existing chamber from allChambers if available, otherwise mark as empty
                if (i < allChambers.Count)
                {
                    var chamber = allChambers[i];
                    slots.Add((chamber, chamber.conf.Name, false, false, false));
                }
                else
                {
                    slots.Add((new Map(), null, false, false, true));
                }
            }
            // Main Loop
            Program programInstance = new Program();
            if (args.Length > 0)
            {
                int.TryParse(args[0], out seed);
            }
            Random globalRandom = new Random(seed);
            Map chamber1 = new Map();
            Console.Clear();
            DrawSaveSelectionGUI();
            Console.Clear();
            DisplayCurrentChamber();
            Console.ResetColor();

            // Start the map update thread
            Thread updateThread = new Thread(() => programInstance.UpdateMaps());
            updateThread.Start();

            // Start the key listener thread
            Thread keyListenerThread = new Thread(() => programInstance.ListenForKeyPress());
            keyListenerThread.Start();

            // Start the weather system thread
            Thread weatherThread = new Thread(() => programInstance.UpdateWeather());
            weatherThread.Start();

            // Start the GUI update thread
            Thread guiThread = new Thread(() => programInstance.UpdateGUI());
            guiThread.Start();
            while (continueSimulating)
            {
                while (!isConfiguring)
                {
                    if (isCommandInputMode)
                    {
                        Console.ResetColor(); // Reset color before reading commands
                        Console.SetCursorPosition(0, chamber1.height + GUIConfig.TopPadding + 2);
                        Console.Write(">> "); // Prompt for input
                        string? command = ReadCommandWithAutocomplete(); // Read input
                        if (command != null)
                        {
                            if (command.ToLower() == "exit")
                            {
                                continueSimulating = false;
                                isConfiguring = true;
                                for (int i = 0; i < GUIConfig.BottomPadding; i++)
                                {
                                    Console.SetCursorPosition(0, Console.BufferHeight - i - 1);
                                    Console.Write(new string(' ', Console.BufferWidth));
                                }
                                Console.CursorVisible = true;
                                break;
                            }
                            else if (command.ToLower().StartsWith("chamber "))
                            {
                                int chamberIndex;
                                if (int.TryParse(command.Split(' ')[1], out chamberIndex) && chamberIndex >= 0 && chamberIndex < chambers.Count)
                                {
                                    currentChamberIndex = chamberIndex;
                                    DisplayCurrentChamber();
                                    for (int i = 0; i < GUIConfig.BottomPadding; i++)
                                    {
                                        Console.SetCursorPosition(0, Console.BufferHeight - i - 1);
                                        Console.Write(new string(' ', Console.BufferWidth));
                                    }
                                }
                            }
                            else
                            {
                                for (int i = 0; i < GUIConfig.BottomPadding; i++)
                                {
                                    Console.SetCursorPosition(0, Console.BufferHeight - i - 1);
                                    Console.Write(new string(' ', Console.BufferWidth));
                                }
                                continueSimulating = programInstance.ProcessCommand(command);
                                DisplayCurrentChamber();
                            }
                        }
                        isCommandInputMode = false; // Exit command input mode after processing the command
                    }
                    else
                    {
                        Thread.Sleep(sleepTime); // Adjust the sleep time as needed
                    }
                }
            }
            foreach (var chamber in chambers)
            {
                SaveMap(chamber);
            }
            Console.Clear();
            // Ensure the update thread stops when the simulation ends
            updateThread.Join();
            keyListenerThread.Join();
            weatherThread.Join();
            guiThread.Join();
        }
        #region map saving
        // Compress JSON string to GZip
        private static byte[] CompressString(string json)
        {
            using var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            using var outputStream = new MemoryStream();
            using (var gzip = new GZipStream(outputStream, CompressionLevel.Optimal))
            {
                inputStream.CopyTo(gzip);
            }
            return outputStream.ToArray();
        }

        // Decompress GZip back to JSON string
        private static string DecompressToString(byte[] compressed)
        {
            using var inputStream = new MemoryStream(compressed);
            using var gzip = new GZipStream(inputStream, CompressionMode.Decompress);
            using var reader = new StreamReader(gzip, Encoding.UTF8);
            return reader.ReadToEnd();
        }
        public class Char2DArrayJsonConverter : JsonConverter<char[,]>
        {
            public override char[,] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
            var lines = JsonSerializer.Deserialize<List<string>>(ref reader, options) ?? new();
            if (lines.Count == 0) return new char[0, 0];

            int height = lines.Count;
            int width = lines[0].Length;
            var result = new char[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                result[x, y] = lines[y][x];
                }
            }
            return result;
            }
            public override void Write(Utf8JsonWriter writer, char[,] value, JsonSerializerOptions options)
            {
            int width = value.GetLength(0);
            int height = value.GetLength(1);
            var lines = new List<string>(height);

            for (int y = 0; y < height; y++)
            {
                var row = new char[width];
                for (int x = 0; x < width; x++)
                {
                row[x] = value[x, y];
                }
                lines.Add(new string(row));
            }
            JsonSerializer.Serialize(writer, lines, new JsonSerializerOptions(options)
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            }
        }
        public class Bool2DArrayJsonConverter : JsonConverter<bool[,]>
        {
            public override bool[,] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
            var lines = JsonSerializer.Deserialize<List<string>>(ref reader, options) ?? new();
            if (lines.Count == 0) return new bool[0, 0];

            int height = lines.Count;
            int width = lines[0].Length;
            var result = new bool[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                result[x, y] = (lines[y][x] == '1');
                }
            }
            return result;
            }
            public override void Write(Utf8JsonWriter writer, bool[,] value, JsonSerializerOptions options)
            {
            int width = value.GetLength(0);
            int height = value.GetLength(1);
            var lines = new List<string>(height);

            for (int y = 0; y < height; y++)
            {
                var row = new char[width];
                for (int x = 0; x < width; x++)
                {
                row[x] = value[x, y] ? '1' : '0';
                }
                lines.Add(new string(row));
            }
            JsonSerializer.Serialize(writer, lines, options);
            }
        }
        public class Int2DArrayJsonConverter : JsonConverter<int[,]>
        {
            public override int[,] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
            var lines = JsonSerializer.Deserialize<List<string>>(ref reader, options) ?? new();
            if (lines.Count == 0) return new int[0, 0];

            int height = lines.Count;
            var splittedLines = new List<List<int>>();
            foreach (var line in lines)
            {
                var rowData = new List<int>();
                var parts = line.Split(new[] { '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
                parts = parts.Select(p => p.Trim())
                            .Where(p => !string.IsNullOrWhiteSpace(p))
                            .ToArray();
                foreach (var chunk in parts)
                {
                    rowData.Add(int.Parse(chunk));
                }
                splittedLines.Add(rowData);
            }

            int width = splittedLines.Max(r => r.Count);
            var result = new int[height, width];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < splittedLines[y].Count; x++)
                {
                    result[y, x] = splittedLines[y][x];
                }
            }
            return result;
            }
            public override void Write(Utf8JsonWriter writer, int[,] value, JsonSerializerOptions options)
            {
            int height = value.GetLength(0);
            int width = value.GetLength(1);
            var lines = new List<string>(height);

            for (int y = 0; y < height; y++)
            {
                var row = new StringBuilder();
                for (int x = 0; x < width; x++)
                {
                row.Append($"({value[y, x]})");
                }
                lines.Add(row.ToString());
            }
            JsonSerializer.Serialize(writer, lines, new JsonSerializerOptions(options)
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            }
        }
        public class BoolJsonConverter : JsonConverter<bool>
        {
            public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
            if (reader.TokenType == JsonTokenType.Number) return reader.GetInt32() != 0;
            throw new JsonException("Expected 0 or 1");
            }
            public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
            {
            writer.WriteNumberValue(value ? 1 : 0);
            }
        }
        public class ValueTupleIntKeyConverter<TValue> : JsonConverter<Dictionary<(int, int), TValue>>
        {
            public override Dictionary<(int, int), TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
            var dict = new Dictionary<(int, int), TValue>();
            var intermediate = JsonSerializer.Deserialize<Dictionary<string, TValue>>(ref reader, options);
            if (intermediate != null)
            {
                foreach (var kvp in intermediate)
                {
                var parts = kvp.Key.Split('|');
                var key = (int.Parse(parts[0]), int.Parse(parts[1]));
                dict[key] = kvp.Value;
                }
            }
            return dict;
            }
            public override void Write(Utf8JsonWriter writer, Dictionary<(int, int), TValue> value, JsonSerializerOptions options)
            {
            var intermediate = value.ToDictionary(k => $"{k.Key.Item1}|{k.Key.Item2}", v => v.Value);
            JsonSerializer.Serialize(writer, intermediate, options);
            }
        }
        public class ValueTupleIntDoubleKeyConverter : JsonConverter<Dictionary<(int, int), double>>
        {
            public override Dictionary<(int, int), double> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                var dict = new Dictionary<(int, int), double>();
                var intermediate = JsonSerializer.Deserialize<Dictionary<string, double>>(ref reader, options);
                if (intermediate != null)
                {
                    foreach (var kvp in intermediate) { var parts = kvp.Key.Split('|'); var key = (int.Parse(parts[0]), int.Parse(parts[1])); dict[key] = kvp.Value; }
                }
                return dict;
            }
            public override void Write(Utf8JsonWriter writer, Dictionary<(int, int), double> value, JsonSerializerOptions options)
            {
                var intermediate = value.ToDictionary(k => $"{k.Key.Item1}|{k.Key.Item2}", v => v.Value);
                JsonSerializer.Serialize(writer, intermediate, options);
            }
        }
        public class Double2DArrayJsonConverter : JsonConverter<double[,]>
        {
            public override double[,] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                var lines = JsonSerializer.Deserialize<List<string>>(ref reader, options) ?? new();
                if (lines.Count == 0) return new double[0, 0];

                int height = lines.Count;
                var splittedLines = new List<List<double>>();
                foreach (var line in lines)
                {
                    var rowData = new List<double>();
                    var parts = line.Split(new[] { '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
                    parts = parts.Select(p => p.Trim())
                                .Where(p => !string.IsNullOrWhiteSpace(p))
                                .ToArray();
                    foreach (var chunk in parts)
                    {
                        rowData.Add(double.Parse(chunk, CultureInfo.InvariantCulture));
                    }
                    splittedLines.Add(rowData);
                }

                int width = splittedLines.Max(r => r.Count);
                var result = new double[height, width];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < splittedLines[y].Count; x++)
                    {
                        result[y, x] = splittedLines[y][x];
                    }
                }
                return result;
            }
            public override void Write(Utf8JsonWriter writer, double[,] value, JsonSerializerOptions options)
            {
            int height = value.GetLength(0);
            int width = value.GetLength(1);
            var lines = new List<string>(height);

            for (int y = 0; y < height; y++)
            {
                var row = new StringBuilder();
                for (int x = 0; x < width; x++)
                {
                row.Append($"({value[y, x].ToString(CultureInfo.InvariantCulture)})");
                }
                lines.Add(row.ToString());
            }
            JsonSerializer.Serialize(writer, lines, new JsonSerializerOptions(options)
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            }
        }

        // Saving without compression
        public static void SaveMap(Map map)
        {
            var folderPath = Path.Combine(Environment.CurrentDirectory, "Saves");
            Directory.CreateDirectory(folderPath);

            string fileName = $"{map.conf.Name}.json";
            string fullPath = Path.Combine(folderPath, fileName);

            var options = new JsonSerializerOptions { WriteIndented = true, IncludeFields = true };
            options.Converters.Add(new Char2DArrayJsonConverter());
            options.Converters.Add(new Bool2DArrayJsonConverter());
            options.Converters.Add(new BoolJsonConverter());
            options.Converters.Add(new Int2DArrayJsonConverter());
            options.Converters.Add(new Double2DArrayJsonConverter());
            options.Converters.Add(new ValueTupleIntKeyConverter<int>());
            options.Converters.Add(new ValueTupleIntDoubleKeyConverter());

            string json = JsonSerializer.Serialize(map, options);
            File.WriteAllText(fullPath, json);
        }
        public static Map? LoadMap(string filePath)
        {
            var options = new JsonSerializerOptions { IncludeFields = true };
            options.Converters.Add(new Char2DArrayJsonConverter());
            options.Converters.Add(new Bool2DArrayJsonConverter());
            options.Converters.Add(new BoolJsonConverter());
            options.Converters.Add(new Int2DArrayJsonConverter());
            options.Converters.Add(new Double2DArrayJsonConverter());
            options.Converters.Add(new ValueTupleIntKeyConverter<int>());
            options.Converters.Add(new ValueTupleIntDoubleKeyConverter());

            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<Map>(json, options);
        }

        // Saving with compression
/*         public static void SaveMap(Map map)
        {
            var savePath = Path.Combine(Environment.CurrentDirectory, "Saves");
            Directory.CreateDirectory(savePath);
            var fileName = $"{map.conf.Name}.json.gz";
            var fullPath = Path.Combine(savePath, fileName);

            var options = new JsonSerializerOptions { IncludeFields = true };
            options.Converters.Add(new Char2DArrayJsonConverter());
            options.Converters.Add(new Bool2DArrayJsonConverter());
            options.Converters.Add(new BoolJsonConverter());
            options.Converters.Add(new Int2DArrayJsonConverter());
            options.Converters.Add(new Double2DArrayJsonConverter());
            options.Converters.Add(new ValueTupleIntKeyConverter<int>());
            options.Converters.Add(new ValueTupleIntDoubleKeyConverter());

            var json = JsonSerializer.Serialize(map, options);
            using var fileStream = File.Create(fullPath);
            using var gzip = new GZipStream(fileStream, CompressionMode.Compress);
            using var writer = new StreamWriter(gzip);
            writer.Write(json);
        }
        public static Map? LoadMap(string filePath)
        {
            var options = new JsonSerializerOptions { IncludeFields = true };
            options.Converters.Add(new Char2DArrayJsonConverter());
            options.Converters.Add(new Bool2DArrayJsonConverter());
            options.Converters.Add(new BoolJsonConverter());
            options.Converters.Add(new Int2DArrayJsonConverter());
            options.Converters.Add(new Double2DArrayJsonConverter());
            options.Converters.Add(new ValueTupleIntKeyConverter<int>());
            options.Converters.Add(new ValueTupleIntDoubleKeyConverter());

            using var fileStream = File.OpenRead(filePath);
            using var gzip = new GZipStream(fileStream, CompressionMode.Decompress);
            using var reader = new StreamReader(gzip);
            var json = reader.ReadToEnd();
            return JsonSerializer.Deserialize<Map>(json, options);
        }
 */
        public static void DeleteMap(string filePath)
        {
            File.Delete(filePath);
        }
        public static void LoadAllMapsFromFolder(string folderPath)
        {
            // Ensure folder exists
            if (!Directory.Exists(folderPath))
                return;

            // Find all .json map files
            foreach (var file in Directory.GetFiles(folderPath, "*.json"))
            {
                Map? loadedMap = LoadMap(file);
                if (loadedMap != null)
                {
                    allChambers.Add(loadedMap);
                }
            }
        }
        #endregion
        public static int ConvertStringToNumbers(string input)
        {
            if (string.IsNullOrEmpty(input))
                return new Random().Next();

            int hash = 17;
            foreach (char c in input)
            {
                hash = hash * 31 + c;
            }

            return hash;
        }
        public static void DisplayCenteredText(string text)
        {
            var lines = text.Split('\n');
            int consoleWidth = Console.WindowWidth;
            int consoleHeight = Console.WindowHeight;
            int startY = (consoleHeight / 2) - (lines.Length / 2);

            for (int i = 0; i < lines.Length; i++)
            {
                int startX = (consoleWidth / 2) - (lines[i].Length / 2);
                Console.SetCursorPosition(startX, startY + i);
                Console.WriteLine(lines[i]);
            }
        }
        #region WinApi functions
        // Import necessary WinAPI functions
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetConsoleMode(IntPtr hConsoleHandle, out int lpMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetConsoleMode(IntPtr hConsoleHandle, int dwMode);

        const int ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

        static void EnableVirtualTerminalProcessing()
        {
            if (isLinux)
            {
                // No need to enable anything on Linux, ANSI escape codes work by default
                return;
            }

            IntPtr handle = System.Diagnostics.Process.GetCurrentProcess().Handle;
            if (GetConsoleMode(handle, out int mode))
            {
                mode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;
                SetConsoleMode(handle, mode);
            }
        }
        #endregion
        public static readonly List<string> Commands = new List<string>
                {
                    "help",
                    "exit",
                    "addchamber",
                    "removechamber",
                    "regenerate",
                    "chamber",
                    "run",
                    "seed",
                    "avaragetemp",
                    "avaragehum"
                };
        public static int sleepTime = 100;
        public int maxSleepTime = 3000;
        public int minSleepTime = 10;
        private void ListenForKeyPress()
        {
            while (continueSimulating)
            {
                while (!isConfiguring)
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true).Key;
                        if ((key == ConsoleKey.P || key == ConsoleKey.Spacebar) && !isCommandInputMode)
                        {
                            IsHumidityRendering = false;
                            IsTemperatureRendering = false;
                            isUpdating = !isUpdating;
                        }
                        else if (key == ConsoleKey.C && !isUpdating)
                        {
                            isCommandInputMode = !isCommandInputMode;
                        }
                        else if (key == ConsoleKey.R)
                        {
                            chambers[currentChamberIndex].Generate();
                            UpdateChamberStats();
                            DisplayCurrentChamber();
                        }
                        else if ((key == ConsoleKey.Q) && !isUpdating)
                        {
                            isCloudsRendering = !isCloudsRendering;
                            UpdateChamberStats();
                            DisplayCurrentChamber();
                        }
                        else if (key == ConsoleKey.LeftArrow && chambers.Count > 1 && currentChamberIndex > 0 && !isUpdating)
                        {
                            currentChamberIndex--;
                            UpdateChamberStats();
                            DisplayCurrentChamber();
                        }
                        else if (key == ConsoleKey.RightArrow && chambers.Count > 1 && currentChamberIndex < chambers.Count - 1 && !isUpdating)
                        {
                            currentChamberIndex++;
                            UpdateChamberStats();
                            DisplayCurrentChamber();
                        }
                        else if (key == ConsoleKey.DownArrow && chambers.Count > 1 && currentChamberIndex != 0 && !isUpdating)
                        {
                            currentChamberIndex = 0;
                            UpdateChamberStats();
                            DisplayCurrentChamber();
                        }
                        else if (key == ConsoleKey.UpArrow && chambers.Count > 1 && currentChamberIndex != chambers.Count - 1 && !isUpdating)
                        {
                            currentChamberIndex = chambers.Count - 1;
                            UpdateChamberStats();
                            DisplayCurrentChamber();
                        }
                        else if ((key == ConsoleKey.D1 || key == ConsoleKey.NumPad1) && chambers.Count > 0 && currentChamberIndex != 0 && !isUpdating)
                        {
                            currentChamberIndex = 0;
                            UpdateChamberStats();
                            DisplayCurrentChamber();
                        }
                        else if ((key == ConsoleKey.D2 || key == ConsoleKey.NumPad2) && chambers.Count > 1 && currentChamberIndex != 1 && !isUpdating)
                        {
                            currentChamberIndex = 1;
                            UpdateChamberStats();
                            DisplayCurrentChamber();
                        }
                        else if ((key == ConsoleKey.D3 || key == ConsoleKey.NumPad3) && chambers.Count > 2 && currentChamberIndex != 2 && !isUpdating)
                        {
                            currentChamberIndex = 2;
                            UpdateChamberStats();
                            DisplayCurrentChamber();
                        }
                        else if ((key == ConsoleKey.D4 || key == ConsoleKey.NumPad4) && chambers.Count > 3 && currentChamberIndex != 3 && !isUpdating)
                        {
                            currentChamberIndex = 3;
                            UpdateChamberStats();
                            DisplayCurrentChamber();
                        }
                        else if ((key == ConsoleKey.D5 || key == ConsoleKey.NumPad5) && chambers.Count > 4 && currentChamberIndex != 4 && !isUpdating)
                        {
                            currentChamberIndex = 4;
                            UpdateChamberStats();
                            DisplayCurrentChamber();
                        }
                        else if ((key == ConsoleKey.D6 || key == ConsoleKey.NumPad6) && chambers.Count > 5 && currentChamberIndex != 5 && !isUpdating)
                        {
                            currentChamberIndex = 5;
                            UpdateChamberStats();
                            DisplayCurrentChamber();
                        }
                        else if ((key == ConsoleKey.D7 || key == ConsoleKey.NumPad7) && chambers.Count > 6 && currentChamberIndex != 6 && !isUpdating)
                        {
                            currentChamberIndex = 6;
                            UpdateChamberStats();
                            DisplayCurrentChamber();
                        }
                        else if ((key == ConsoleKey.D8 || key == ConsoleKey.NumPad8) && chambers.Count > 7 && currentChamberIndex != 7 && !isUpdating)
                        {
                            currentChamberIndex = 7;
                            UpdateChamberStats();
                            DisplayCurrentChamber();
                        }
                        else if ((key == ConsoleKey.D9 || key == ConsoleKey.NumPad9) && chambers.Count > 8 && currentChamberIndex != 8 && !isUpdating)
                        {
                            currentChamberIndex = 8;
                            UpdateChamberStats();
                            DisplayCurrentChamber();
                        }
                        else if ((key == ConsoleKey.D0 || key == ConsoleKey.NumPad0) && chambers.Count > 9 && currentChamberIndex != 9 && !isUpdating)
                        {
                            currentChamberIndex = 9;
                            UpdateChamberStats();
                            DisplayCurrentChamber();
                        }
                        else if (key == ConsoleKey.PageDown)
                        {
                            if (sleepTime < maxSleepTime)
                            {
                                sleepTime += 10;
                            }
                        }
                        else if (key == ConsoleKey.PageUp)
                        {
                            if (sleepTime > minSleepTime)
                            {
                                sleepTime -= 10;
                            }
                        }
                        else if (key == ConsoleKey.T && !isUpdating && !IsHumidityRendering)
                        {
                            if (IsTemperatureRendering) isCloudsShadowsRendering = !isCloudsShadowsRendering;
                            IsHumidityRendering = false;
                            if (!IsTemperatureRendering) chambers[currentChamberIndex].RenderTemperatureNoise();
                            else chambers[currentChamberIndex].DisplayMap();
                            IsTemperatureRendering = !IsTemperatureRendering;
                        }
                        else if (key == ConsoleKey.H && !isUpdating && !IsTemperatureRendering)
                        {
                            if (!IsHumidityRendering) isCloudsShadowsRendering = !isCloudsShadowsRendering;
                            IsTemperatureRendering = false;
                            if (!IsHumidityRendering) chambers[currentChamberIndex].RenderHumidityNoise();
                            else chambers[currentChamberIndex].DisplayMap();
                            IsHumidityRendering = !IsHumidityRendering;
                        }
                    }
                    Thread.Sleep(sleepTime); // Adjust the sleep time as needed
                }
            }
        }
        private void UpdateMaps()
        {
            while (continueSimulating)
            {
                while (!isConfiguring)
                {
                    lock (mapLock)
                    {
                        if (isUpdating)
                        {
                            foreach (var chamber in chambers)
                            {
                                chamber.Update(); // Call the Update function to modify mapData and overlayData if needed
                                for (int y = 0; y < chambers[currentChamberIndex].height; y++)
                                {
                                    for (int x = 0; x < chambers[currentChamberIndex].width; x++)
                                    {
                                        if (chambers[currentChamberIndex].HasTileChanged(x, y))
                                        {
                                            chambers[currentChamberIndex].UpdateTile(x, y);
                                        }
                                        if (chambers[currentChamberIndex].HasOverlayTileChanged(x, y))
                                        {
                                            chambers[currentChamberIndex].UpdateOverlayTile(x, y);
                                        }
                                    }
                                }
                                chamber.UpdatePreviousMapData();
                                chamber.UpdatePreviousOverlayData();
                                Console.SetCursorPosition(GUIConfig.LeftPadding, config.Height + GUIConfig.TopPadding - 1);
                            }
                        }
                        Thread.Sleep(sleepTime); // Adjust the sleep time as needed
                    }
                }
            }
        }
        private void UpdateWeather()
        {
            while (continueSimulating)
            {
                while (!isConfiguring)
                {
                    lock (mapLock)
                    {  
                        if (isUpdating)
                        {
                            var currentMap = chambers[currentChamberIndex];
                            currentMap.AnimateWater();
                            currentMap.DisplayDayNightTransition();
                            foreach (var chamber in chambers)
                            {
                                chamber.UpdateClouds();
                            }
                            if (currentMap.isCloudsShadowsRendering) 
                                currentMap.DisplayCloudShadows();
                            if (currentMap.isCloudsRendering)
                            {
                                currentMap.RenderClouds(); // Ensure this is called correctly
                            }
                        }
                        Thread.Sleep(sleepTime); // Adjust the sleep time as needed
                    }
                }
            }
        }
        private void UpdateGUI()
        {
            while (continueSimulating)
            {
                while (!isConfiguring)
                {
                    lock (mapLock)
                    {
                        if (isUpdating)
                        {
                            chambers[currentChamberIndex].UpdateGUIValues();
                            Console.SetCursorPosition(GUIConfig.LeftPadding, config.Height + GUIConfig.TopPadding - 1);
                        }
                        Thread.Sleep(sleepTime); // Adjust the sleep time as needed
                    }
                }
            }
        }
        public static void DisplayCurrentChamber()
        {
            chambers[currentChamberIndex].DisplayMap();
        }
        private static string ReadCommandWithAutocomplete()
        {
            StringBuilder input = new StringBuilder();
            int cursorPosition = 0;

            while (true)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);

                if (keyInfo.Key == ConsoleKey.Enter)
                {
                    return input.ToString();
                }
                else if (keyInfo.Key == ConsoleKey.Backspace)
                {
                    if (cursorPosition > 0)
                    {
                        input.Remove(cursorPosition - 1, 1);
                        cursorPosition--;
                        Console.Write("\b \b");
                    }
                }
                else if (keyInfo.Key == ConsoleKey.Tab)
                {
                    string prefix = input.ToString();
                    string? suggestion = Commands.FirstOrDefault(cmd => cmd.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
                    if (suggestion != null)
                    {
                        // Clear the current input
                        Console.Write(new string('\b', cursorPosition) + new string(' ', cursorPosition) + new string('\b', cursorPosition));
                        input.Clear();
                        input.Append(suggestion);
                        cursorPosition = suggestion.Length;
                        Console.Write(suggestion);
                    }
                }
                else
                {
                    input.Insert(cursorPosition, keyInfo.KeyChar);
                    cursorPosition++;
                    Console.Write(keyInfo.KeyChar);
                }
            }
        }
        public bool ProcessCommand(string command)
        {
            string[] tokens = command.Split(' ');
            switch (tokens[0])
            {
            case "chamber":
                if (tokens.Length > 1 && int.TryParse(tokens[1], out int chamberIndex))
                {
                chamberIndex--; // Adjust for 1-based index
                if (chamberIndex >= 0 && chamberIndex < chambers.Count)
                {
                    Program.currentChamberIndex = chamberIndex;
                }
                else
                {
                    outputBuffer.Add("Invalid chamber index");
                }
                }
                else
                {
                outputBuffer.Add("Please specify the chamber index");
                }
                break;
            case "exit":
                return false;
            case "addchamber":
                if (tokens.Length > 1 && int.TryParse(tokens[1], out int numberOfChambers))
                {
                for (int i = 0; i < numberOfChambers; i++)
                {
                    isConfiguring = true;
                    seed = rng.Next();
                    Map newChamber = new Map();
                    isConfiguring = newChamber.GetConfig();
                    DisplayCenteredText(asciiArt);
                    newChamber.Generate();
                    chambers.Add(newChamber);
                    outputBuffer.Add("Added a new chamber");
                }
                }
                else
                {
                isConfiguring = true;
                seed = rng.Next();
                Map newChamber = new Map();
                isConfiguring = newChamber.GetConfig();
                DisplayCenteredText(asciiArt);
                newChamber.Generate();
                chambers.Add(newChamber);
                outputBuffer.Add("Added a new chamber");
                }
                break;
            case "removechamber":
                if (tokens.Length > 1 && int.TryParse(tokens[1], out chamberIndex))
                {
                chamberIndex--; // Adjust for 1-based index
                if (chamberIndex >= 0 && chamberIndex < chambers.Count)
                {
                    chambers.RemoveAt(chamberIndex);
                    outputBuffer.Add($"Removed chamber {chamberIndex + 1}");
                    if (chamberIndex == currentChamberIndex)
                    {
                    currentChamberIndex = Math.Max(0, chamberIndex - 1);
                    }
                    else if (chamberIndex < currentChamberIndex)
                    {
                    currentChamberIndex--;
                    }
                }
                else
                {
                    outputBuffer.Add("Invalid chamber index");
                }
                }
                else
                {
                outputBuffer.Add("Please specify the chamber index to remove");
                }
                break;
            case "regenerate":
                if (tokens.Length > 1 && int.TryParse(tokens[1], out chamberIndex))
                {
                chamberIndex--; // Adjust for 1-based index
                if (chamberIndex >= 0 && chamberIndex < chambers.Count)
                {
                    chambers[chamberIndex].Generate();
                    outputBuffer.Add($"Regenerated chamber {chamberIndex + 1}");
                }
                else
                {
                    outputBuffer.Add("Invalid chamber index");
                }
                }
                else
                {
                outputBuffer.Add("Please specify the chamber index to regenerate");
                }
                break;
            case "run":
                if (tokens.Length > 1)
                {
                // Extract method name and parameters
                string fullCommand = string.Join(" ", tokens.Skip(1));
                int methodStart = fullCommand.IndexOf('(');
                string methodName = methodStart == -1 ? fullCommand : fullCommand.Substring(0, methodStart).Trim();

                // Parse parameters if they exist
                object[] parameters = new object[0];
                if (methodStart != -1)
                {
                    int methodEnd = fullCommand.LastIndexOf(')');
                    if (methodEnd != -1)
                    {
                    string paramString = fullCommand.Substring(methodStart + 1, methodEnd - methodStart - 1);
                    parameters = paramString.Split(',')
                        .Where(p => !string.IsNullOrWhiteSpace(p))
                        .Select(p => Convert.ChangeType(p.Trim(), typeof(int)))
                        .ToArray();
                    }
                }

                try
                {
                    // Get method info with exact parameter count match
                    var method = typeof(Program).GetMethod(methodName,
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance,
                    null,
                    CallingConventions.Any,
                    parameters.Select(p => p.GetType()).ToArray(),
                    null);

                    if (method != null)
                    {
                    method.Invoke(this, parameters);
                    }
                    else
                    {
                    outputBuffer.Add($"Method '{methodName}' not found");
                    }
                }
                catch (Exception ex)
                {
                    outputBuffer.Add($"Error executing method '{methodName}': {ex.Message}");
                }
                }
                else
                {
                outputBuffer.Add("Please specify the method to run");
                }
                break;
            case "seed":
                outputBuffer.Add($"Current seed: {chambers[currentChamberIndex].conf.Seed}");
                break;
            case "avaragetemp":
                outputBuffer.Add($"Average temperature: {Math.Round(chambers[currentChamberIndex].avarageTempature, 2)}");
                break;
            case "avaragehum":
                outputBuffer.Add($"Average humidity: {Math.Round(chambers[currentChamberIndex].avarageHumidity, 2)}");
                break;
            default:
                outputBuffer.Add("Invalid command");
                break;
            }
            chambers[currentChamberIndex].DisplayMap();
            return true;
        }
        public static string GetCommand()
        {
            Console.Write(">> ");
            string? input = Console.ReadLine();
            if (input == null)
            {
                // Handle the case when the user cancels the input
                // For example, you can return an empty string or a default value
                return string.Empty;
            }
            return input;
        }
        public void UpdateChamberStats()
        {
            chambers[currentChamberIndex].isCloudsRendering = isCloudsRendering;
            chambers[currentChamberIndex].isCloudsShadowsRendering = isCloudsShadowsRendering;
            Map.outputBuffer.AddRange(outputBuffer);
            outputBuffer.Clear();
            continueSimulating = chambers[currentChamberIndex].shouldSimulationContinue;
            chambers[currentChamberIndex].actualOutputBuffer = Map.outputBuffer;
        }
        #region run functions
        public void SpawnMoreTurtles(int count)
        {
            chambers[currentChamberIndex].InitializeSpecies(count - 1, count, new Turtle(0, 0, 0, 0, chambers[currentChamberIndex].mapData, chambers[currentChamberIndex].overlayData, config.Height, config.Width, seed));
        }
        public void SpawnCloud(int x, int y, CloudType type)
        {
            chambers[currentChamberIndex].SpawnCloud(x, y, type);
        }
        public void Malware()
        {
            isUpdating = true;
            isCloudsRendering = false;
            isCloudsShadowsRendering = false;
            chambers[currentChamberIndex].DrawSkull();
            chambers[currentChamberIndex].SkullEatsMap();
            int hehe = int.Parse("hehe");
            outputBuffer.Add("Malware executed.");
            continueSimulating = false;
        }
        #endregion
        #region save selection GUI
        static (int x, int y) terminalCentre = (Console.WindowWidth / 2, Console.WindowHeight / 2);
        static int menuWidth = 95;
        static int menuHeight = 50;
        static int numberOfRows = 8;
        static int heightOffset = (Console.WindowHeight - (13 + (6 * numberOfRows))) / 4;
        static string delete = @"
.__@@__.
 \##$$/ 
 /@$$$\ ";
        static string select = @"
 $%\
 ##$%}
 $%/";
        static List<(int r, int g, int b)> colors = new List<(int r, int g, int b)>
        {
            ColorSpectrum.ANTIQUE_WHITE,
            ColorSpectrum.BEIGE
        };
        public static void DrawSaveSelectionGUI()
        {
            Console.Clear();
            var folderPath = Path.Combine(Environment.CurrentDirectory, "Saves");
            Directory.CreateDirectory(folderPath);
            Map.DrawColoredBox(terminalCentre.x - menuWidth / 2, terminalCentre.y - menuHeight / 2 + heightOffset, menuWidth, 10, "", ColorSpectrum.LIGHT_CYAN);
            Map.DisplayCenteredTextAtCords(Map.title, terminalCentre.x, terminalCentre.y - menuHeight / 2 + heightOffset + 5, ColorSpectrum.CYAN);
            string[] files = Directory.GetFiles(Path.Combine(Environment.CurrentDirectory, "Saves"), "*.json")
                .Select(f => Path.GetFileName(f))
                .ToArray();
            
            for (int i = 0; i < numberOfRows; i++)
            {
                int index = colors.Count > i ? i : i % colors.Count;
                DrawSaveFileBox(i, colors[index], false,  i == 0 ? 1 : 4);
            }
            DrawLoadButton(false);
            ManageSaveSlots();
        }
        public static void DrawSaveFileBox(int index, (int r, int g, int b) color, bool isSelected, int currentSelection, List<string>? typedLettersBuffer = null)
        {
            int x = terminalCentre.x - menuWidth / 2;
            int y = terminalCentre.y - menuHeight / 2 + heightOffset + 9 + index * 5 + 1;

            bool isBox0Selected = currentSelection == 1 || isSelected;
            bool isBox1Selected = currentSelection == 0 || isSelected;
            bool isBox2Selected = currentSelection == 2 || isSelected;
            if (currentSelection == 4)
            {
                isBox0Selected = false;
                isBox1Selected = false;
                isBox2Selected = false;
            }

            DrawSelectableBox(x + 11, y, menuWidth - 22, 5, isBox0Selected, isSelected, color);
            DrawSelectableBox(x, y, 10, 5, isBox1Selected, isSelected, ColorSpectrum.YELLOW);
            DrawSelectableBox(x + menuWidth - 10, y, 10, 5, isBox2Selected, isSelected, ColorSpectrum.LIGHT_GREEN);
            Console.ResetColor();

            // Draw the delete ASCII in the first small box
            var deleteLines = delete.Split('\n');
            for (int i = 0; i < deleteLines.Length; i++)
            {
            Console.SetCursorPosition(x + 1, y + i);
            Console.Write(Map.SetForegroundColor(ColorSpectrum.YELLOW.r, ColorSpectrum.YELLOW.g, ColorSpectrum.YELLOW.b) + deleteLines[i] + Map.ResetColor());
            }

            // Draw the select ASCII in the second small box
            var selectLines = select.Split('\n');
            for (int i = 0; i < selectLines.Length; i++)
            {
            Console.SetCursorPosition(x + menuWidth - 8, y + i);
            Console.Write(Map.SetForegroundColor(ColorSpectrum.LIGHT_GREEN.r, ColorSpectrum.LIGHT_GREEN.g, ColorSpectrum.LIGHT_GREEN.b) + selectLines[i] + Map.ResetColor());
            }
            if (typedLettersBuffer != null)
            {
                DisplayCustomLetters(index, typedLettersBuffer);
            }
            else if (slots[index].name != null)
            {
                DisplayCustomLetters(index, ConvertStringToList(slots[index].name ?? ""));
            }
            if (currentSelection == 5)
            {
                for (int i = 0; i < 3; i++)
                {
                    Console.SetCursorPosition(x + 11, y++ + i);
                    Console.Write(new string(' ', menuWidth - 24));
                }
            }

        }
        private static List<string> ConvertStringToList(string str)
        {
            List<string> list = new List<string>();
            foreach (char c in str)
            {
                if (c == ' ')
                {
                    list.Add("Spacebar");
                }
                else if (char.IsDigit(c))
                {
                    list.Add($"D{c}");
                }
                else
                {
                    list.Add(char.ToUpper(c).ToString());
                }
            }
            return list;
        }
        public static void ManageSaveSlots()
        {
            int currentIndex = 0; // Index of the currently selected slot
            int currentSection = 1; // 0=delete, 1=main, 2=select 4=none 5=none
            bool isTyping = false;
            bool isLoad = false;
            bool shouldMenu = true;
            List<string> typedLettersBuffer = new List<string>();
            string previousBuffer = "";

            void RedrawSaveUI(bool isSelected, int currentSelection, List<string>? typedLettersBuffer = null)
            {
                int index = colors.Count > currentIndex ? currentIndex : currentIndex % colors.Count;
                if (!isLoad && typedLettersBuffer != null) DrawSaveFileBox(currentIndex, colors[index], isSelected, currentSelection, typedLettersBuffer);
                else if (!isLoad) DrawSaveFileBox(currentIndex, colors[index], isSelected, currentSelection);
                else DrawSaveFileBox(currentIndex, colors[index], isSelected, 4);
            }

            while (shouldMenu)
            {
                while (!isConfiguring)
                {
                    var key = Console.ReadKey(true).Key;
                    if (isTyping)
                    {

                        switch (key)
                        {
                            case ConsoleKey.Enter:
                                string name = "";
                                bool nonSpaceFound = false;
                                foreach (var letter in typedLettersBuffer)
                                {
                                    switch (letter)
                                    {
                                        case "Spacebar":
                                            name += " ";
                                            break;
                                        case "D0":
                                            name += "0";
                                            nonSpaceFound = true;
                                            break;
                                        case "D1":
                                            name += "1";
                                            nonSpaceFound = true;
                                            break;
                                        case "D2":
                                            name += "2";
                                            nonSpaceFound = true;
                                            break;
                                        case "D3":
                                            name += "3";
                                            nonSpaceFound = true;
                                            break;
                                        case "D4":
                                            name += "4";
                                            nonSpaceFound = true;
                                            break;
                                        case "D5":
                                            name += "5";
                                            nonSpaceFound = true;
                                            break;
                                        case "D6":
                                            name += "6";
                                            nonSpaceFound = true;
                                            break;
                                        case "D7":
                                            name += "7";
                                            nonSpaceFound = true;
                                            break;
                                        case "D8":
                                            name += "8";
                                            nonSpaceFound = true;
                                            break;
                                        case "D9":
                                            name += "9";
                                            nonSpaceFound = true;
                                            break;
                                        default:
                                            name += letter;
                                            nonSpaceFound = true;
                                            break;
                                    }
                                }
                                // Only exit typing mode if the name contains non-space characters.
                                if (!nonSpaceFound || string.IsNullOrWhiteSpace(name) || typedLettersBuffer.Count < 1)
                                {
                                    break;
                                }
                                isTyping = false;
                                string oldPath = Path.Combine("Saves", slots[currentIndex].name + ".json");
                                slots[currentIndex] = (slots[currentIndex].chamber, name, false, false, true);
                                slots[currentIndex].chamber.conf.Name = name;
                                typedLettersBuffer = new List<string>();
                                RedrawSaveUI(slots[currentIndex].isSelected, currentSection);
                                string newPath = Path.Combine("Saves", name + ".json");

                                if (File.Exists(oldPath))
                                {
                                    File.Move(oldPath, newPath);
                                }
                                break;
                            case ConsoleKey.Escape:
                                isTyping = false;
                                RedrawSaveUI(slots[currentIndex].isSelected, currentSection);
                                break;
                            default:
                                if (key == ConsoleKey.Backspace && typedLettersBuffer.Count > 0)
                                {
                                    typedLettersBuffer.RemoveAt(typedLettersBuffer.Count - 1);
                                    RedrawSaveUI(slots[currentIndex].isSelected, currentSection, typedLettersBuffer);
                                    int x = terminalCentre.x - menuWidth / 2;
                                    int y = terminalCentre.y - menuHeight / 2 + heightOffset + 9 + currentIndex * 5 + 2;
                                    DisplayCustomLetters(currentIndex, typedLettersBuffer);
                                }
                                else if (
                                    (
                                        (key == ConsoleKey.Spacebar && typedLettersBuffer.Count > 0) ||
                                        key == ConsoleKey.A || key == ConsoleKey.B || key == ConsoleKey.C || key == ConsoleKey.D ||
                                        key == ConsoleKey.E || key == ConsoleKey.F || key == ConsoleKey.G || key == ConsoleKey.H ||
                                        key == ConsoleKey.I || key == ConsoleKey.J || key == ConsoleKey.K || key == ConsoleKey.L ||
                                        key == ConsoleKey.M || key == ConsoleKey.N || key == ConsoleKey.O || key == ConsoleKey.P ||
                                        key == ConsoleKey.Q || key == ConsoleKey.R || key == ConsoleKey.S || key == ConsoleKey.T ||
                                        key == ConsoleKey.U || key == ConsoleKey.V || key == ConsoleKey.W || key == ConsoleKey.X ||
                                        key == ConsoleKey.Y || key == ConsoleKey.Z || key == ConsoleKey.D0 || key == ConsoleKey.D1 ||
                                        key == ConsoleKey.D2 || key == ConsoleKey.D3 || key == ConsoleKey.D4 || key == ConsoleKey.D5 ||
                                        key == ConsoleKey.D6 || key == ConsoleKey.D7 || key == ConsoleKey.D8 || key == ConsoleKey.D9
                                    )
                                    && GetTypedLettersListLenght(typedLettersBuffer)
                                        + GetLetter(ConvertConsoleKeyToLetter(key.ToString())).Split('\n')[0].Length + 1 < 67)
                                {
                                    typedLettersBuffer.Add(key.ToString());
                                    DisplayCustomLetters(currentIndex, typedLettersBuffer);
                                }
                                break;
                        }
                        continue;
                    }

                    switch (key)
                    {
                        case ConsoleKey.UpArrow:
                        case ConsoleKey.W:
                            if (slots[currentIndex].isSelected)
                            {
                                currentSection = 1;
                                RedrawSaveUI(slots[currentIndex].isSelected, currentSection);
                            }
                            if (currentIndex > 0 && !isLoad)
                            {
                                RedrawSaveUI(slots[currentIndex].isSelected, 4);
                                currentIndex--;
                                RedrawSaveUI(slots[currentIndex].isSelected, currentSection);
                            }
                            if (isLoad)
                            {
                                isLoad = false;
                                currentSection = 1;
                                RedrawSaveUI(slots[currentIndex].isSelected, currentSection);
                                DrawLoadButton(isLoad);
                            }
                            break;
                        case ConsoleKey.DownArrow:
                        case ConsoleKey.S:
                            if (slots[currentIndex].isSelected && !isLoad)
                            {
                                currentSection = 1;
                                RedrawSaveUI(slots[currentIndex].isSelected, currentSection);
                            }
                            if (currentIndex < slots.Count - 1 && !isLoad)
                            {
                                RedrawSaveUI(slots[currentIndex].isSelected, 4);
                                currentIndex++;
                                RedrawSaveUI(slots[currentIndex].isSelected, currentSection);
                            }
                            else if (!isLoad && currentSection == 1)
                            {
                                isLoad = true;
                                RedrawSaveUI(slots[currentIndex].isSelected, 4);
                                DrawLoadButton(isLoad);
                            }
                            break;
                        case ConsoleKey.LeftArrow:
                        case ConsoleKey.A:
                            if (!isLoad)
                            {
                                if (currentSection > 0)
                                {
                                    RedrawSaveUI(slots[currentIndex].isSelected, 4);
                                    currentSection--;
                                }
                                else
                                {
                                    RedrawSaveUI(slots[currentIndex].isSelected, 4);
                                    currentSection = 1; // Move to Delete if possible
                                }
                                RedrawSaveUI(slots[currentIndex].isSelected, currentSection);
                            }
                            break;
                        case ConsoleKey.RightArrow:
                        case ConsoleKey.D:
                            if (!isLoad)
                            {
                                if (currentSection < 2)
                                {
                                    RedrawSaveUI(slots[currentIndex].isSelected, 4);
                                    currentSection++;
                                }
                                RedrawSaveUI(slots[currentIndex].isSelected, currentSection);
                            }
                            break;
                        case ConsoleKey.Enter:
                            bool canLoadAnything = false;
                            foreach (var slot in slots)
                            {
                                if (slot.isSelected)
                                {
                                    canLoadAnything = true;
                                    break;
                                }
                            }
                            if (isLoad && canLoadAnything)
                            {
                                LoadSelectedSlots();
                                shouldMenu = false;
                                return;
                            }
                            else if (slots[currentIndex].isSelected)
                            {
                                var slot = slots[currentIndex];
                                slot.isSelected = false;
                                slots[currentIndex] = slot;
                            }
                            else if (currentSection == 1 && !isLoad)
                            {
                                typedLettersBuffer = ConvertStringToList(slots[currentIndex].name ?? "");
                                previousBuffer = slots[currentIndex].name ?? "";
                                isTyping = true;
                            }
                            else if (currentSection == 0)
                            {
                                if (!slots[currentIndex].isEmpty || slots[currentIndex].name != null)
                                {
                                    DeleteMap(Path.Combine(Environment.CurrentDirectory, "Saves", slots[currentIndex].name ?? "") + ".json");
                                    slots[currentIndex] = (new Map(), null, false, false, true);
                                    RedrawSaveUI(slots[currentIndex].isSelected, 1);
                                    RedrawSaveUI(slots[currentIndex].isSelected, 2);
                                }
                            }
                            else if (currentSection == 2)
                            {
                                if (slots[currentIndex].chamber.seed == 0)
                                {
                                    slots[currentIndex] = AddNewChamber(slots[currentIndex]);
                                }
                                else
                                {
                                    var slot = slots[currentIndex];
                                    slot.isSelected = !slot.isSelected;
                                    slots[currentIndex] = slot;
                                    RedrawSaveUI(slots[currentIndex].isSelected, 2);
                                }
                            }
                            RedrawSaveUI(slots[currentIndex].isSelected, currentSection);
                            break;
                    }
                    foreach (var slot in slots)
                    {
                        if (slot.isSelected)
                        {
                            int index = colors.Count > currentIndex ? currentIndex : currentIndex % colors.Count;
                            DrawSaveFileBox(slots.IndexOf(slot), colors[index], true, currentSection);
                        }
                    }
                }
            }
        }
        public static void DisplayCustomLetters(int currentIndex, List<string> buffer)
        {
            int x = terminalCentre.x - menuWidth / 2 + 13;
            int y = terminalCentre.y - menuHeight / 2 + heightOffset + 9 + currentIndex * 5 + 1;

            int currentX = x;

            foreach (var letter in buffer)
            {
                if (letter == "Spacebar" || letter == " ")
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Console.SetCursorPosition(currentX, y + 1 + i);
                        Console.Write(new string(' ', 3));
                    }
                    currentX += 4; // 5 spaces plus 1 for spacing between letters
                    continue;
                }

                string letterString = GetLetter(letter);
                var lines = letterString.Split('\n');

                int letterWidth = lines.Max(line => line.Length);

                for (int i = 0; i < lines.Length; i++)
                {
                    Console.SetCursorPosition(currentX, y + i);
                    Console.Write(lines[i]);
                }

                currentX += letterWidth + 1; // Add 1 for spacing between letters
            }
        }
        public static string GetLetter(string letter)
        {
            switch (letter)
            {
                case "A":
                    return Characters.A;
                case "B":
                    return Characters.B;
                case "C":
                    return Characters.C;
                case "D":
                    return Characters.D;
                case "E":
                    return Characters.E;
                case "F":
                    return Characters.F;
                case "G":
                    return Characters.G;
                case "H":
                    return Characters.H;
                case "I":
                    return Characters.I;
                case "J":
                    return Characters.J;
                case "K":
                    return Characters.K;
                case "L":
                    return Characters.L;
                case "M":
                    return Characters.M;
                case "N":
                    return Characters.N;
                case "O":
                    return Characters.O;
                case "P":
                    return Characters.P;
                case "Q":
                    return Characters.Q;
                case "R":
                    return Characters.R;
                case "S":
                    return Characters.S;
                case "T":
                    return Characters.T;
                case "U":
                    return Characters.U;
                case "V":
                    return Characters.V;
                case "W":
                    return Characters.W;
                case "X":
                    return Characters.X;
                case "Y":
                    return Characters.Y;
                case "Z":
                    return Characters.Z;
                case "D0":
                    return Characters.Zero;
                case "D1":
                    return Characters.One;
                case "D2":
                    return Characters.Two;
                case "D3":
                    return Characters.Three;
                case "D4":
                    return Characters.Four;
                case "D5":
                    return Characters.Five;
                case "D6":
                    return Characters.Six;
                case "D7":
                    return Characters.Seven;
                case "D8":
                    return Characters.Eight;
                case "D9":
                    return Characters.Nine;
                default:
                    return Characters.Unknown;
            }
        }
        public static string ConvertConsoleKeyToLetter(string key)
        {
            switch (key)
            {
                case "Spacebar":
                    return " ";
                case "D0":
                    return "0";
                case "D1":
                    return "1";
                case "D2":
                    return "2";
                case "D3":
                    return "3";
                case "D4":
                    return "4";
                case "D5":
                    return "5";
                case "D6":
                    return "6";
                case "D7":
                    return "7";
                case "D8":
                    return "8";
                case "D9":
                    return "9";
                default:
                    return key;
            }
        }
        public static int GetTypedLettersListLenght(List<string> list)
        {
            int length = 0;
            foreach (var letter in list)
            {
                if (letter == "Spacebar" || letter == " ")
                {
                    length += 4;
                }
                else
                {
                    string letterRepresentation = GetLetter(letter);
                    int letterWidth = letterRepresentation.Split('\n').Max(line => line.Length);
                    length += letterWidth + 1;
                }
            }
            return length;
        }
        public static void DrawLoadButton(bool isLoad)
        {
            DrawSelectableBox(terminalCentre.x - 5, terminalCentre.y - menuHeight / 2 + heightOffset + 9 + numberOfRows * 5 + 1, 10, 3, false, false, ColorSpectrum.LIGHT_GREEN);
            Console.SetCursorPosition(terminalCentre.x - 2, terminalCentre.y - menuHeight / 2 + heightOffset + 9 + numberOfRows * 5 + 2);
            Console.ResetColor();
            string foreground = isLoad ? Map.SetForegroundColor(ColorSpectrum.BLACK.r, ColorSpectrum.BLACK.g, ColorSpectrum.BLACK.b) : "";
            string background = isLoad ? Map.SetBackgroundColor(ColorSpectrum.SILVER.r, ColorSpectrum.SILVER.g, ColorSpectrum.SILVER.b) : "";
            Console.Write(background + foreground + "LOAD" + Map.ResetColor());
        }
        public static void DrawSelectableBox(int x, int y, int width, int height, bool isSelected, bool isFullySelected, (int r, int g, int b) color)
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

            // Define selection colors
            (int r, int g, int b) normalSelectionColor = ColorSpectrum.SILVER;
            (int r, int g, int b) selectedColor = ColorSpectrum.YELLOW;

            // Set background color based on selection
            string background;
            if (isFullySelected) background = Map.SetBackgroundColor(selectedColor.r, selectedColor.g, selectedColor.b);
            else if (isSelected) background = Map.SetBackgroundColor(normalSelectionColor.r, normalSelectionColor.g, normalSelectionColor.b);
            else background = "";

            // Draw top border with double lines
            Console.SetCursorPosition(x, y);
            if (!isLinux)
                Console.Write(background + Map.SetForegroundColor(color.r, color.g, color.b) + topLeft + new string(doubleHorizontal[0], width - 2) + topRight + Map.ResetColor());
            else
                Console.Write(background + Map.SetForegroundColor(color.r, color.g, color.b) + corner + new string(horizontal[0], width - 2) + corner + Map.ResetColor());

            // Draw sides and content area
            for (int i = 1; i < height - 1; i++)
            {
                Console.SetCursorPosition(x, y + i);
                if (!isLinux)
                {
                    Console.Write(
                        background +
                        Map.SetForegroundColor(color.r, color.g, color.b) + doubleVertical + Map.ResetColor() +
                        new string(' ', width - 2) +
                        background +
                        Map.SetForegroundColor(color.r, color.g, color.b) + doubleVertical + Map.ResetColor()
                    );
                }
                else
                {
                    Console.Write(
                        background +
                        Map.SetForegroundColor(color.r, color.g, color.b) + vertical + Map.ResetColor() +
                        new string(' ', width - 2) +
                        background +
                        Map.SetForegroundColor(color.r, color.g, color.b) + vertical + Map.ResetColor()
                    );
                }
            }

            // Draw bottom border with double lines
            Console.SetCursorPosition(x, y + height - 1);
            if (!isLinux)
                Console.Write(background + Map.SetForegroundColor(color.r, color.g, color.b) + bottomLeft + new string(doubleHorizontal[0], width - 2) + bottomRight + Map.ResetColor());
            else
                Console.Write(background + Map.SetForegroundColor(color.r, color.g, color.b) + corner + new string(horizontal[0], width - 2) + corner + Map.ResetColor());

            // Reset background color
            if (isSelected)
                Console.Write(Map.ResetColor());
        }
        public static (Map chamber, string? name, bool isSelected, bool isTyping, bool isEmpty) AddNewChamber((Map chamber, string? name, bool isSelected, bool isTyping, bool isEmpty) chamber)
        {
            isConfiguring = true;
            isConfiguring = chamber.chamber.GetConfig();
            DisplayCenteredText(asciiArt);
            bool shouldSave = chamber.chamber.conf.ShouldSave;
            
            // Use the provided name if available; otherwise use "NEW CHAMBER"
            string providedName = chamber.name ?? "";
            string baseName = string.IsNullOrWhiteSpace(providedName) ? "NEW CHAMBER" : providedName;
            string uniqueName = baseName;
            
            string savesFolder = Path.Combine(Environment.CurrentDirectory, "Saves");
            Directory.CreateDirectory(savesFolder);
            string fullPath = Path.Combine(savesFolder, baseName + ".json");

            if (File.Exists(fullPath))
            {
            int candidate = 1;
            while (File.Exists(Path.Combine(savesFolder, baseName + candidate.ToString() + ".json")))
            {
                candidate++;
            }
            uniqueName = baseName + candidate.ToString();
            }
            
            chamber.chamber.conf.Name = uniqueName;
            chamber.name = uniqueName;

            if (shouldSave)
            {
            chamber.chamber.Generate();
            SaveMap(chamber.chamber);
            }
            
            (Map chamber, string? name, bool isSelected, bool isTyping, bool isEmpty) newSlot = shouldSave
            ? (chamber.chamber, chamber.name, chamber.isSelected, chamber.isTyping, false)
            : (new Map(), null, false, false, true);
            Console.Clear();
            Map.DrawColoredBox(terminalCentre.x - menuWidth / 2, terminalCentre.y - menuHeight / 2 + heightOffset, menuWidth, 10, "", ColorSpectrum.LIGHT_CYAN);
            Map.DisplayCenteredTextAtCords(Map.title, terminalCentre.x, terminalCentre.y - menuHeight / 2 + heightOffset + 5, ColorSpectrum.CYAN);
            for (int i = 0; i < numberOfRows; i++)
            {
            int index = colors.Count > i ? i : i % colors.Count;
            DrawSaveFileBox(i, colors[index], false, 4);
            }
            DrawLoadButton(false);
            return newSlot;
        }
        public static void LoadSelectedSlots()
        {
            isConfiguring = false;
            foreach (var slot in slots)
            {
                if (slot.isSelected)
                {
                    chambers.Add(slot.chamber);
                }
            }
        }
        #endregion
    }
}