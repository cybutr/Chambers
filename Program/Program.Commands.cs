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
            foreach (var chamber in chambers) SaveMap(chamber);
            chambers.Clear();
            GUI.Clear();
            foreach (var slot in slots.Where(s => s.isSelected).ToList())
            {
                var updatedSlot = slot;
                updatedSlot.isSelected = false;
                slots[slots.IndexOf(slot)] = updatedSlot;
            }
            DrawSaveSelectionGUI();
            GUI.Clear();
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
            if (args.Length == 0 || args[0] == "get")
            {
                long ticks = (long)map.dayNight.DayCount * DayNightCycle.TicksPerDay + (long)(map.dayNight.TimeOfDay / 24.0 * DayNightCycle.TicksPerDay);
                outputBuffer.Add($"Time: {ticks} ticks | Day {map.dayNight.DayCount} | {map.dayNight.TimeOfDay:F1}h");
                return;
            }
            if (args[0] != "set" || args.Length < 2 || !long.TryParse(args[1], out long newTicks) || newTicks < 0)
            { outputBuffer.Add("Usage: time get | time set <ticks>"); return; }

            int prevSeason = (int)map.dayNight.Season;
            map.dayNight.DayCount = (int)(newTicks / DayNightCycle.TicksPerDay);
            map.dayNight.TimeOfDay = (newTicks % DayNightCycle.TicksPerDay) / (double)DayNightCycle.TicksPerDay * 24.0;
            map.dayNight.Season = map.dayNight.GetSeason(newTicks);
            map.dayNight.UpdateSunTimes();
            int newSeason = (int)map.dayNight.Season;
            if (prevSeason != newSeason) EventBus.Emit(new SeasonChangedEvent(newSeason));
            outputBuffer.Add($"Time: {newTicks} ticks | Day {map.dayNight.DayCount} | {map.dayNight.TimeOfDay:F1}h");
        });

        commandRegistry.Register("season", "Get/set season. Usage: season get | season set <0-4>", args =>
        {
            var map = chambers[currentChamberIndex];
            if (args.Length == 0 || args[0] == "get")
            {
                outputBuffer.Add($"Season: {map.dayNight.Season:F2} | {(int)map.dayNight.Season} ({map.dayNight.GetSeasonName()})");
                return;
            }
            if (args[0] != "set" || args.Length < 2 || !double.TryParse(args[1], out double s) || s < 0 || s > 4)
            { outputBuffer.Add("Usage: season get | season set <0-4>"); return; }
            s += 0.03;
            double offset = ((s - map.dayNight.InitSeason) % 4 + 4) % 4;
            int newDayCount = (int)(offset * DayNightCycle.DaysPerSeason);
            while (newDayCount < map.dayNight.DayCount) newDayCount += 4 * DayNightCycle.DaysPerSeason;
            int prevSeason = (int)map.dayNight.Season;
            map.dayNight.DayCount = newDayCount;
            map.dayNight.Season = map.dayNight.GetSeason((long)newDayCount * DayNightCycle.TicksPerDay);
            map.dayNight.UpdateSunTimes();
            int newSeason = (int)map.dayNight.Season;
            if (prevSeason != newSeason) EventBus.Emit(new SeasonChangedEvent(newSeason));
            outputBuffer.Add($"Season: {map.dayNight.Season:F2} | {(int)map.dayNight.Season} ({map.dayNight.GetSeasonName()})");
        });

        commandRegistry.Register("speed", "Get/set simulation speed. Usage: speed get | speed set <0.5-50>", args =>
        {
            if (args.Length == 0 || args[0] == "get") { outputBuffer.Add($"Speed: {SimulationSpeed}x"); return; }
            if (args[0] != "set" || args.Length < 2 || !double.TryParse(args[1], out double s))
            { outputBuffer.Add("Usage: speed get | speed set <n>"); return; }
            SimulationSpeed = Math.Clamp(s, 0.5, 50.0);
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
                outputBuffer.Add($"Wind: {map.weather.WindDirection:F0}° at {map.weather.WindSpeed:F1} m/s");
                return;
            }
            if (args[0] != "set" || args.Length < 3 || !double.TryParse(args[1], out double dir) || !double.TryParse(args[2], out double spd))
            { outputBuffer.Add("Usage: wind get | wind set <deg> <speed>"); return; }
            map.weather.WindDirection = Math.Clamp(dir, 0, 360);
            map.weather.WindSpeed = Math.Clamp(spd, 0, 100);
            outputBuffer.Add($"Wind: {map.weather.WindDirection:F0}° at {map.weather.WindSpeed:F1} m/s");
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

        commandRegistry.Register("caminfo", "Show world size, viewport size, and camera position", _ =>
        {
            var map = chambers[currentChamberIndex];
            if (map.camera == null) { outputBuffer.Add("No camera"); return; }
            outputBuffer.Add($"World: {map.width}×{map.height} | Viewport: {map.camera.Width}×{map.camera.Height} | Camera: [{map.camera.X},{map.camera.Y}]");
        });

        #endregion
        #region species commands

        commandRegistry.Register("species", "List species counts", _ =>
        {
            var map = chambers[currentChamberIndex];
            outputBuffer.Add(map._species.GetSummary());
        });

        commandRegistry.Register("spawn", "Spawn a species. Usage: spawn <type> [x y]", args =>
        {
            var map = chambers[currentChamberIndex];
            if (args.Length == 0) { outputBuffer.Add("Usage: spawn <type> [x y]"); return; }
            string type = args[0].ToLower();
            if (!Enum.TryParse(type, ignoreCase: true, out EntityId entityId) || entityId == EntityId.None)
            { outputBuffer.Add($"Unknown type '{type}'. Try: crab turtle cow sheep wolf bear goat fish bird"); return; }
            var def = EntityRegistry.Get(entityId);
            if (def?.Factory == null) { outputBuffer.Add($"{type} not spawnable"); return; }
            var allowed = def.AllowedTiles;
            bool anyTile = allowed.Count == 0;

            int x, y;
            if (args.Length >= 3 && int.TryParse(args[1], out int sx) && int.TryParse(args[2], out int sy))
            {
                if (sx < 0 || sx >= map.width || sy < 0 || sy >= map.height) { outputBuffer.Add("Out of bounds"); return; }
                if (!anyTile && !allowed.Contains(map.mapData[sx, sy])) { outputBuffer.Add($"Invalid tile for {type}"); return; }
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
                    var tile = map.mapData[rx, ry];
                    if (tile == TileId.Border) continue;
                    if ((anyTile || allowed.Contains(tile)) && map.overlayData[rx, ry] == EntityId.None)
                        (x, y) = (rx, ry);
                }
                if (x == -1) { outputBuffer.Add($"No valid tile for {type}"); return; }
            }

            map._species.Spawn(entityId, x, y, map.rng.Next(), map.overlayData);
            outputBuffer.Add($"Spawned {type} at [{x},{y}]");
        });

        commandRegistry.Register("clear", "Remove all of a species. Usage: clear <type>", args =>
        {
            var map = chambers[currentChamberIndex];
            if (args.Length == 0) { outputBuffer.Add("Usage: clear <type>"); return; }
            string type = args[0].ToLower();
            if (!Enum.TryParse(type, ignoreCase: true, out EntityId entityId) || entityId == EntityId.None)
            { outputBuffer.Add($"Unknown type. Try: crab turtle cow sheep wolf bear goat fish bird"); return; }
            int removed = map._species.ClearType(entityId, map.overlayData);
            outputBuffer.Add($"Removed {removed} {type}");
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
