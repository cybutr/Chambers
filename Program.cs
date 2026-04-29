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
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Runtime.InteropServices;
using System.Reflection;
using Internal;

partial class Program
{
    #region map
    //public static int seed = DateTime.Now.Millisecond;
    private static readonly Random _seedRng = new Random();
    public static string seedString = Math.Round(_seedRng.Next() * ((_seedRng.NextDouble() - 0.5) * 2)).ToString();
    public static int seed = ConvertStringToNumbers(seedString);
    public Random rng = new Random(seed);
    public static List<Map> chambers = [];
    public static List<Map> allChambers = [];
    public static int currentChamberIndex;
    public static bool continueSimulating = true;
    public static bool isUpdating = false;
    public static bool isCommandInputMode = false;
    public static bool isCloudsRendering = false;
    public static bool isCloudsShadowsRendering = true;
    public static bool IsHumidityRendering = false;
    public static bool IsTemperatureRendering = false;
    public static bool isConfiguring = false;
    public static bool isMenu = true;
    public static bool autoResize = true;
    public static SerializationFormat SaveFormat = SerializationFormat.Binary;
    static KeybindRegistry keybinds = new();
    #endregion
    private static bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    private static readonly object mapLock = new object();
    private static int _lastConsoleW = 0;
    private static int _lastConsoleH = 0;
    private static volatile bool _renderPending;
    private static int _pendingPanDx = 0;
    private static int _pendingPanDy = 0;
    public static List<string> outputBuffer = [];
    public static List<string> eventBuffer = [];
    public static List<(Map chamber, string? name, bool isSelected, bool isTyping, bool isEmpty)> slots = [];
    public static Config config = new Config(Console.WindowWidth / 2 - GUIConfig.LeftPadding - GUIConfig.RightPadding, Console.WindowHeight - GUIConfig.BottomPadding - GUIConfig.TopPadding,
    10.0, seedString);
    public static bool displayGUI = true;
    static Thread updateThread = null!;
    static Thread keyListenerThread = null!;
    static Thread weatherThread = null!;
    static Thread guiThread = null!;
    // Loading screen
    public static string loadingAsciiArt = @"
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
        // Check for test mode
        if (args.Length > 0 && args[0] == "--test-sync")
        {
            TestSynchronizationIntegration();
            return;
        }
        else if (args.Length > 0 && args[0] == "--test-gui")
        {
            TestGUIFixes();
            return;
        }
        else if (args.Length > 0 && args[0] == "--test-config")
        {
            TestConfigGUI();
            return;
        }
        else if (args.Length > 0 && args[0] == "--testing")
        {
            Testing();
            return;
        }
        else if (args.Length > 0 && args[0] == "--camera-test")
        {
            TestCamera();
            return;
        }
        else if (args.Length > 0 && args[0] == "--gui-layout")
        {
            TestGUILayout();
            return;
        }
        EnableVirtualTerminalProcessing();
        RequestMinimumTerminalResize();
        _lastConsoleW = Console.WindowWidth;
        _lastConsoleH = Console.WindowHeight;
        currentChamberIndex = 0;
        GUI.Write(GUI.ResetColor());
        GUI.Clear();
        TryLoadUserConfig(config);
        LoadAllMapsFromFolder(Path.Combine(Environment.CurrentDirectory, "Data/Saves"));

        // Synchronize file names with config names at startup
        SynchronizeAllChamberFiles();

        eventBuffer.Add("None");
        GUI.SetCursorVisible(false);

        // Check console size with fallback for headless environments
        try
        {
            if (Console.WindowHeight < 55 || Console.WindowWidth < 100)
            {
                GUI.WriteLine("Note: Console window is smaller than recommended (100x60). Some UI elements may not display properly.");
                GUI.WriteLine("Current size: {0}x{1}", Console.WindowWidth, Console.WindowHeight);
                GUI.WriteLine("Press Enter to exit...");
                Console.ReadLine();
                return;
            }
        }
        catch
        {
            GUI.WriteLine("Running in headless mode, continuing with default settings...");
        }

        for (int i = 0; i < numberOfRows; i++)
        {
            // Use existing chamber from allChambers if available, otherwise mark as empty
            if (i < allChambers.Count)
            {
                var chamber = allChambers[i];
                // Ensure the chamber name is not null or empty
                string chamberName = string.IsNullOrWhiteSpace(chamber.conf.Name) ? "NEW CHAMBER" : chamber.conf.Name;
                chamber.conf.Name = chamberName; // Ensure consistency
                slots.Add((chamber, chamberName, false, false, false));
            }
            else slots.Add((new Map(), null, false, false, true));
        }
        // Main Loop
        Program programInstance = new Program();
        Random globalRandom = new Random(seed);
        Map chamber1 = new Map();
        GUI.Clear();
        DrawSaveSelectionGUI();
        GUI.Clear();
        if (chambers.Count > 0) RequestTerminalResize(chambers[currentChamberIndex]);
        DisplayCurrentChamber();
        GUI.Write(GUI.ResetColor());

        programInstance.RegisterKeybinds();

        // Start the map update thread
        updateThread = new Thread(() => programInstance.UpdateMaps());
        updateThread.Start();

        // Start the key listener thread
        keyListenerThread = new Thread(() => programInstance.ListenForKeyPress());
        keyListenerThread.Start();

        // Start the weather system thread
        weatherThread = new Thread(() => programInstance.UpdateWeather());
        weatherThread.Start();

        // Start the GUI update thread
        guiThread = new Thread(() => programInstance.UpdateGUI());
        guiThread.Start();
        while (continueSimulating)
        {
            while (!isConfiguring)
            {
                if (isCommandInputMode)
                {
                    GUI.Write(GUI.ResetColor()); // Reset color before reading commands
                    GUI.SetCursorPosition(0, chamber1.height + GUIConfig.TopPadding + 2);
                    GUI.Write(">> "); // Prompt for input
                    string? command = ReadCommandWithAutocomplete(); // Read input
                    if (command != null && !string.IsNullOrWhiteSpace(command))
                    {
                        if (command.ToLower() == "exit")
                        {
                            continueSimulating = false;
                            isConfiguring = true;
                            for (int i = 0; i < GUIConfig.BottomPadding; i++)
                            {
                                GUI.SetCursorPosition(0, Console.BufferHeight - i - 1);
                                GUI.Write(new string(' ', Console.BufferWidth));
                            }
                            GUI.SetCursorVisible(true);
                            break;
                        }
                        else if (command.ToLower().StartsWith("chamber "))
                        {
                            if (int.TryParse(command.Split(' ')[1], out int chamberIndex) && chamberIndex >= 0 && chamberIndex < chambers.Count)
                            {
                                SwitchToChamber(chamberIndex);
                                DisplayCurrentChamber();
                                for (int i = 0; i < GUIConfig.BottomPadding; i++)
                                {
                                    GUI.SetCursorPosition(0, Console.BufferHeight - i - 1);
                                    GUI.Write(new string(' ', Console.BufferWidth));
                                }
                            }
                        }
                        else
                        {
                            for (int i = 0; i < GUIConfig.BottomPadding; i++)
                            {
                                GUI.SetCursorPosition(0, Console.BufferHeight - i - 1);
                                GUI.Write(new string(' ', Console.BufferWidth));
                            }
                            continueSimulating = programInstance.ProcessCommand(command);
                            DisplayCurrentChamber();
                        }
                    }
                    // Clear the command prompt line
                    GUI.SetCursorPosition(0, chamber1.height + GUIConfig.TopPadding + 2);
                    GUI.Write(new string(' ', Console.BufferWidth));
                    isCommandInputMode = false; // Exit command input mode after processing the command
                    if (displayGUI) UpdateStaticChamberStats();
                    if (displayGUI)
                    {
                        var m = chambers[currentChamberIndex];
                        m.DisplayGUI();
                        m._guiBuf.Flush();
                    }
                }
                else Thread.Sleep(sleepTime); // Adjust the sleep time as neededed
            }
        }
        foreach (var chamber in chambers) SaveMap(chamber);
        GUI.Clear();
        updateThread.Join();
        keyListenerThread.Join();
        weatherThread.Join();
        guiThread.Join();
    }
    private static void OnExit(object? sender, ConsoleCancelEventArgs args)
    {
        if (isUpdating)
        {
            foreach (var chamber in chambers) SaveMap(chamber);
            continueSimulating = false;
            updateThread.Join();
            keyListenerThread.Join();
            weatherThread.Join();
            guiThread.Join();
            Environment.Exit(0);
        }
    }
    public static int ConvertStringToNumbers(string input)
    {
        if (string.IsNullOrEmpty(input)) return new Random().Next();
        int hash = 17;
        foreach (char c in input) hash = hash * 31 + c;
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
            GUI.SetCursorPosition(startX, startY + i);
            GUI.WriteLine(lines[i]);
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
        if (isLinux) return;

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
                "avaragehum",
                "menu"
            };
    public static int sleepTime = 100;
    public static double SimulationSpeed = 5.0;
    public int maxSleepTime = 3000;
    public int minSleepTime = 3;
    private void ListenForKeyPress()
    {
        while (continueSimulating)
        {
            while (!isConfiguring)
            {
                if (Console.KeyAvailable)
                {
                    var keyInfo = Console.ReadKey(true);
                    keybinds.Execute(keyInfo);
                    Thread.Sleep(sleepTime);
                }
            }
        }
    }
    private static void PanCamera(int dx, int dy)
    {
        if (chambers.Count == 0) return;
        var map = chambers[currentChamberIndex];
        if (map.camera == null) return;
        Interlocked.Add(ref _pendingPanDx, dx);
        Interlocked.Add(ref _pendingPanDy, dy);
        _renderPending = true;
    }
    private void RegisterKeybinds()
    {
        void chamberSwitch(int index)
        {
            var was = isUpdating;
            isUpdating = false;
            SwitchToChamber(index);
            UpdateChamberStats();
            _renderPending = true;
            isUpdating = was;
        }
        void directJump(int index) { if (chambers.Count > index && currentChamberIndex != index) chamberSwitch(index); }

        keybinds.RegisterCommand("ToggleUpdate", () =>
        {
            if (isCommandInputMode) return;
            IsHumidityRendering = false;
            IsTemperatureRendering = false;
            isUpdating = !isUpdating;
        });
        keybinds.Bind(InputContext.World, [ConsoleKey.P, ConsoleKey.Spacebar], "ToggleUpdate");
        keybinds.RegisterCommand("ToggleCommandMode", () => { if (!isUpdating) isCommandInputMode = !isCommandInputMode; });
        keybinds.Bind(InputContext.World, ConsoleKey.C, "ToggleCommandMode");
        /*keybinds.RegisterCommand("Cmd723", () => { chambers[currentChamberIndex].Generate(); UpdateChamberStats(); DisplayCurrentChamber(); });
        keybinds.Bind(InputContext.World, ConsoleKey.R, "Cmd723");*/
        keybinds.RegisterCommand("ToggleClouds", () =>
        {
            var was = isUpdating;
            isUpdating = false;
            isCloudsRendering = !isCloudsRendering;
            Thread.Sleep(200);
            UpdateChamberStats();
            _renderPending = true;
            Thread.Sleep(200);
            isUpdating = was;
        });
        keybinds.Bind(InputContext.World, ConsoleKey.Q, "ToggleClouds");
        keybinds.RegisterCommand("ToggleGUI", () =>
        {
            var was = isUpdating;
            isUpdating = false;
            displayGUI = !displayGUI;
            if (displayGUI)
            {
                UpdateStaticChamberStats();
                var m = chambers[currentChamberIndex];
                m.DisplayGUI();
                m._guiBuf.Flush();
            }
            else GUI.Clear();
            _renderPending = true;
            Thread.Sleep(200);
            isUpdating = was;
        });
        keybinds.Bind(InputContext.World, ConsoleKey.G, "ToggleGUI");
        keybinds.RegisterCommand("ChamberPrev", () =>  { if (chambers.Count > 1 && currentChamberIndex > 0) chamberSwitch(currentChamberIndex - 1); });
        keybinds.Bind(InputContext.World, ConsoleKey.LeftArrow, "ChamberPrev");
        keybinds.RegisterCommand("ChamberNext", () => { if (chambers.Count > 1 && currentChamberIndex < chambers.Count - 1) chamberSwitch(currentChamberIndex + 1); });
        keybinds.Bind(InputContext.World, ConsoleKey.RightArrow, "ChamberNext");
        keybinds.RegisterCommand("ChamberFirst", () =>  { if (chambers.Count > 1 && currentChamberIndex != 0) chamberSwitch(0); });
        keybinds.Bind(InputContext.World, ConsoleKey.DownArrow, "ChamberFirst");
        keybinds.RegisterCommand("ChamberLast", () =>    { if (chambers.Count > 1 && currentChamberIndex != chambers.Count - 1) chamberSwitch(chambers.Count - 1); });
        keybinds.Bind(InputContext.World, ConsoleKey.UpArrow, "ChamberLast");
        keybinds.RegisterCommand("Jump0", () => directJump(0));
        keybinds.Bind(InputContext.World, [ConsoleKey.D1, ConsoleKey.NumPad1], "Jump0");
        keybinds.RegisterCommand("Jump1", () => directJump(1));
        keybinds.Bind(InputContext.World, [ConsoleKey.D2, ConsoleKey.NumPad2], "Jump1");
        keybinds.RegisterCommand("Jump2", () => directJump(2));
        keybinds.Bind(InputContext.World, [ConsoleKey.D3, ConsoleKey.NumPad3], "Jump2");
        keybinds.RegisterCommand("Jump3", () => directJump(3));
        keybinds.Bind(InputContext.World, [ConsoleKey.D4, ConsoleKey.NumPad4], "Jump3");
        keybinds.RegisterCommand("Jump4", () => directJump(4));
        keybinds.Bind(InputContext.World, [ConsoleKey.D5, ConsoleKey.NumPad5], "Jump4");
        keybinds.RegisterCommand("Jump5", () => directJump(5));
        keybinds.Bind(InputContext.World, [ConsoleKey.D6, ConsoleKey.NumPad6], "Jump5");
        keybinds.RegisterCommand("Jump6", () => directJump(6));
        keybinds.Bind(InputContext.World, [ConsoleKey.D7, ConsoleKey.NumPad7], "Jump6");
        keybinds.RegisterCommand("Jump7", () => directJump(7));
        keybinds.Bind(InputContext.World, [ConsoleKey.D8, ConsoleKey.NumPad8], "Jump7");
        keybinds.RegisterCommand("Jump8", () => directJump(8));
        keybinds.Bind(InputContext.World, [ConsoleKey.D9, ConsoleKey.NumPad9], "Jump8");
        keybinds.RegisterCommand("Jump9", () => directJump(9));
        keybinds.Bind(InputContext.World, [ConsoleKey.D0, ConsoleKey.NumPad0], "Jump9");
        keybinds.RegisterCommand("SpeedDown", () => SimulationSpeed = Math.Max(0.5,  SimulationSpeed - 0.5));
        keybinds.Bind(InputContext.World, ConsoleKey.PageDown, "SpeedDown");
        keybinds.RegisterCommand("SpeedUp", () => SimulationSpeed = Math.Min(20.0, SimulationSpeed + 0.5));
        keybinds.Bind(InputContext.World, ConsoleKey.PageUp, "SpeedUp");
        keybinds.RegisterCommand("ToggleTempLayer", () =>
        {
            if (isUpdating || IsHumidityRendering) return;
            if (IsTemperatureRendering) isCloudsShadowsRendering = !isCloudsShadowsRendering;
            IsHumidityRendering = false;
            if (!IsTemperatureRendering) chambers[currentChamberIndex].RenderTemperatureNoise();
            else chambers[currentChamberIndex].DisplayMap(displayGUI);
            IsTemperatureRendering = !IsTemperatureRendering;
        });
        keybinds.Bind(InputContext.World, ConsoleKey.T, ConsoleModifiers.Shift, "ToggleTempLayer");
        keybinds.RegisterCommand("ToggleHumLayer", () =>
        {
            if (isUpdating || IsTemperatureRendering) return;
            if (IsHumidityRendering) isCloudsShadowsRendering = !isCloudsShadowsRendering;
            IsTemperatureRendering = false;
            if (!IsHumidityRendering) chambers[currentChamberIndex].RenderHumidityNoise();
            else chambers[currentChamberIndex].DisplayMap(displayGUI);
            IsHumidityRendering = !IsHumidityRendering;
        });
        keybinds.Bind(InputContext.World, ConsoleKey.H, ConsoleModifiers.Shift, "ToggleHumLayer");
        keybinds.RegisterCommand("PanUp", () => { if (!isCommandInputMode) PanCamera(0, -3); });
        keybinds.Bind(InputContext.World, ConsoleKey.W, "PanUp");
        keybinds.RegisterCommand("PanLeft", () => { if (!isCommandInputMode) PanCamera(-5, 0); });
        keybinds.Bind(InputContext.World, ConsoleKey.A, "PanLeft");
        keybinds.RegisterCommand("PanDown", () => { if (!isCommandInputMode) PanCamera(0,  3); });
        keybinds.Bind(InputContext.World, ConsoleKey.S, "PanDown");
        keybinds.RegisterCommand("PanRight", () => { if (!isCommandInputMode) PanCamera(5,  0); });
        keybinds.Bind(InputContext.World, ConsoleKey.D, "PanRight");
        
        string bindsPath = "Data/Config/keybinds.json";
        if (System.IO.File.Exists(bindsPath))
        {
            keybinds.LoadFromFile(bindsPath);
        }
        else
        {
            keybinds.SaveToFile(bindsPath);
        }
    }
    private void UpdateMaps()
        {
            while (continueSimulating)
            {
                while (!isConfiguring && !isMenu)
                {
                    lock (mapLock)
                    {
                        if (isUpdating)
                            foreach (var chamber in chambers)
                                chamber.Update();
                    }
                    Thread.Sleep(sleepTime);
                }
            }
        }
        private void UpdateWeather()
        {
            while (continueSimulating)
            {
                while (!isConfiguring && !isMenu)
                {
                    lock (mapLock)
                    {
                        if (isUpdating)
                        {
                            var map = chambers[currentChamberIndex];
                            map.deltaTime = sleepTime / 1000.0 * SimulationSpeed;
                            map.AnimateWater();
                            map.ComputeDayNightDarkness();
                            foreach (var chamber in chambers) chamber.UpdateClouds();
                            map.UpdateCloudState();
                            map.ComputeCloudShadows();
                        }
                    }
                    Thread.Sleep(sleepTime);
                }
            }
        }
        private void UpdateGUI()
        {
            while (continueSimulating)
            {
                while (!isConfiguring && !isMenu)
                {
                    lock (mapLock)
                    {
                        var map = chambers[currentChamberIndex];
                        if (CheckAndApplyResize(map)) _renderPending = true;
                        int pdx = Interlocked.Exchange(ref _pendingPanDx, 0);
                        int pdy = Interlocked.Exchange(ref _pendingPanDy, 0);
                        if ((pdx != 0 || pdy != 0) && map.camera != null)
                        {
                            var cam = map.camera;
                            cam.X = Math.Clamp(cam.X + pdx, 0, Math.Max(0, map.width - cam.Width));
                            cam.Y = Math.Clamp(cam.Y + pdy, 0, Math.Max(0, map.height - cam.Height));
                            _renderPending = true;
                        }
                        if (isUpdating || _renderPending)
                        {
                            _renderPending = false;
                            if (isUpdating) UpdateChamberStats();
                            map.DisplayMap(displayGUI);
                            if (displayGUI) map._guiBuf.Flush();
                        }
                    }
                    Thread.Sleep(sleepTime);
                }
            }
        }
        private static void UpdateStaticChamberStats()
        {
            chambers[currentChamberIndex].isCloudsRendering = isCloudsRendering;
            chambers[currentChamberIndex].isCloudsShadowsRendering = isCloudsShadowsRendering;
            Map.outputBuffer.AddRange(outputBuffer);
            outputBuffer.Clear();
            continueSimulating = chambers[currentChamberIndex].shouldSimulationContinue;
            chambers[currentChamberIndex].actualOutputBuffer = Map.outputBuffer;
        }
        public static void RequestTerminalResize(Map map)
        {
            if (!autoResize || map.SavedConsoleWidth <= 0 || map.SavedConsoleHeight <= 0) return;
            // ANSI xterm resize: ESC[8;<rows>;<cols>t
            Console.Write($"\033[8;{map.SavedConsoleHeight};{map.SavedConsoleWidth}t");
            // Brief pause to allow the terminal emulator to process the resize
            Thread.Sleep(150);
        }
        public static void RequestMinimumTerminalResize()
        {
            if (!autoResize) return;
            // Resize to the minimum viable size for the program (matches the startup size check)
            const int minWidth = 100;
            const int minHeight = 55;
            Console.Write($"\033[8;{minHeight};{minWidth}t");
            // Poll until the terminal actually reports the new size, or give up after 2 seconds
            var deadline = DateTime.Now.AddSeconds(2);
            while (DateTime.Now < deadline)
            {
                Thread.Sleep(50);
                if (Console.WindowWidth >= minWidth && Console.WindowHeight >= minHeight) break;
            }
        }
        public static bool CheckAndApplyResize(Map map)
        {
            int w = Console.WindowWidth;
            int h = Console.WindowHeight;
            if (w == _lastConsoleW && h == _lastConsoleH) return false;
            _lastConsoleW = w;
            _lastConsoleH = h;
            int viewW = Math.Min(map.width,  Math.Max(20, w / 2 - GUIConfig.LeftPadding - GUIConfig.RightPadding));
            int viewH = Math.Min(map.height, Math.Max(15, h - GUIConfig.BottomPadding - GUIConfig.TopPadding));
            if (map.camera == null) return false;
            map.camera.X = Math.Min(map.camera.X, Math.Max(0, map.width - viewW));
            map.camera.Y = Math.Min(map.camera.Y, Math.Max(0, map.height - viewH));
            map.camera.Resize(viewW, viewH);
            map.InvalidateFramebuffer();
            map._guiBuf.Resize(w, h);
            GUI.Clear();
            return true;
        }
        public static void SwitchToChamber(int index)
        {
            currentChamberIndex = index;
            chambers[index].InvalidateFramebuffer();
        }
        public static void DisplayCurrentChamber()
        {
            var map = chambers[currentChamberIndex];
            map.isCloudsRendering = false;
            map.InvalidateFramebuffer();
            map.DisplayMap(displayGUI);
            if (displayGUI) map._guiBuf.Flush();
        }
        private static string? ReadCommandWithAutocomplete()
        {
            StringBuilder input = new StringBuilder();
            int cursorPosition = 0;

            while (true)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);

                if (keyInfo.Key == ConsoleKey.Enter) return input.ToString();
                else if (keyInfo.Key == ConsoleKey.Escape) return null;
                else if (keyInfo.Key == ConsoleKey.Backspace)
                {
                    if (cursorPosition > 0)
                    {
                        input.Remove(cursorPosition - 1, 1);
                        cursorPosition--;
                        GUI.Write("\b \b");
                    }
                }
                else if (keyInfo.Key == ConsoleKey.Tab)
                {
                    string prefix = input.ToString();
                    string? suggestion = Commands.FirstOrDefault(cmd => cmd.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
                    if (suggestion != null)
                    {
                        // Clear the current input
                        GUI.Write(new string('\b', cursorPosition) + new string(' ', cursorPosition) + new string('\b', cursorPosition));
                        input.Clear();
                        input.Append(suggestion);
                        cursorPosition = suggestion.Length;
                        GUI.Write(suggestion);
                    }
                }
                else
                {
                    input.Insert(cursorPosition, keyInfo.KeyChar);
                    cursorPosition++;
                    GUI.Write(keyInfo.KeyChar);
                }
            }
        }
        public bool ProcessCommand(string command)
        {
            string[] tokens = command.Split(' ');
            switch (tokens[0])
            {
                case "menu":
                    isMenu = true;
                    isCloudsRendering = false;
                    isCloudsShadowsRendering = false;
                    IsTemperatureRendering = false;
                    IsHumidityRendering = false;
                    isUpdating = false;
                    chambers.Clear();
                    GUI.Clear();
                    foreach (var slot in slots.Where(s => s.isSelected).ToList())
                    {
                        var updatedSlot = slot;
                        updatedSlot.isSelected = false;
                        slots[slots.IndexOf(slot)] = updatedSlot;
                    }
                    DrawSaveSelectionGUI();
                    break;
                case "chamber":
                    if (tokens.Length > 1 && int.TryParse(tokens[1], out int chamberIndex))
                    {
                        chamberIndex--; // Adjust for 1-based index
                        if (chamberIndex >= 0 && chamberIndex < chambers.Count) SwitchToChamber(chamberIndex);
                        else outputBuffer.Add("Invalid chamber index");
                    }
                    else outputBuffer.Add("Please specify the chamber index");
                    break;
                case "exit":
                    return false;
                case "addchamber":
                    if (tokens.Length > 1 && int.TryParse(tokens[1], out int numberOfChambers))
                    {
                        for (int i = 0; i < numberOfChambers; i++) AddChamber();
                    }
                    else AddChamber();
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
                                int newIndex = Math.Max(0, chamberIndex - 1);
                                if (chambers.Count > 0) SwitchToChamber(newIndex);
                                else currentChamberIndex = 0;
                            }
                            else if (chamberIndex < currentChamberIndex) currentChamberIndex--;
                        }
                        else outputBuffer.Add("Invalid chamber index");
                    }
                    else outputBuffer.Add("Please specify the chamber index to remove");
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
                        else outputBuffer.Add("Invalid chamber index");
                    }
                    else outputBuffer.Add("Please specify the chamber index to regenerate");
                    break;
                case "run":
                    if (tokens.Length > 1)
                    {
                        // Extract method name and parameters
                        string fullCommand = string.Join(" ", tokens.Skip(1));
                        int methodStart = fullCommand.IndexOf('(');
                        string methodName = methodStart == -1 ? fullCommand : fullCommand.Substring(0, methodStart).Trim();

                        // Parse parameters if they exist
                        object[] parameters = [];
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

                            if (method != null) method.Invoke(this, parameters);
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
                    else outputBuffer.Add("Please specify the method to run");
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
                    outputBuffer.Add($"Unknown command: {tokens[0]}");
                    break;
            }
            chambers[currentChamberIndex].DisplayMap(displayGUI);
            return true;
        }
        public static string GetCommand()
        {
            GUI.Write(">> ");
            string? input = Console.ReadLine();
            if (input == null) return string.Empty;
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
        public void AddChamber()
        {
            isConfiguring = true;
            seed = rng.Next();
            Map newChamber = new Map(); 
            isConfiguring = newChamber.GetConfig();
            DisplayCenteredText(loadingAsciiArt);
            newChamber.Generate();
            chambers.Add(newChamber);
            outputBuffer.Add("Added a new chamber");
        }
        #region run functions
        public void SpawnMoreTurtles(int count) => chambers[currentChamberIndex].InitializeSpecies(count - 1, count, new Turtle(0, 0, seed));
        public void SpawnCloud(int x, int y, CloudType type) => chambers[currentChamberIndex].SpawnCloud(x, y, type);
        #endregion
}
