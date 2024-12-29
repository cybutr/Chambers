using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.IO;
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

#nullable enable
namespace Internal
{
    public class Program
    {
        private static bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        private static readonly object mapLock = new object();
        public static List<string> outputBuffer = new List<string>();
        public static List<string> eventBuffer = new List<string>();
        public static Config config = new Config(Console.WindowWidth / 2 - GUIConfig.LeftPadding - GUIConfig.RightPadding, Console.WindowHeight - GUIConfig.BottomPadding - GUIConfig.TopPadding, 
        10.0, 0.2, 12, 16, 7, 10, 0.4, 0.7, 0.4, 1.0, new Random().Next());
        public static void Main(string[] args)
        {
            EnableVirtualTerminalProcessing();
            currentChamberIndex = 0;
            Console.ResetColor();
            Console.Clear();

            eventBuffer.Add("None");
            Console.CursorVisible = false;

            // Loading screen
            string asciiArt = @"
                            _,.---._        ,---.                       .=-.-.  .-._               _,---.                     
            _.-.       ,-.' , -  `.    .--.'  \       _,..---._     /==/_ / /==/ \  .-._    _.='.'-,  \                       
            .-,.'|      /==/_,  ,  - \   \==\-/\ \    /==/,   -  \   |==|, |  |==|, \/ /, /  /==.'-     /                     
            |==|, |     |==|   .=.     |  /==/-|_\ |   |==|   _   _\  |==|  |  |==|-  \|  |  /==/ -   .-'                     
            |==|- |     |==|_ : ;=:  - |  \==\,   - \  |==|  .=.   |  |==|- |  |==| ,  | -|  |==|_   /_,-.                    
            |==|, |     |==| , '='     |  /==/ -   ,|  |==|,|   | -|  |==| ,|  |==| -   _ |  |==|  , \_.' )                   
            |==|- `-._   \==\ -    ,_ /  /==/-  /\ - \ |==|  '='   /  |==|- |  |==|  /\ , |  \==\-  ,    (   .=.   .=.   .=.  
            /==/ - , ,/   '.='. -   .'   \==\ _.\=\.-' |==|-,   _`/   /==/. /  /==/, | |- |   /==/ _  ,  /  :=; : :=; : :=; : 
            `--`-----'      `--`--''      `--`         `-.`.____.'    `--`-`   `--`./  `--`   `--`------'    `=`   `=`   `=` ";

            DisplayCenteredText(asciiArt);

            // Main Loop
            Program programInstance = new Program();
            Map chamber1 = new Map(config, new Random().Next());
            chamber1.Generate();
            chambers.Add(chamber1);
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
                        }
                        else if (command.ToLower().StartsWith("chamber "))
                        {
                            int chamberIndex;
                            if (int.TryParse(command.Split(' ')[1], out chamberIndex) && chamberIndex >= 0 && chamberIndex < chambers.Count)
                            {
                                currentChamberIndex = chamberIndex;
                                DisplayCurrentChamber();
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
                    Thread.Sleep(100); // Adjust the sleep time as needed
                }
            }

            // Ensure the update thread stops when the simulation ends
            updateThread.Join();
            keyListenerThread.Join();
            weatherThread.Join();
            guiThread.Join();
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
        #region map
        public static int seed = DateTime.Now.Millisecond;
        public static List<Map> chambers = new List<Map>();
        public static int currentChamberIndex;
        public static bool continueSimulating = true;
        public static bool isUpdating = false;
        public static bool isCommandInputMode = false;
        public static bool isCloudsRendering = false;
        public static bool isCloudsShadowsRendering = true;
        public static bool IsHumidityRendering = true;
        public static bool IsTemperatureRendering = true;

        #endregion
        public static readonly List<string> Commands = new List<string>
                {
                    "help",
                    "exit",
                    "addchamber",
                    "removechamber",
                    "regenerate",
                    "chamber",
                    "run"

                };
        public static int sleepTime = 100;
        public int maxSleepTime = 3000;
        public int minSleepTime = 10;
        private void ListenForKeyPress()
        {
            while (continueSimulating)
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
                    else if (key == ConsoleKey.T && !isUpdating)
                    {
                        isCloudsShadowsRendering = !isCloudsShadowsRendering;
                        IsHumidityRendering = false;
                        if (!IsTemperatureRendering) chambers[currentChamberIndex].RenderTemperatureNoise();
                        else chambers[currentChamberIndex].DisplayMap();
                        IsTemperatureRendering = !IsTemperatureRendering;
                    }
                    else if (key == ConsoleKey.H && !isUpdating)
                    {
                        isCloudsShadowsRendering = !isCloudsShadowsRendering;
                        IsTemperatureRendering = false;
                        if (!IsHumidityRendering) chambers[currentChamberIndex].RenderHumidityNoise();
                        else chambers[currentChamberIndex].DisplayMap();
                        IsHumidityRendering = !IsHumidityRendering;
                    }
                }
                Thread.Sleep(sleepTime); // Adjust the sleep time as needed
            }
        }
        private void UpdateMaps()
        {
            while (continueSimulating)
            {
                lock (mapLock)
                {
                    UpdateChamberStats();
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
                        UpdateChamberStats();
                    }
                    Thread.Sleep(sleepTime); // Adjust the sleep time as needed
                }
            }
        }
        private void UpdateWeather()
        {
            while (continueSimulating)
            {
                lock (mapLock)
                {  
                    if (isUpdating)
                    {
                        var currentMap = chambers[currentChamberIndex];
                        currentMap.AnimateWater();
                        if (currentMap.isCloudsShadowsRendering) 
                            currentMap.DisplayCloudShadows();
                        currentMap.DisplayDayNightTransition();
                        foreach (var chamber in chambers)
                        {
                            chamber.UpdateClouds();
                        }
                        if (currentMap.isCloudsRendering)
                        {
                            currentMap.RenderClouds(); // Ensure this is called correctly
                        }
                        // ...rest of update code...
                    }
                    Thread.Sleep(sleepTime); // Adjust the sleep time as needed
                }
            }
        }
        private void UpdateGUI()
        {
            while (continueSimulating)
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
                    if (tokens.Length > 1)
                    {
                        int chamberIndex = int.Parse(tokens[1]) - 1; // Subtract 1 to adjust for 1-based index
                        if (chamberIndex >= 0 && chamberIndex < chambers.Count)
                        {
                            Program.currentChamberIndex = chamberIndex;
                        }
                        else
                        {
                            outputBuffer.Add("Invalid chamber index.");
                        }
                    }
                    else
                    {
                        outputBuffer.Add("Please specify the chamber index.");
                    }
                    break;
                case "exit":
                    return false;
                case "addchamber":
                    if (tokens.Length > 1)
                    {
                        for (int i = 0; i < int.Parse(tokens[1]); i++)
                        {
                            Map newChamber = new Map(config, new Random().Next());
                            newChamber.Generate();
                            chambers.Add(newChamber);
                            outputBuffer.Add("Added a new chamber.");
                        }
                    }
                    else
                    {
                        Map newChamber = new Map(config, new Random().Next());
                        newChamber.Generate();
                        chambers.Add(newChamber);
                        outputBuffer.Add("Added a new chamber.");
                    }

                    break;
                case "removechamber":
                    if (tokens.Length > 1)
                    {
                        int chamberIndex = int.Parse(tokens[1]) - 1; // Subtract 1 to adjust for 1-based index
                        if (chamberIndex >= 0 && chamberIndex < chambers.Count)
                        {
                            chambers.RemoveAt(chamberIndex);
                            outputBuffer.Add($"Removed chamber {chamberIndex + 1}.");
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
                            outputBuffer.Add("Invalid chamber index.");
                        }
                    }
                    else
                    {
                        outputBuffer.Add("Please specify the chamber index to remove.");
                    }
                    break;
                case "regenerate":
                    if (tokens.Length > 1)
                    {
                        int chamberIndex = int.Parse(tokens[1]) - 1; // Subtract 1 to adjust for 1-based index
                        if (chamberIndex >= 0 && chamberIndex < chambers.Count)
                        {
                            chambers[chamberIndex].Generate();
                            outputBuffer.Add($"Regenerated chamber {chamberIndex}.");
                        }
                        else
                        {
                            outputBuffer.Add("Invalid chamber index.");
                        }
                    }
                    else
                    {
                        outputBuffer.Add("Please specify the chamber index to regenerate.");
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
                                outputBuffer.Add($"Method '{methodName}' not found.");
                            }
                        }
                        catch (Exception ex)
                        {
                            outputBuffer.Add($"Error executing method '{methodName}': {ex.Message}");
                        }
                    }
                    else
                    {
                        outputBuffer.Add("Please specify the method to run.");
                    }
                    break;
                default:
                    outputBuffer.Add("Invalid command. Seek help on the right side of the screen.");
                    break;
            }
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
            continueSimulating = chambers[currentChamberIndex].shouldSimulationContinue;
            Map.outputBuffer.AddRange(outputBuffer);
            outputBuffer.Clear();
            chambers[currentChamberIndex].actualOutputBuffer = Map.outputBuffer;
        }
        #region run functions
        public void SpawnMoreTurtles(int count)
        {
            chambers[currentChamberIndex].InitializeSpecies(count - 1, count, new Turtle(0, 0, 0, 0, chambers[currentChamberIndex].mapData, chambers[currentChamberIndex].overlayData, config.Height, config.Width));
        }
        public void SpawnCloud(int x, int y, Map.CloudType type)
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
    }
}