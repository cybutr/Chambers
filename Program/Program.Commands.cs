using System.Reflection;
using Internal;

public partial class Program
{
    public CommandRegistry commandRegistry { get; } = new();

    private void RegisterAllCommands()
    {
        commandRegistry.Register("help", "List commands. Usage: help [page]", args =>
        {
            var lines = commandRegistry.GetHelp().ToList();
            int pageSize = 7;
            int page = args.Length > 0 && int.TryParse(args[0], out int p) ? Math.Clamp(p, 1, int.MaxValue) : 1;
            int totalPages = (lines.Count + pageSize - 1) / pageSize;
            page = Math.Min(page, totalPages);
            foreach (var line in lines.Skip((page - 1) * pageSize).Take(pageSize).Reverse())
                outputBuffer.Add(line);
            outputBuffer.Add($"Commands — page {page}/{totalPages}");
        });

        commandRegistry.Register("exit", "Exit the simulation", _ => commandRegistry.SignalExit());

        commandRegistry.Register("menu", "Return to main menu", _ =>
        {
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
        });

        commandRegistry.Register("chamber", "Switch to chamber <n>", args =>
        {
            if (args.Length > 0 && int.TryParse(args[0], out int i))
            {
                i--;
                if (i >= 0 && i < chambers.Count) SwitchToChamber(i);
                else outputBuffer.Add("Invalid chamber index");
            }
            else outputBuffer.Add("Usage: chamber <n>");
        });

        commandRegistry.Register("addchamber", "Add [n] new chambers (default 1)", args =>
        {
            int n = args.Length > 0 && int.TryParse(args[0], out int parsed) ? parsed : 1;
            for (int i = 0; i < n; i++) AddChamber();
        });

        commandRegistry.Register("removechamber", "Remove chamber <n>", args =>
        {
            if (args.Length > 0 && int.TryParse(args[0], out int i))
            {
                i--;
                if (i >= 0 && i < chambers.Count)
                {
                    chambers.RemoveAt(i);
                    outputBuffer.Add($"Removed chamber {i + 1}");
                    if (i == currentChamberIndex)
                    {
                        if (chambers.Count > 0) SwitchToChamber(Math.Max(0, i - 1));
                        else currentChamberIndex = 0;
                    }
                    else if (i < currentChamberIndex) currentChamberIndex--;
                }
                else outputBuffer.Add("Invalid chamber index");
            }
            else outputBuffer.Add("Usage: removechamber <n>");
        });

        commandRegistry.Register("regenerate", "Regenerate chamber <n>", args =>
        {
            if (args.Length > 0 && int.TryParse(args[0], out int i))
            {
                i--;
                if (i >= 0 && i < chambers.Count)
                {
                    chambers[i].Generate();
                    outputBuffer.Add($"Regenerated chamber {i + 1}");
                }
                else outputBuffer.Add("Invalid chamber index");
            }
            else outputBuffer.Add("Usage: regenerate <n>");
        });

        commandRegistry.Register("run", "Invoke a Program method. Example: run SpawnCloud(10,10,1)", args =>
        {
            if (args.Length == 0) { outputBuffer.Add("Usage: run Method(params)"); return; }
            string full = string.Join(" ", args);
            int parenStart = full.IndexOf('(');
            string methodName = parenStart == -1 ? full : full[..parenStart].Trim();
            object[] parameters = [];
            if (parenStart != -1)
            {
                int parenEnd = full.LastIndexOf(')');
                if (parenEnd != -1)
                {
                    parameters = [.. full[(parenStart + 1)..parenEnd]
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => (object)Convert.ChangeType(p.Trim(), typeof(int)))];
                }
            }
            try
            {
                var method = typeof(Program).GetMethod(methodName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                    null, CallingConventions.Any, [.. parameters.Select(p => p.GetType())], null);
                if (method != null) method.Invoke(this, parameters);
                else outputBuffer.Add($"Method '{methodName}' not found");
            }
            catch (Exception ex) { outputBuffer.Add($"Error: {ex.Message}"); }
        });

        commandRegistry.Register("seed", "Print current map seed", _ =>
            outputBuffer.Add($"Seed: {chambers[currentChamberIndex].conf.Seed}"));

        commandRegistry.Register("avaragetemp", "Print average map temperature", _ =>
            outputBuffer.Add($"Avg temp: {Math.Round(chambers[currentChamberIndex].avarageTempature, 2)}"));

        commandRegistry.Register("avaragehum", "Print average map humidity", _ =>
            outputBuffer.Add($"Avg humidity: {Math.Round(chambers[currentChamberIndex].avarageHumidity, 2)}"));

        #region time commands

        commandRegistry.Register("time", "Get/set world time in ticks (24000=1 day). Usage: time get | time set <ticks>", args =>
        {
            var map = chambers[currentChamberIndex];
            const int ticksPerDay = 24000;
            const int daysPerSeason = 10;
            if (args.Length == 0 || args[0] == "get")
            {
                long ticks = (long)map.dayNight.DayCount * ticksPerDay + (long)(map.dayNight.TimeOfDay / 24.0 * ticksPerDay);
                outputBuffer.Add($"Time: {ticks} ticks | Day {map.dayNight.DayCount} | {map.dayNight.TimeOfDay:F1}h");
                return;
            }
            if (args[0] != "set" || args.Length < 2 || !long.TryParse(args[1], out long newTicks) || newTicks < 0)
            { outputBuffer.Add("Usage: time get | time set <ticks>"); return; }

            int prevSeason = (int)map.dayNight.Season;
            map.dayNight.DayCount = (int)(newTicks / ticksPerDay);
            map.dayNight.TimeOfDay = (newTicks % ticksPerDay) / (double)ticksPerDay * 24.0;
            map.dayNight.Season = (double)(newTicks % ((long)daysPerSeason * 4 * ticksPerDay)) / (daysPerSeason * ticksPerDay);
            map.dayNight.UpdateSunTimes();
            int newSeason = (int)map.dayNight.Season;
            if (prevSeason != newSeason) EventBus.Emit(new SeasonChangedEvent(newSeason));
            map.SubscribeSpeciesEvents();
            outputBuffer.Add($"Time: {newTicks} ticks | Day {map.dayNight.DayCount} | {map.dayNight.TimeOfDay:F1}h");
        });

        commandRegistry.Register("season", "Get/set season. Usage: season get | season set <0-3>", args =>
        {
            var map = chambers[currentChamberIndex];
            string[] names = ["Spring", "Summer", "Autumn", "Winter"];
            if (args.Length == 0 || args[0] == "get")
            {
                int s = Math.Clamp((int)map.dayNight.Season, 0, 3);
                outputBuffer.Add($"Season: {names[s]} | Day {map.dayNight.DayCount}");
                return;
            }
            if (args[0] != "set" || args.Length < 2 || !int.TryParse(args[1], out int season) || season < 0 || season > 3)
            { outputBuffer.Add("Usage: season get | season set <0-3>"); return; }
            int prev = (int)map.dayNight.Season;
            map.dayNight.Season = season;
            map.dayNight.UpdateSunTimes();
            if (prev != season) EventBus.Emit(new SeasonChangedEvent(season));
            outputBuffer.Add($"Season: {names[season]}");
        });

        commandRegistry.Register("speed", "Get/set simulation speed. Usage: speed get | speed set <0.5-20>", args =>
        {
            if (args.Length == 0 || args[0] == "get") { outputBuffer.Add($"Speed: {SimulationSpeed}x"); return; }
            if (args[0] != "set" || args.Length < 2 || !double.TryParse(args[1], out double s))
            { outputBuffer.Add("Usage: speed get | speed set <n>"); return; }
            SimulationSpeed = Math.Clamp(s, 0.5, 20.0);
            outputBuffer.Add($"Speed: {SimulationSpeed}x");
        });

        #endregion
        #region weather commands

        commandRegistry.Register("weather", "Get/set weather type. Usage: weather get | weather set <type>", args =>
        {
            var map = chambers[currentChamberIndex];
            if (args.Length == 0 || args[0] == "get")
            {
                outputBuffer.Add($"Weather: {map.weather.CurrentWeather} → {map.weather.NextWeather}");
                return;
            }
            if (args[0] != "set" || args.Length < 2)
            { outputBuffer.Add($"Types: {string.Join(" ", Enum.GetNames<WeatherType>())}"); return; }
            if (!Enum.TryParse<WeatherType>(args[1], true, out WeatherType wt))
            { outputBuffer.Add($"Types: {string.Join(" ", Enum.GetNames<WeatherType>())}"); return; }
            var previous = map.weather.CurrentWeather;
            map.weather.CurrentWeather = wt;
            EventBus.Emit(new WeatherChangedEvent(previous, wt));
            outputBuffer.Add($"Weather: {wt}");
        });

        commandRegistry.Register("wind", "Get/set wind. Usage: wind get | wind set <deg> <speed>", args =>
        {
            var map = chambers[currentChamberIndex];
            if (args.Length == 0 || args[0] == "get")
            {
                outputBuffer.Add($"Wind: {map.weather.WindDirection:F0}° at {map.weather.WindSpeed:F1} km/h");
                return;
            }
            if (args[0] != "set" || args.Length < 3 || !double.TryParse(args[1], out double dir) || !double.TryParse(args[2], out double spd))
            { outputBuffer.Add("Usage: wind get | wind set <deg> <speed>"); return; }
            map.weather.WindDirection = Math.Clamp(dir, 0, 360);
            map.weather.WindSpeed = Math.Clamp(spd, 0, 100);
            outputBuffer.Add($"Wind: {map.weather.WindDirection:F0}° at {map.weather.WindSpeed:F1} km/h");
        });

        #endregion
        #region map commands

        commandRegistry.Register("info", "Tile info at coords. Usage: info <x> <y>", args =>
        {
            var map = chambers[currentChamberIndex];
            if (args.Length < 2 || !int.TryParse(args[0], out int x) || !int.TryParse(args[1], out int y))
            { outputBuffer.Add("Usage: info <x> <y>"); return; }
            if (x < 0 || x >= map.width || y < 0 || y >= map.height)
            { outputBuffer.Add($"Out of bounds (0–{map.width - 1}, 0–{map.height - 1})"); return; }
            TileId tile = map.mapData[x, y];
            EntityId entity = map.overlayData[x, y];
            int temp = map.temperatureData[x, y];
            int hum = map.humidityData[x, y];
            string ent = entity != EntityId.None ? $" | {entity}" : "";
            outputBuffer.Add($"[{x},{y}] {tile} | {temp}°C | {hum}%{ent}");
        });

        commandRegistry.Register("goto", "Move camera to coords. Usage: goto <x> <y>", args =>
        {
            var map = chambers[currentChamberIndex];
            if (map.camera == null) { outputBuffer.Add("No camera"); return; }
            if (args.Length < 2 || !int.TryParse(args[0], out int x) || !int.TryParse(args[1], out int y))
            { outputBuffer.Add("Usage: goto <x> <y>"); return; }
            map.camera.X = Math.Clamp(x, 0, Math.Max(0, map.width - map.camera.Width));
            map.camera.Y = Math.Clamp(y, 0, Math.Max(0, map.height - map.camera.Height));
            map.InvalidateFramebuffer();
            outputBuffer.Add($"Camera → [{map.camera.X},{map.camera.Y}]");
        });

        #endregion
        #region species commands

        commandRegistry.Register("species", "List species counts", _ =>
        {
            var map = chambers[currentChamberIndex];
            outputBuffer.Add($"Crabs: {map.crabs.Count} | Turtles: {map.turtles.Count} | Cows: {map.cows.Count} | Sheeps: {map.sheeps.Count}");
        });

        commandRegistry.Register("spawn", "Spawn a species. Usage: spawn <type> [x y] | crab turtle cow sheep", args =>
        {
            var map = chambers[currentChamberIndex];
            if (args.Length == 0) { outputBuffer.Add("Usage: spawn <type> [x y]"); return; }
            string type = args[0].ToLower();
            HashSet<TileId>? allowed = type switch
            {
                "crab"   => [TileId.Beach, TileId.BeachDark],
                "turtle" => [TileId.Beach, TileId.BeachDark],
                "cow"    => [TileId.Plains],
                "sheep"  => [TileId.Plains],
                _        => null
            };
            if (allowed is null) { outputBuffer.Add("Types: crab turtle cow sheep"); return; }

            int x, y;
            if (args.Length >= 3 && int.TryParse(args[1], out int sx) && int.TryParse(args[2], out int sy))
            {
                if (sx < 0 || sx >= map.width || sy < 0 || sy >= map.height) { outputBuffer.Add("Out of bounds"); return; }
                if (!allowed.Contains(map.mapData[sx, sy])) { outputBuffer.Add($"Invalid tile for {type}"); return; }
                if (map.overlayData[sx, sy] != EntityId.None) { outputBuffer.Add($"[{sx},{sy}] occupied"); return; }
                (x, y) = (sx, sy);
            }
            else
            {
                (x, y) = (-1, -1);
                for (int i = 0; i < 500 && x == -1; i++)
                {
                    int rx = map.rng.Next(0, map.width);
                    int ry = map.rng.Next(0, map.height);
                    if (allowed.Contains(map.mapData[rx, ry]) && map.overlayData[rx, ry] == EntityId.None)
                        (x, y) = (rx, ry);
                }
                if (x == -1) { outputBuffer.Add($"No valid tile for {type}"); return; }
            }

            switch (type)
            {
                case "crab":   map.crabs.Add(new Crab(x, y, map.rng.Next()));     map.overlayData[x, y] = EntityId.Crab;   break;
                case "turtle": map.turtles.Add(new Turtle(x, y, map.rng.Next())); map.overlayData[x, y] = EntityId.Turtle; break;
                case "cow":    map.cows.Add(new Cow(x, y, map.rng.Next()));       map.overlayData[x, y] = EntityId.Cow;    break;
                case "sheep":  map.sheeps.Add(new Sheep(x, y, map.rng.Next()));   map.overlayData[x, y] = EntityId.Sheep;  break;
            }
            outputBuffer.Add($"Spawned {type} at [{x},{y}]");
        });

        commandRegistry.Register("clear", "Remove all of a species. Usage: clear <type>", args =>
        {
            var map = chambers[currentChamberIndex];
            if (args.Length == 0) { outputBuffer.Add("Usage: clear <type>"); return; }

            void ClearList<T>(List<T> list, string name) where T : Species
            {
                foreach (var e in list) map.overlayData[e.X, e.Y] = EntityId.None;
                int n = list.Count;
                list.Clear();
                outputBuffer.Add($"Removed {n} {name}");
            }

            switch (args[0].ToLower())
            {
                case "crab":   ClearList(map.crabs,   "crabs");   break;
                case "turtle": ClearList(map.turtles, "turtles"); break;
                case "cow":    ClearList(map.cows,    "cows");    break;
                case "sheep":  ClearList(map.sheeps,  "sheeps");  break;
                default: outputBuffer.Add("Types: crab turtle cow sheep"); break;
            }
        });

        #endregion
    }

    public bool ProcessCommand(string commandString)
    {
        if (string.IsNullOrWhiteSpace(commandString)) return true;
        bool result = commandRegistry.Execute(commandString);
        chambers[currentChamberIndex].DisplayMap(displayGUI);
        return result;
    }
}
