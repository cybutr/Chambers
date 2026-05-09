using System.Collections.Generic;
using System.Linq;

public static class EntityRegistry
{
    private static readonly EntityDefinition[] _defs = new EntityDefinition[11];
    private static readonly Dictionary<char, EntityId> _byChar = new();

    static EntityRegistry() => Reload();

    public static void Reload()
    {
        System.Array.Clear(_defs, 0, _defs.Length);
        _byChar.Clear();

        Register(new EntityDefinition {
            Id = EntityId.Crab, Name = "Crab", LegacyChar = 'c',
            Habitat = "Beach",
            AllowedTiles = [TileId.Beach, TileId.BeachDark],
            Spawn = new SpawnConfig { Min = 3, Max = 6 },
            Factory = (x, y, seed) => new Crab(x, y, seed),
            BaseColor = ColorSpectrum.ParseDynamic("crab"),
            Icon = "󰨰 ",
            Activity = ActivityPattern.Cathemeral,
            MoveChance = 0.7f, NightMoveChance = 0.4f,
            BreedChance = 0.002f, BreedCooldown = 1800, BreedRadius = 2, PopMax = 25,
            BreedPopulationPressure = 3.0f, MinHerdSize = 6, MaxHerdSize = 12,
            MaxHealth = 0.8f, HealRate = 0.003f,
            MaturityAge = 200, MaxAge = 6000, AgeDeathVariance = 0.2f,
            LitterMin = 2, LitterMax = 4,
        });
        Register(new EntityDefinition {
            Id = EntityId.Turtle, Name = "Turtle", LegacyChar = 'T',
            Habitat = "Beach",
            AllowedTiles = [TileId.Beach, TileId.BeachDark],
            Spawn = new SpawnConfig { Min = 2, Max = 4 },
            Factory = (x, y, seed) => new Turtle(x, y, seed),
            BaseColor = ColorSpectrum.ParseDynamic("turtle"),
            Icon = "󰳗 ",
            Activity = ActivityPattern.Cathemeral,
            MoveChance = 0.3f, NightMoveChance = 0.1f,
            BreedChance = 0.001f, BreedCooldown = 2400, BreedRadius = 2, PopMax = 15,
            BreedPopulationPressure = 3.25f,
            MaxHealth = 1.5f, HealRate = 0.001f,
            MaturityAge = 400, MaxAge = 24000, AgeDeathVariance = 0.1f,
            LitterMin = 1, LitterMax = 3,
            SwimChance = 0.05f, RestMin = 20, RestMax = 60,
        });
        Register(new EntityDefinition {
            Id = EntityId.Cow, Name = "Cow", LegacyChar = 'C',
            Habitat = "Plains",
            AllowedTiles = [TileId.Plains],
            Spawn = new SpawnConfig {
                SpawnInHerds = true,
                Min = 1, Max = 4,
                MinHerdCount = 2, MaxHerdCount = 3,
                MinHerdSize = 2, MaxHerdSize = 4,
                MinMountainDistance = 5
            },
            Factory = (x, y, seed) => new Cow(x, y, seed),
            BaseColor = ColorSpectrum.ParseDynamic("cow"),
            Icon = "󰆚 ",
            Activity = ActivityPattern.Diurnal,
            MoveChance = 0.7f, NightMoveChance = 0f,
            FoodTiles = [TileId.Plains],
            HungerRate = 0.002f, GrazeRate = 0.008f, StarvationThreshold = 1.0f,
            BreedChance = 0.002f, BreedCooldown = 1800, BreedRadius = 3, PopMax = 30, MinHerdSize = 6, MaxHerdSize = 10,
            LitterMin = 1, LitterMax = 2,
            MaxHealth = 1.5f, HealRate = 0.0015f,
            MaturityAge = 400, MaxAge = 12000, AgeDeathVariance = 0.15f,
            AnchorRadius = 25f,
        });
        Register(new EntityDefinition {
            Id = EntityId.Sheep, Name = "Sheep", LegacyChar = 'S',
            Habitat = "Plains",
            AllowedTiles = [TileId.Plains],
            Spawn = new SpawnConfig {
                SpawnInHerds = true,
                Min = 1, Max = 3,
                MinHerdCount = 2, MaxHerdCount = 3,
                MinHerdSize = 1, MaxHerdSize = 3,
                MinMountainDistance = 5
            },
            Factory = (x, y, seed) => new Sheep(x, y, seed),
            BaseColor = ColorSpectrum.ParseDynamic("sheep"),
            Icon = "󰳆 ",
            Activity = ActivityPattern.Diurnal,
            MoveChance = 0.7f, NightMoveChance = 0f,
            FoodTiles = [TileId.Plains],
            HungerRate = 0.002f, GrazeRate = 0.009f, StarvationThreshold = 1.0f,
            BreedChance = 0.003f, BreedCooldown = 1500, BreedRadius = 3, PopMax = 40, MinHerdSize = 8, MaxHerdSize = 14,
            LitterMin = 1, LitterMax = 2,
            MaxHealth = 1.0f, HealRate = 0.002f,
            MaturityAge = 300, MaxAge = 10000, AgeDeathVariance = 0.2f,
            AnchorRadius = 25f,
        });
        Register(new EntityDefinition {
            Id = EntityId.Wolf, Name = "Wolf", LegacyChar = 'W',
            Habitat = "Forest",
            AllowedTiles = [TileId.Forest, TileId.Plains],
            Spawn = new SpawnConfig { Min = 1, Max = 3 },
            Factory = (x, y, seed) => new Wolf(x, y, seed),
            BaseColor = ColorSpectrum.ParseDynamic("wolf"),
            Icon = "󰩃 ",
            PreyIds = [EntityId.Sheep, EntityId.Cow, EntityId.Goat],
            FearRadius = 15f, FearStrength = 1.0f,
            Activity = ActivityPattern.Cathemeral,
            HuntActivity = ActivityPattern.Nocturnal,
            MoveChance = 0.8f, NightMoveChance = 0.9f,
            AnchorRadius = 30f,
            HuntScanRadius = 15,
            HuntAllowedTiles = [TileId.Forest, TileId.Plains, TileId.Mountain, TileId.MountainDeep, TileId.Snow],
            HuntOrderWeight = 0.35f, HuntHealthWeight = 1.75f, HuntAgeWeight = 1.0f, HuntDistanceWeight = 0.5f,
            HuntHealthTarget = 0f, HuntAgeTarget = 0.2f,
            HungerRate = 0.0002f, StarvationThreshold = 1.0f,
            BreedChance = 0.001f, BreedCooldown = 3600, BreedRadius = 4, BreedSeason = 0, PopMax = 12, MinHerdSize = 3, MaxHerdSize = 6,
            LitterMin = 1, LitterMax = 2,
            MaxHealth = 1.2f, AttackDamage = 0.4f, AttackCooldown = 80, HealRate = 0.001f,
            MaturityAge = 500, MaxAge = 16000, AgeDeathVariance = 0.15f,
            HuntAttemptChance = 0.8f,
        });
        Register(new EntityDefinition {
            Id = EntityId.Bear, Name = "Bear", LegacyChar = 'B',
            Habitat = "Forest",
            AllowedTiles = [TileId.Forest, TileId.Plains],
            Spawn = new SpawnConfig { Min = 1, Max = 2 },
            Factory = (x, y, seed) => new Bear(x, y, seed),
            BaseColor = ColorSpectrum.ParseDynamic("bear"),
            Icon = "󱣻 ",
            PreyIds = [EntityId.Sheep, EntityId.Cow, EntityId.Fish, EntityId.Crab, EntityId.Goat],
            FearRadius = 8f, FearStrength = 0.5f,
            Activity = ActivityPattern.Cathemeral,
            MoveChance = 0.5f, NightMoveChance = 0.3f,
            HuntScanRadius = 12,
            HuntAllowedTiles = [TileId.Forest, TileId.Plains, TileId.OceanShallow, TileId.River, TileId.RiverShallow, TileId.Lake, TileId.LakeShallow],
            HuntOrderWeight = 0.3f, HuntHealthWeight = 1.5f, HuntAgeWeight = 0.8f, HuntDistanceWeight = 0.45f,
            HuntHealthTarget = 0.1f, HuntAgeTarget = 0.5f,
            HungerRate = 0.00015f, StarvationThreshold = 1.0f,
            BreedChance = 0.0005f, BreedCooldown = 4800, BreedRadius = 4, BreedSeason = 0, PopMax = 10,
            LitterMin = 1, LitterMax = 2,
            MaxHealth = 2.0f, AttackDamage = 0.5f, AttackCooldown = 120, HealRate = 0.0008f,
            MaturityAge = 600, MaxAge = 20000, AgeDeathVariance = 0.1f,
            HuntAttemptChance = 0.3f,
            HibernateSeason = 3, HibernateChance = 0.95f,
        });
        Register(new EntityDefinition {
            Id = EntityId.Goat, Name = "Goat", LegacyChar = 'G',
            Habitat = "Mountain",
            AllowedTiles = [TileId.Mountain, TileId.MountainDeep],
            Spawn = new SpawnConfig { Min = 1, Max = 3 },
            Factory = (x, y, seed) => new Goat(x, y, seed),
            BaseColor = ColorSpectrum.ParseDynamic("goat"),
            Icon = "󰳆 ",
            Activity = ActivityPattern.Diurnal,
            MoveChance = 0.6f, NightMoveChance = 0.1f,
            FoodTiles = [TileId.Mountain, TileId.MountainDeep, TileId.Plains, TileId.Snow],
            HungerRate = 0.0015f, GrazeRate = 0.007f, StarvationThreshold = 1.0f,
            BreedChance = 0.002f, BreedCooldown = 2100, BreedRadius = 3, PopMax = 20, MinHerdSize = 4, MaxHerdSize = 8,
            MaxHealth = 1.0f, HealRate = 0.002f,
            MaturityAge = 300, MaxAge = 11000, AgeDeathVariance = 0.2f,
            LitterMin = 1, LitterMax = 2,
        });
        Register(new EntityDefinition {
            Id = EntityId.Fish, Name = "Fish", LegacyChar = 'F',
            Habitat = "Water",
            AllowedTiles = [TileId.Ocean, TileId.OceanShallow, TileId.River, TileId.RiverShallow, TileId.Lake, TileId.LakeShallow],
            Spawn = new SpawnConfig {
                Min = 2, Max = 5,
                SpawnInHerds = true,
                MinHerdCount = 1, MaxHerdCount = 2,
                MinHerdSize = 2, MaxHerdSize = 5
            },
            Factory = (x, y, seed) => new Fish(x, y, seed),
            BaseColor = ColorSpectrum.ParseDynamic("fish"), ColorVarianceRange = 30,
            Icon = "󰈺 ",
            Activity = ActivityPattern.Cathemeral,
            MoveChance = 0.9f, NightMoveChance = 0.7f,
            BreedChance = 0.008f, BreedCooldown = 1200, BreedRadius = 4, PopMax = 80, MinHerdSize = 10, MaxHerdSize = 20,
            LitterMin = 1, LitterMax = 3,
            MaxHealth = 0.5f, HealRate = 0.005f,
            MaturityAge = 160, MaxAge = 5000, AgeDeathVariance = 0.3f,
            BoidCohesionRadius = 6, BoidSeparationDist = 2f, MaxBoidSize = 10,
        });
        Register(new EntityDefinition {
            Id = EntityId.Bird, Name = "Bird", LegacyChar = 'A',
            Habitat = "Air",
            AllowedTiles = [],
            Spawn = new SpawnConfig { Min = 10, Max = 20 },
            Factory = (x, y, seed) => new Bird(x, y, seed),
            BaseColor = ColorSpectrum.ParseDynamic("bird"), ColorVarianceRange = 20,
            Icon = "󱗆 ",
            Activity = ActivityPattern.Diurnal,
            MoveChance = 0.85f, NightMoveChance = 0.1f,
            PreyIds = [EntityId.Crab, EntityId.Fish],
            HuntActivity = ActivityPattern.Diurnal,
            HuntScanRadius = 12,
            HuntAttemptChance = 0.25f,
            HuntOrderWeight = 0.2f, HuntHealthWeight = 1.2f, HuntAgeWeight = 0.9f, HuntDistanceWeight = 0.6f,
            HuntHealthTarget = 0f, HuntAgeTarget = 0.25f,
            BreedChance = 0.001f, BreedCooldown = 3000, BreedRadius = 5, PopMax = 20, MinHerdSize = 8, MaxHerdSize = 15,
            MaxHealth = 0.5f, HealRate = 0.005f,
            MaturityAge = 200, MaxAge = 6000, AgeDeathVariance = 0.2f,
            LitterMin = 1, LitterMax = 3,
            BoidCohesionRadius = 8, BoidSeparationDist = 3f,
        });
        Register(new EntityDefinition {
            Id = EntityId.Villager, Name = "Villager", LegacyChar = 'V',
            Habitat = "Plains",
            AllowedTiles = [TileId.Plains],
            Spawn = new SpawnConfig { Min = 0, Max = 0 },
            Factory = null,
            BaseColor = ColorSpectrum.ParseDynamic("villager"),
            Icon = "󰋧 ",
        });
    }

    private static void Register(EntityDefinition def)
    {
        _defs[(int)def.Id] = def;
        if (def.LegacyChar != '\0') _byChar[def.LegacyChar] = def.Id;
    }

    public static EntityDefinition              Get(EntityId id) => _defs[(int)id];
    public static EntityId                      FromChar(char c) => _byChar.TryGetValue(c, out var id) ? id : EntityId.None;
    public static IEnumerable<EntityDefinition> Spawnable()      => _defs.Where(d => d?.Spawn.Max > 0);
}
