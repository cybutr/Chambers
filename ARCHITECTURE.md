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
├── Species.cs                        # Animal base class + Crab, Turtle, Boids
├── debug_console.cs                  # Debug utilities (minimal)
│
├── MapGeneration/
│   ├── Map.cs                        # Core Map entity, generation coordinator, partial class root
│   ├── PerlinNoise.cs                # Perlin + Simplex noise generators
│   ├── Cloud.cs                      # Cloud data model
│   ├── Wave.cs                       # Wave data model
│   ├── Weather.cs                    # Weather state + WeatherType/CloudType enums
│   └── Components/
│       ├── Map.Frame.cs              # Coastline/border generation (313 lines)
│       ├── Map.Mountains.cs          # Mountain ranges, snow caps, forest integration (634 lines)
│       ├── Map.WaterGen.cs           # Rivers, lakes, flow simulation (572 lines)
│       ├── Map.Waves.cs              # Wave rendering and animation (385 lines)
│       └── Map.Utils.cs              # Flood-fill, biome helpers, distance utils, A* wrappers (779 lines)
│
├── Other/
│   ├── Config.cs                     # Config class (70+ params), GUIConfig, Habitat enum, Biome class
│   ├── GUI.cs                        # Thread-safe console rendering wrapper
│   ├── Characters.cs                 # ASCII art sprites for A-Z, 0-9
│   ├── ColorSpectrum.cs              # 100+ named RGB color constants
│   └── AStar.cs                      # A* pathfinding with organic styling (763 lines)
│
└── Saves/                            # JSON world persistence files
    └── *.json                        # One file per world (named by Config.Name)
```

`Map` is a `partial class` split across `Map.cs` and all five `Components/` files — they all compile into one class.

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
| `mapData` | `char[,]` | `width × height` | Primary terrain layer |
| `overlayData` | `char[,]` | `width × height` | Animals/NPCs drawn on top |
| `previousMapData` | `char[,]` | `width × height` | Last frame snapshot (dirty-checking) |
| `previousOverlayData` | `char[,]` | `width × height` | Last frame overlay snapshot |
| `cloudData` | `char[,]` | `cloudDataWidth × cloudDataHeight` | Cloud visual layer (3× map size) |
| `previousCloudData` | `char[,]` | same | Last frame cloud snapshot |
| `cloudDepthData` | `int[,]` | same | Visual depth per cloud tile |
| `precipitationData` | `double[,]` | same | Rainfall intensity 0.0–1.0 |
| `temperatureData` | `int[,]` | `width × height` | Temperature per tile (0–100) |
| `humidityData` | `int[,]` | `width × height` | Humidity per tile (0–100) |
| `noise` | `double[,]` | `width × height` | Raw Perlin height values |
| `tempatureNoise` | `double[,]` | `width × height` | Perlin temperature values |
| `humidityNoise` | `double[,]` | `width × height` | Perlin humidity values |

**Cloud data** is 3× the map size (`cloudDataWidth = width * 3`) so clouds can scroll off-screen and re-enter from the other side without being clipped.

### Key Scalar Properties

```csharp
public double time;                  // Simulation tick counter
public bool shouldSimulationContinue;// Per-map pause flag
public bool isCloudsRendering;
public bool isCloudsShadowsRendering;
public int cloudShadowOffsetX/Y;     // Shadow parallax offset
public double avarageTempature;
public double avarageHumidity;
public Config conf;                  // All generation parameters
public Weather weather;              // Current climate state
public List<Cloud> clouds;           // Active cloud entities
public List<Wave> waves;             // Active wave entities
public List<string> actualOutputBuffer; // Per-map event log
```

---

## Tile System

All terrain is stored as a single `char` per cell. Uppercase = primary biome, lowercase = depth/variant.

### Terrain Tiles (`mapData`)

| Char | Biome | A* Cost | Notes |
|---|---|---|---|
| `'P'` | Plains | 0.8 | Default land |
| `'F'` | Forest | 0.2 | Easiest to traverse |
| `'M'` | Mountain peak | 10.0 | High cost |
| `'m'` | Mountain depth | 15.0 | Interior mountain (8 mountain neighbors) |
| `'O'` | Ocean deep | 5.0 | |
| `'o'` | Ocean shallow | 6.0 | |
| `'R'` | River | 4.0 | |
| `'r'` | River shallow | 3.0 | |
| `'L'` | Lake | 4.0 | |
| `'l'` | Lake shallow | 3.0 | |
| `'B'` | Beach | 0.5 | Transition land↔water |
| `'b'` | Beach dark | 0.7 | |
| `'S'` | Snow | 20.0 | Mountain tops |
| `'s'` | Snow shallow | 3.0 | |
| `'@'` | Border | blocked | Map edge, impassable |
| `' '` | Empty | — | No render |

### Overlay Tiles (`overlayData`)

| Char | Entity |
|---|---|
| `'W'`/`'w'` | Wolf (predator) |
| `'C'`/`'c'` | Cow (herbivore) |
| `'S'`/`'s'` | Sheep (herbivore) |
| Crab/Turtle | Rendered via species position |

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

### `Weather` class (`MapGeneration/Weather.cs:1`)

```csharp
WeatherType CurrentWeather   // active weather
WeatherType NextWeather      // transitioning to
double Intensity             // 0.0–1.0 strength
double IntensityTarget       // lerp target
double IntensityChangeSpeed  // lerp rate
double Temperature
double Humidity
double Pressure
double WindSpeed
double WindDirection
double TimeOfDay             // 0.0–24.0
double Season                // 0.0–4.0 (seasons)
```

### `WeatherType` enum

```
Clear=1, Rain=2, Snow=3, Thunderstorm=4, Fog=5, Overcast=6,
Hail=7, Sleet=8, Drizzle=9, BlowingSnow=10, Sandstorm=11
```

### `CloudType` enum

```
Cumulus=1, Stratus=2, Cirrus=3, Cumulonimbus=4, Nimbostratus=5, Altocumulus=6
```

### `Cloud` class (`MapGeneration/Cloud.cs`)

Per-cloud data: position, speed, direction, precipitation intensity, `CloudType`.  
Cloud layer is `3× map size` so clouds scroll continuously. Shadow offset (`cloudShadowOffsetX/Y`) creates a parallax shadow underneath each cloud.

### `Wave` class (`MapGeneration/Wave.cs`)

Per-wave data: list of points, direction, speed, curvature, intensity.  
Rendered in `Map.Waves.cs` — wave positions animate each tick, creating rolling ocean patterns.

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
2. Map grid — DrawGrid() with per-cell color from mapData tile type
3. Overlay grid — animals from overlayData on top
4. Cloud layer — cloudData with depth shading
5. Shadow layer — cloud shadows if isCloudsShadowsRendering
6. Wave animation — water surface patterns
7. Event log — actualOutputBuffer scrolled at bottom
```

**Platform detection:** `RuntimeInformation.IsOSPlatform(OSPlatform.Linux)` gates box-drawing character choice throughout `GUI.DrawBox()`.

---

## Save / Load System

### Custom JSON Converters (`Program.cs:263+`)

Standard `System.Text.Json` can't serialize 2D arrays or `(int,int)` tuple keys. Five custom converters handle this:

| Converter | Handles | Encoding |
|---|---|---|
| `Char2DArrayJsonConverter` | `char[,]` | Each row → one string in `List<string>` |
| `Bool2DArrayJsonConverter` | `bool[,]` | Each row → string of `'1'`/`'0'` chars |
| `Int2DArrayJsonConverter` | `int[,]` | Each row → string of `(value)` tuples |
| `Double2DArrayJsonConverter` | `double[,]` | Similar encoding |
| `ValueTupleIntKeyConverter` | `Dictionary<(int,int),T>` | Keys serialized as `"x,y"` strings |

All converters are registered in `JsonSerializerOptions` before every `Serialize`/`Deserialize` call.

### `SaveMap(Map map)` (`Program.cs:509`)

```csharp
string path = Path.Combine("Saves", map.conf.Name + ".json");
var options = new JsonSerializerOptions { WriteIndented = true };
// register all 5 converters
string json = JsonSerializer.Serialize(map, options);
File.WriteAllText(path, json);
```

**Triggers:** On `exit` command, on `Ctrl+C` (`OnExit` handler), and when `conf.ShouldSave == true` after slot operations.

### `LoadAllMapsFromFolder(string folder)` (`Program.cs:707`)

```csharp
foreach .json file in folder:
    string json = File.ReadAllText(file)
    Map map = JsonSerializer.Deserialize<Map>(json, options)
    allChambers.Add(map)
```

After loading, `SynchronizeAllChamberFiles()` renames any files whose names don't match their `conf.Name`.

### What is persisted

Every field on `Map` marked as a property with `{ get; set; }` is serialized — this includes all 2D arrays, all scalars, `conf` (full Config), `weather`, `wavePositions`, `actualOutputBuffer`, and all boolean flags.

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
