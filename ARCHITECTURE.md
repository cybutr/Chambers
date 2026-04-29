# Chambers — Architecture & Developer Reference

A console-based procedural world simulation engine written in C# (.NET 10.0).  
Generates and simulates living worlds with terrain, weather, animals, and persistent saves — rendered entirely in the terminal using ANSI escape codes and ASCII/Unicode art.

---

## Table of Contents

1. [Project Structure](#project-structure)
2. [Application Lifecycle](#application-lifecycle)
3. [Threading Model](#threading-model)
4. [Data Model: The Map](#data-model-the-map)
5. [Tile System](#tile-system)
6. [Procedural Generation Pipeline](#procedural-generation-pipeline)
7. [Configuration System](#configuration-system)
8. [Animal AI (Species)](#animal-ai-species)
9. [A* Pathfinding (AStar.cs)](#a-pathfinding-astarcs)
10. [Weather System](#weather-system)
11. [GUI & Rendering](#gui--rendering)
12. [Save / Load System](#save--load-system)
13. [Command System](#command-system)
14. [Key Interactions Between Components](#key-interactions-between-components)

---

## Project Structure

```
Chambers/
├── Program.cs                        # Entry point, app lifecycle, threads, save/load, commands
├── Program/
│   ├── Program.Serialization.cs      # JSON converters, SaveMap/LoadMap, binary format
│   ├── Program.GUI.cs                # Save selection GUI, slot management
│   └── Program.Tests.cs              # Test methods, Testing() runner
│
├── MapGeneration/
│   ├── Map.cs                        # Core Map entity, generation coordinator, partial class root
│   ├── Models/
│   │   ├── PerlinNoise.cs            # Perlin + Simplex noise generators
│   │   ├── Wave.cs                   # Wave data model
│   │   └── Weather.cs                # Weather state + WeatherType/CloudType enums
│   └── Components/
│       ├── Map.Camera.cs             # Camera, framebuffer, InvalidateFramebuffer()
│       ├── Map.Frame.cs              # Coastline/border generation
│       ├── Map.Mountains.cs          # Mountain ranges, snow caps, forest integration
│       ├── Map.WaterGen.cs           # Rivers, lakes, flow simulation
│       ├── Map.Waves.cs              # Wave rendering and animation
│       ├── Map.Clouds.cs             # Cloud spawning, movement, rendering, morph
│       ├── Map.Weather.cs            # Weather state, wind, temperature, day/night cycle
│       ├── Map.Display.cs            # DisplayMap(), framebuffer dirty renderer
│       ├── Map.GUI.cs                # DisplayGUI(), widgets, config GUI
│       ├── Map.Species.cs            # Species lists, InitializeSpecies, Update*
│       └── Map.Utils.cs              # Flood-fill, biome helpers, distance utils, A* wrappers
│
├── Core/
│   ├── TileId.cs                     # TileId enum (typed terrain)
│   ├── EntityId.cs                   # EntityId enum (typed overlay)
│   ├── TileRegistry.cs               # TileDefinition registry — color, cost, flags per tile
│   └── Config.cs                     # Config class (70+ params), GUIConfig, Habitat enum
│
├── Species/
│   └── Species.cs                    # Species base class + Crab, Turtle, Cow, Sheep
│
├── Other/
│   ├── GUI.cs                        # Thread-safe console rendering wrapper
│   ├── Characters.cs                 # ASCII art sprites for A-Z, 0-9
│   ├── ColorSpectrum.cs              # 100+ named RGB color constants
│   └── AStar.cs                      # A* pathfinding with organic styling
│
└── Data/Saves/                       # World persistence files
    └── *.chmb                        # Binary saves (primary); *.json legacy
```

`Map` is a `partial class` split across `Map.cs` and all `Components/` files — they all compile into one class.

---

## Application Lifecycle

**Entry: `Program.Main()`** (`Program.cs:61`)

```
1. Parse CLI args (--test-sync, --test-gui, --test-config, --testing)
2. Enable virtual terminal processing (ANSI on Windows)
3. LoadAllMapsFromFolder("Saves/")       — deserialize existing worlds
4. SynchronizeAllChamberFiles()          — ensure filenames match Config.Name
5. Build save slot list (up to numberOfRows slots)
6. DrawSaveSelectionGUI()               — show slot picker UI
7. Start 4 background threads (update, key, weather, gui)
8. Main loop: wait for key input → read command → ProcessCommand()
9. On exit: SaveMap() for all chambers, join all threads
```

**Save slot system:**  
`slots` is a `List<(Map chamber, string? name, bool isSelected, bool isTyping, bool isEmpty)>`.  
Slots map 1:1 to JSON files in `Saves/`. Loading populates `allChambers`; selecting slots moves maps into the active `chambers` list.

---

## Threading Model

Four threads run concurrently from `Main()`. All console I/O is protected by `lock (_consoleLock)` inside `GUI.cs`.

```
Main Thread      ─── reads user commands, drives ProcessCommand()
updateThread     ─── calls UpdateMaps()   → ticks simulation forward
keyListenerThread─── calls ListenForKeyPress() → sets isCommandInputMode = true
weatherThread    ─── calls UpdateWeather() → advances climate state
guiThread        ─── calls UpdateGUI()    → redraws screen every frame
```

**Shared state protection:**  
- `mapLock` (`object`) guards critical sections that write to `chambers` or `mapData`
- `GUI._consoleLock` (`object`) guards all console writes
- `sleepTime` controls the main-thread idle rate

**Global flags (all `static` on `Program`):**

| Flag | Purpose |
|---|---|
| `continueSimulating` | Master loop exit condition |
| `isUpdating` | True while simulation tick is running |
| `isCommandInputMode` | True while user is typing a command |
| `isCloudsRendering` | Cloud layer visible |
| `isCloudsShadowsRendering` | Shadow layer visible |
| `IsHumidityRendering` | Humidity overlay active |
| `IsTemperatureRendering` | Temperature overlay active |
| `isConfiguring` | Config GUI open (pauses other threads) |
| `isMenu` | Save selection screen visible |
| `displayGUI` | Whether to draw the side panels |

---

## Data Model: The Map

`Map` (`MapGeneration/Map.cs`) is the core simulation entity. All terrain, weather, animals, and render state live here.

### Arrays

| Field | Type | Size | Purpose |
|---|---|---|---|
| `mapData` | `TileId[,]` | `width × height` | Primary terrain layer |
| `overlayData` | `EntityId[,]` | `width × height` | Animals/NPCs drawn on top |
| `cloudData` | `CloudType[,]` | `cloudDataWidth × cloudDataHeight` | Cloud visual layer (3× map size) |
| `cloudDepthData` | `int[,]` | same | Visual depth per cloud tile |
| `_cloudDataSwap` | `CloudType[,]` | same | Pre-allocated swap buffer for cloud shift |
| `_cloudDepthSwap` | `int[,]` | same | Pre-allocated swap buffer for depth shift |
| `_framebuffer` | `((r,g,b),overlayId)[,]?` | viewport | Per-cell dirty cache; null = full redraw |
| `darknessData` | `int[,]` | `width × height` | Day/night shadow intensity per tile |
| `shadowData` | `double[,]` | `width × height` | Cloud shadow intensity per tile |
| `precipitationData` | `double[,]` | `cloudDataWidth × cloudDataHeight` | Rainfall intensity 0.0–1.0 |
| `temperatureData` | `int[,]` | `width × height` | Temperature per tile |
| `humidityData` | `int[,]` | `width × height` | Humidity per tile |
| `noise` | `double[,]` | `width × height` | Raw Perlin height values |
| `tempatureNoise` | `double[,]` | `width × height` | Perlin temperature values |
| `humidityNoise` | `double[,]` | `width × height` | Perlin humidity values |

**Cloud data** is 3× the map size (`cloudDataWidth = width * 3`) so clouds can scroll continuously without clipping. Cloud presence is checked via `cloudData[x,y] != CloudType.None` — no position dictionary.

### Key Scalar Properties

```csharp
public double time;                  // Mirror of weather.TimeOfDay
public bool shouldSimulationContinue;// Per-map pause flag
public bool isCloudsRendering;
public bool isCloudsShadowsRendering;
public int cloudShadowOffsetX/Y;     // Shadow parallax offset
public double avarageTempature;
public double avarageHumidity;
public double deltaTime;             // sleepTime/1000 * SimulationSpeed (set by weatherThread)
public ulong tick;                   // Incremented each UpdateClouds() call [JsonIgnore]
public int cloudMorphInterval;       // SmoothAndFluffClouds runs every N ticks (default 2)
public double _cloudAccumX/Y;        // Sub-pixel cloud movement accumulators [JsonIgnore]
public Config conf;                  // All generation parameters
public Weather weather;              // Current climate state
public Camera camera;                // Viewport dimensions
public List<Cloud> clouds;           // Active cloud entities
public List<Wave> waves;             // Active wave entities
public List<string> actualOutputBuffer; // Per-map event log
```

---

## Tile System

All terrain is stored as typed enums — never raw `char`. Properties (color, movement cost, water/land flags) are looked up via `TileRegistry.Get(TileId)` → `TileDefinition`.

### Terrain Tiles (`mapData` — `TileId[,]`)

| TileId | Biome | A* Cost | Notes |
|---|---|---|---|
| `TileId.Plains` | Plains | 0.8 | Default land |
| `TileId.Forest` | Forest | 0.2 | Easiest to traverse |
| `TileId.Mountain` | Mountain peak | 10.0 | |
| `TileId.MountainDeep` | Mountain interior | 15.0 | Surrounded by 8 mountain neighbors |
| `TileId.Ocean` | Ocean deep | 5.0 | |
| `TileId.OceanShallow` | Ocean shallow | 6.0 | |
| `TileId.River` | River | 4.0 | |
| `TileId.RiverShallow` | River shallow | 3.0 | |
| `TileId.Lake` | Lake | 4.0 | |
| `TileId.LakeShallow` | Lake shallow | 3.0 | |
| `TileId.Beach` | Beach | 0.5 | Land↔water transition |
| `TileId.BeachDark` | Beach dark | 0.7 | |
| `TileId.Snow` | Snow | 20.0 | Mountain tops |
| `TileId.Stream` | Stream | 3.0 | |
| `TileId.Border` | Border | blocked | Map edge, impassable |
| `TileId.Empty` | Empty | — | No render |

### Overlay Tiles (`overlayData` — `EntityId[,]`)

| EntityId | Entity |
|---|---|
| `EntityId.None` | Empty tile |
| `EntityId.Crab` | Crab |
| `EntityId.Turtle` | Turtle |
| `EntityId.Cow` | Cow |
| `EntityId.Sheep` | Sheep |
| `EntityId.Wolf` | Wolf |

Tile properties come from `TileRegistry.Get(TileId)` → `TileDefinition`: `.IsWater`, `.IsLand`, `.MovementCost`, `.BaseColor`, `.CreatureCategories`.

---

## Procedural Generation Pipeline

`Map.Generate()` (`Map.cs:105`) runs three noise passes then delegates to `HandleGen()`.

```csharp
noise          = Perlin.GeneratePerlinNoise(width, height, NoiseScale,      seed)
tempatureNoise = Perlin.GeneratePerlinNoise(width, height, NoiseScale * 25, seed)
humidityNoise  = Perlin.GeneratePerlinNoise(width, height, NoiseScale * 12, seed)
```

### `HandleGen()` — Sequential Stages (`Map.cs:116`)

| # | Method | What it does |
|---|---|---|
| 1 | `SetAvarageTempatureHumidity()` | Computes global averages from noise |
| 2 | `AssignTempAndHumData()` | Sets per-tile `temperatureData` and `humidityData` from noise |
| 3 | `SmoothOutTempatureHumidity()` | Averages each cell with its neighbors for locality |
| 4 | `AssignBiomes(noise)` | Threshold-based assignment: `noise < 0.4` → `F`, `< 0.7` → `P`, `< 1.0` → `M` |
| 5 | `EnsureMinimumBiomeSize()` | Flood-fills connected regions; expands those smaller than `MinBiomeSize²` |
| 6 | `ReplaceBiome('M', 'P')` | Cleans up leftover raw mountain tiles before range gen |
| 7 | `CreateMountains()` | Pathfinding-style ridges, width variation, `'m'` interior, snow caps, forest integration |
| 8 | `CreateRiver()` | Flow-based water placement with width constraints |
| 9 | `CreateLakes()` | Cluster-based `'L'` placement respecting `MinLakeSize` |
| 10 | `CreateComplexFrame()` | Coastline via point-connection algorithm, water spreading from borders |
| 11 | `SingleTileCheckPF('P','F',2)` | Remove isolated plains tiles (< 2 neighbors) |
| 12 | `CreateBeaches()` | Add `'B'`/`'b'` between land and water |
| 13 | `WaterDepth()` | Assign shallow (`'o'`,`'r'`,`'l'`) vs deep uppercase by neighbor count |
| 14 | `FrameMap('@')` | Stamp border tiles around the edge |
| 15 | `RemoveSeperatedOceanTiles()` | Remove landlocked ocean tiles |
| 16 | `InitializeSpecies(...)` | Place Crabs, Turtles, Cows, Sheep on valid tiles |
| 17 | `InitializeWaves()` | Create `Wave` objects for ocean animation |
| 18 | `InitializeWeather()` | Set starting `Weather` state |
| 19 | `InitializeClouds()` | Populate `clouds` list based on weather type |

**All stages gate on `Config` flags** (e.g., `conf.EnableRivers`, `conf.GenerateAnimals`). Seed flows through `rng = new Random(seed)` so all generation is deterministic per seed string.

### Perlin Noise (`PerlinNoise.cs`)

```csharp
// Uses octave-based fBm (fractal Brownian motion)
// NoiseScale = 10.0 for terrain (zoomed out, large features)
// NoiseScale * 25 for temperature (coarser regions)
// NoiseScale * 12 for humidity (medium regions)
```

---

## Configuration System

### `Config` class (`Other/Config.cs:4`)

Holds all generation and simulation parameters. Passed into `Map.conf` at creation.

```csharp
// Construction
new Config(int Width, int Height, double NoiseScale, string Seed)
```

**Parameter groups:**

```
Map Shape
  Width, Height, NoiseScale, ErosionFactor
  MinBiomeSize, MinLakeSize, MinRiverWidth/MaxRiverWidth
  MinMountainWidth/MaxMountainWidth, RiverFlowChance
  ForestHeightThreshold (0.4), PlainsHeightThreshold (0.7), MountainHeightThreshold (1.0)

Features (bool toggles)
  EnableRivers, EnableLakes, EnableMountainRanges
  EnableTempatureBiomeChanges, EnableHumidityBiomeChanges
  BiomeBlend, NoiseType

Game Rules
  EnableWildfires, EnableSecrets, DoTimeCycle, DoWeatherCycle

Structures (all false by default)
  GenerateStructrs, EnableVillages, EnableCities, EnableDungeons

Animals
  GenerateAnimals, EnablePredators, EnableAnimalMovement
  EnableAnimalBreeding, EnableAnimalDeath, EnableAnimalExtinction
  EnableAnimalMigration, EnableAnimalHunting, EnableAnimalDomestication

Disasters (all false by default)
  EnableTornadoes, EnableEarthquakes, EnableVolcanoes, EnableFloods, EnableMeteors

Events (all false by default)
  EnableRobberies, EnableMurders, EnableRiots, EnablePlagues, EnableWars

Economy (all false by default)
  EnableTrade, EnableCurrency, EnableTaxes, EnableBanks

Visuals
  DisplayShadows (true), DisplayWaves (true), NumberOfWaves (13)

Persistence
  Name (world name / filename), Seed (string), ShouldSave (true)
```

**Seed system:** `string Seed` → `Program.ConvertStringToNumbers(Seed)` → `int` → `new Random(seed)`. Any string (text name, number, anything) produces a deterministic world.

### `GUIConfig` class (`Other/Config.cs:92`)

Calculates all panel dimensions from `Console.WindowWidth/Height` at startup.

```
TopPadding    = 11   (rows reserved above the map)
BottomPadding = 5    (rows reserved below the map)
LeftPadding   = 0
RightPadding  = 0

Panels:
  Radar      — weather radar (top-left)
  Title      — "Chambers" ASCII title (top-center)  width=50
  Stats      — weather statistics (flanks the title)
  Time       — time-of-day display
  Help       — keybind reference (right side)
  Tile       — hovered tile info (right side)
  Thanks     — credits (only if consoleWidth > 100)
  Output     — event log (only if consoleWidth > 100)
```

Minimum console size: **100 × 55**. Below that the app warns and exits.

### `Habitat` enum (`Other/Config.cs:178`)

43 biome types used for terrain classification and future feature gating:
`Plains, Forest, Mountain, Desert, Tundra, Ocean, River, Lake, Swamp, Jungle, Taiga, Savanna, Grassland, Wetland, Marsh, Reef, Coral, Volcano, Glacier, Iceberg, Island, Archipelago, Peninsula, Canyon, Oasis, Delta, Estuary, Fjord, Bay, Lagoon, Atoll, Cavern, Cave, Grotto, Ruins, Temple, Pyramid, Castle, Fortress, Dungeon, Village, Town, City, Capital`

### `Biome` class (`Other/Config.cs:225`)

```csharp
public class Biome {
    public Habitat Habitat { get; }
    public double HeightThreshold { get; }
    public double TempatureThreshold { get; }
    public double HumidityThreshold { get; }
}
```

Describes a biome by its three environmental thresholds — used when temperature/humidity biome changes are enabled.

---

## Animal AI (Species)

### Class Hierarchy (`Species.cs`)

```
Species (abstract)
├── abstract void Behave()
├── string Name
└── string Habitat

Boids
└── List<Species> Species  → calls Behave() on each

Crab : Species             (Beach: 'B', 'b')
Turtle : Species           (Beach + water: 'B','b','O','o','L','l','R','r')
```

### `Crab` (`Species.cs:49`)

```csharp
AllowedTiles = ['B', 'b']

Behave():
  if (rng > 0.7 && !IsHunted) → MoveRandomly()
  SearchForFood()              — periodic food search
  if (IsAggressive) Attack()   — 10% chance to attack
  CheckForPredatorsInRange()   — scan 10-tile radius for 'W' (wolves)
  AvoidPredators()             — flee if PredatorNearby
```

`IsAggressive` is set at construction: `rng.NextDouble() > 0.005` — so almost all crabs are aggressive by default.

### `Turtle` (`Species.cs`)

Same structure as Crab but `AllowedTiles` includes water tiles, and it has logic to return to beach when too far from shore (20% chance per tick).

### `Boids` (`Species.cs:18`)

Simple container. Iterates `Species` list and calls `Behave()` on each. Used for flock-level management.

---

## A* Pathfinding (`AStar.cs`)

Used by the generation pipeline to draw mountain ranges, rivers, and any organic linear features across the map.

### `PathNode` (inner class)

```csharp
int X, Y
float GCost    // cost from start
float HCost    // heuristic to goal
float FCost    // GCost + HCost
PathNode Parent
char TerrainType
```

### `TerrainConfig` (inner class)

Controls movement costs and path behavior. All defaults:

```
Movement costs:
  'F' = 0.2   'B' = 0.5   'b' = 0.7   'P' = 0.8
  's' = 3.0   'r' = 3.0   'l' = 3.0   'R' = 4.0
  'L' = 4.0   'O' = 5.0   'o' = 6.0   'M' = 10.0
  'm' = 15.0  'S' = 20.0  '@' = blocked

Organic path style:
  PathRandomness         = 0.2   (jitter in cost evaluation)
  WanderingBias          = 2.8   (pull toward non-direct routes)
  WaveAmplitude          = 0.7   (sinusoidal path curvature)
  WaveFrequency          = 0.6   
  OrganicNoise           = 1.8   (per-node noise jitter)
  StraightLineAvoidance  = 1.7   (penalizes perfectly straight paths)
  DirectionInertia       = 0.2   (momentum — prefer same direction)
  BiomeChangePenalty     = 0.5   (cost for crossing into a new biome)
  BiomeStickiness        = 0.8   (bonus for staying in current biome)
  BacktrackingPenalty    = 5.0   (heavy penalty for moving away from goal)
  AvoidBacktracking      = true
  RequireCardinalConnectivity = true  (N/S/E/W only, no diagonals)
  EnableObstacleAvoidance    = false (pre-computed distance maps)
  HighCostThreshold      = 3.0   (what counts as an obstacle)
  AvoidanceDistance      = 2.0   (tiles to stay away from obstacles)
  AvoidancePenaltyMultiplier = 2.0
```

**Usage in generation:** `CreateMountains()` calls AStar to trace a mountain ridge from point A to B, then expands width around the path. `CreateRiver()` traces flow paths through terrain with water-preferring costs.

---

## Weather System

### `Weather` class (`MapGeneration/Models/Weather.cs`)

```csharp
WeatherType CurrentWeather   // active weather
WeatherType NextWeather      // transitioning to
double Intensity             // 0.0–1.0 strength
double IntensityTarget       // lerp target
double IntensityChangeSpeed  // lerp rate
double Temperature           // °C — driven by continuous formula (not lookup table)
double Humidity              // 0–100
double Pressure              // hPa ~1013
double WindSpeed             // 0–40, includes gusts
double WindDirection         // degrees 0–360, turns ±90° max per event
double TimeOfDay             // 0.0–24.0
double Season                // 0.0–4.0 (0=spring, 1=summer, 2=autumn, 3=winter)
```

### Temperature formula (`Map.Weather.cs: GetTemperature`)

```
baseRegionTemp = (avarageTempature - 0.5) * 50        // -25 to +25°C from map setting
seasonalOffset = cos((season - 1.5) × π/2) × 15      // +15°C midsummer, -15°C midwinter
dailyOffset    = sin((timeOfDay - 8) / 24 × 2π) × 8  // +8°C at 2pm, -8°C at 2am
weatherOffset  = per-WeatherType constant (-3 to +1.5°C)
```

### Wind system (`Map.Weather.cs: UpdateWind`)

- Direction turns ±90° per event (smooth, 2°/tick), driven by `WindChangeTimer`
- Speed = `baseSpeed × timeOfDay × season × weatherFactor × 3 + gustStrength`
- Season factor: smooth cosine — winter 1.2×, summer 0.6×, equinoxes 0.9×
- Gusts: `_gustStrength` spikes 3–13 m/s, decays at 2/s; probability driven by weather type (thunderstorm 40%, sandstorm 50%, clear 5%)

### `WeatherType` enum

```
Clear=1, Rain=2, Snow=3, Thunderstorm=4, Fog=5, Overcast=6,
Hail=7, Sleet=8, Drizzle=9, BlowingSnow=10, Sandstorm=11
```

### `CloudType` enum

```
None=0, Cumulus=1, Stratus=2, Cirrus=3, Cumulonimbus=4, Nimbostratus=5, Altocumulus=6
```

### `Wave` class (`MapGeneration/Models/Wave.cs`)

Per-wave data: list of points, direction, speed, curvature, intensity.  
Rendered in `Map.Waves.cs`. Shoreline cache rebuilds lazily on first `AddNewWave` call after load.

---

## GUI & Rendering

### `GUI` static class (`Other/GUI.cs`)

Thread-safe wrapper around `System.Console`. Every method locks `_consoleLock`.

```csharp
// ANSI color — returns escape sequence strings
GUI.SetForegroundColor(r, g, b)   // \u001b[38;2;R;G;Bm
GUI.SetBackgroundColor(r, g, b)   // \u001b[48;2;R;G;Bm
GUI.ResetColor()                  // \u001b[0m

// Cursor
GUI.SetCursorPosition(x, y)
GUI.SetCursorVisible(bool)
GUI.Clear()

// Output
GUI.Write(string)
GUI.WriteLine(string)
GUI.WriteAt(x, y, text)           // position + write in one call

// Higher-level
GUI.DrawBox(x, y, w, h, title, leftDecor, rightDecor)
  // Unicode box-drawing on Windows: ╔═╗║╚╝
  // ASCII fallback on Linux: +-|
GUI.DrawColoredBox(x, y, w, h, title, r, g, b)
GUI.DrawGrid(width, height, leftPad, topPad, getPixel)
  // getPixel: (x,y) → (r,g,b,symbol) callback per cell
GUI.DrawPixel(x, y, r, g, b, symbol)
GUI.DisplayCenteredTextAtCords(text, x, y, color)
```

### `ColorSpectrum` static class (`Other/ColorSpectrum.cs`)

Named `(int r, int g, int b)` tuples for every visual element:

```
Cloud types: CIRRUS, ALTOCUMULUS, CUMULUS, CUMULONIMBUS, NIMBOSTRATUS, STRATUS
  each with _LIGHT, _MEDIUM, _DARK variants

Biome colors: FOREST_GREEN, PLAINS_TAN, MOUNTAIN_GREY, OCEAN_BLUE, BEACH_BEIGE, ...
Wave colors:  WAVE_1 through WAVE_5 (blue gradient)
Extended:     100+ CSS-like names (ALICE_BLUE, BURNT_ORANGE, DODGER_BLUE, ...)
```

### `Characters` static class (`Other/Characters.cs`)

Multi-line ASCII art definitions for A–Z, a–z, 0–9, plus an unknown fallback. Each character is 3 lines tall and variable width. Used to render the "Chambers" title overlay at startup and in the title panel.

### Render Pipeline (in `guiThread` → `UpdateGUI()`)

```
1. Static panels (title, stats, help, time) — redrawn on change
2. DisplayMap() — framebuffer dirty renderer (Map.Display.cs):
   a. Builds composite color per cell: tile base → temperature/humidity tint → darkness → cloud shadow
   b. Compares against _framebuffer[sx, sy] cache
   c. Only writes to console if (color, overlayId) changed
   d. _framebuffer = null forces full redraw (set by InvalidateFramebuffer())
3. Wave animation — water surface patterns drawn over ocean tiles
4. Event log — actualOutputBuffer scrolled at bottom
```

**Framebuffer invalidation:** Call `map.InvalidateFramebuffer()` on chamber switch, map load, or any bulk state change. It sets `_framebuffer = null`; next `DisplayMap` does a full console flush.

**Platform detection:** `RuntimeInformation.IsOSPlatform(OSPlatform.Linux)` gates box-drawing character choice throughout `GUI.DrawBox()`.

---

## Save / Load System

Two formats, controlled by `Program.SaveFormat` (`SerializationFormat` enum). Binary is the default.

| Format | Extension | Entry points |
|---|---|---|
| Binary | `.chmb` | `SaveMapBinary` / `LoadMapBinary` in `Program.Serialization.cs` |
| JSON | `.json` | `SaveMapJson` / `LoadMapJson` (legacy) |

`LoadMap(path)` auto-detects by extension. `LoadAllMapsFromFolder` prefers `.chmb` when both exist for the same name.

### Binary format (`.chmb`)

- 5-byte header: `CHMB` + version byte, then GZip-compressed `BinaryWriter` stream
- 2D arrays written as raw bytes/ints/doubles; enum arrays cast to `byte`
- Complex objects (`Config`, `Weather`, `Wave`, species lists) embedded as length-prefixed JSON strings
- `noise`, `tempatureNoise`, `humidityNoise` — **not saved**, regenerated via `RegenerateNoise()` on load
- `rng` — **not saved**, reconstructed as `new Random(map.seed)` on load
- `[JsonIgnore]` fields (`_framebuffer`, `tick`, swap buffers, gust state) — **not saved**

### JSON format (legacy)

Custom converters handle 2D arrays and typed enums:

| Converter | Handles |
|---|---|
| `TileIdArrayJsonConverter` | `TileId[,]` — rows as legacy char strings |
| `EntityIdArrayJsonConverter` | `EntityId[,]` |
| `CloudTypeArrayJsonConverter` | `CloudType[,]` |
| `Bool2DArrayJsonConverter` | `bool[,]` |
| `Int2DArrayJsonConverter` | `int[,]` |
| `Double2DArrayJsonConverter` | `double[,]` |

`RegenerateNoise()` is called on JSON load too — noise arrays in old saves are ignored.

---

## Command System

**Entry:** `ProcessCommand(string command)` (`Program.cs:1146`)  
Input is read via `ReadCommandWithAutocomplete()` (supports tab completion) after the user presses the command key.

### Commands

| Command | Description |
|---|---|
| `exit` | Save all, quit |
| `menu` | Return to save slot selection screen |
| `chamber <n>` | Switch displayed chamber to index n (1-based) |
| `addchamber [n]` | Open config GUI, generate, and add n new chambers (default 1) |
| `removechamber <n>` | Remove chamber n from active list |
| `regenerate <n>` | Re-run generation on chamber n |
| `clouds` | Toggle cloud rendering |
| `shadows` | Toggle cloud shadow rendering |
| `temperature` | Toggle temperature overlay |
| `humidity` | Toggle humidity overlay |
| `save` | Manually save all chambers |

Commands write feedback to `outputBuffer` which the GUI thread renders in the event log panel.

---

## Key Interactions Between Components

```
Program.Main()
  │
  ├─ LoadAllMapsFromFolder() ──→ JsonSerializer + custom converters → List<Map>
  │
  ├─ DrawSaveSelectionGUI()  ──→ GUI.DrawBox / DrawGrid / WriteAt
  │
  ├─ [user selects slot] ────→ Map.Generate()
  │                                │
  │                                ├─ Perlin.GeneratePerlinNoise() → noise[,]
  │                                ├─ AssignBiomes() → mapData[,]
  │                                ├─ CreateMountains() → AStar.FindPath() → mapData[,]
  │                                ├─ CreateRiver()    → AStar.FindPath() → mapData[,]
  │                                ├─ InitializeSpecies() → Crab/Turtle into overlayData[,]
  │                                └─ InitializeWeather() → weather, clouds
  │
  ├─ updateThread: Map.Update()
  │     ├─ Species.Behave() → Crab/Turtle move in overlayData[,]
  │     ├─ Cloud positions advance → cloudData[,]
  │     └─ Wave positions advance
  │
  ├─ weatherThread: Weather state machine advances
  │     └─ temperature, humidity, pressure, wind evolve over time
  │
  └─ guiThread: Map.DisplayGUI() + static panels
        ├─ GUI.DrawGrid(mapData) with ColorSpectrum colors
        ├─ GUI.DrawGrid(overlayData) on top
        ├─ GUI.DrawGrid(cloudData) if isCloudsRendering
        └─ GUI.WriteAt(actualOutputBuffer) at bottom
```

---

## Adding New Features

### New Species

1. Inherit `Species`, implement `Behave()`
2. Define `AllowedTiles` list
3. Call `InitializeSpecies(min, max, new YourAnimal(...))` in `HandleGen()`

### New Biome Type

1. Add to `Habitat` enum (`Config.cs:178`)
2. Add color constant to `ColorSpectrum`
3. Add tile char and cost to `AStar.TerrainConfig`
4. Handle the char in `GUI.DrawGrid` color dispatch

### New Weather Type

1. Add to `WeatherType` enum (`Weather.cs`)
2. Add state transition logic in `UpdateWeather()`
3. Add cloud initialization for the new type in `InitializeClouds()`

### New Generation Stage

1. Add a method to the appropriate `Map.*.cs` partial file
2. Call it from `HandleGen()` in `Map.cs`
3. Add a `Config` toggle if it should be optional

### New Command

1. Add a `case` to the `switch` in `ProcessCommand()` (`Program.cs:1149`)
2. Write feedback to `outputBuffer`
