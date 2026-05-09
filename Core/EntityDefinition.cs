using System.Collections.Generic;

public enum ActivityPattern { Diurnal, Nocturnal, Cathemeral }

public sealed class SpawnConfig
{
    public int  Min                 { get; init; }
    public int  Max                 { get; init; }
    public bool SpawnInHerds        { get; init; }
    public int  MinHerdCount        { get; init; } = 1;
    public int  MaxHerdCount        { get; init; } = 1;
    public int  MinHerdSize         { get; init; } = 1;
    public int  MaxHerdSize         { get; init; } = 1;
    public int  MinMountainDistance { get; init; }
}

public sealed class EntityDefinition
{
    public EntityId                      Id                 { get; init; }
    public string                        Name               { get; init; } = "";
    public char                          LegacyChar         { get; init; }
    public HashSet<TileId>               AllowedTiles       { get; init; } = [];
    public string                        Habitat            { get; init; } = "";
    public SpawnConfig                   Spawn              { get; init; } = new();
    public Func<int, int, int, Species>? Factory            { get; init; }
    public string                        Icon               { get; init; } = "  ";
    public (int r, int g, int b)         BaseColor          { get; init; }
    public int                           ColorVarianceRange { get; init; } = 0;

    // Ecosystem relationships
    public EntityId[]                    PreyIds            { get; init; } = [];
    public float                         FearRadius         { get; init; } = 0f;
    public float                         FearStrength       { get; init; } = 0f;

    // Food and hunger
    public HashSet<TileId>?              FoodTiles          { get; init; }         // null = predator; set = grazes these
    public float                         HungerRate         { get; init; } = 0f;   // per tick; 0 = no hunger
    public float                         StarvationThreshold{ get; init; } = 1.0f;
    public float                         GrazeRate          { get; init; } = 0.01f;// per tick when on food tile

    // Breeding
    public float                         BreedChance        { get; init; } = 0f;
    public int                           BreedCooldown      { get; init; } = 0;    // ticks between breeds
    public int                           BreedRadius        { get; init; } = 3;
    public int                           BreedSeason        { get; init; } = -1;   // -1 = any season
    public int                           PopMax             { get; init; } = 50;   // soft cap
    public int                           LitterMin          { get; init; } = 1;    // offspring per birth
    public int                           LitterMax          { get; init; } = 1;
    public float                         BreedPopulationPressure { get; init; } = 2f;
    public bool                          BreedRequiresMaturePartner { get; init; } = true;
    public int                           MinHerdSize            { get; init; } = 0;
    public int                           MaxHerdSize            { get; init; } = 0;

    // Activity and movement (evolution-mutable via init)
    public ActivityPattern               Activity           { get; init; } = ActivityPattern.Cathemeral;
    public ActivityPattern               HuntActivity       { get; init; } = ActivityPattern.Cathemeral;
    public float                         MoveChance         { get; init; } = 0.8f;
    public float                         NightMoveChance    { get; init; } = 0.5f;
    public int                           HuntScanRadius     { get; init; } = 0;   // 0 = not a hunter
    public HashSet<TileId>?              HuntAllowedTiles   { get; init; }         // null = same as AllowedTiles; set = extended terrain for hunting only
    public float                         HuntOrderWeight    { get; init; } = 0.35f;
    public float                         HuntHealthWeight   { get; init; } = 1.75f;
    public float                         HuntAgeWeight      { get; init; } = 1.15f;
    public float                         HuntDistanceWeight { get; init; } = 0.5f;
    public float                         HuntHealthTarget   { get; init; } = 0f;
    public float                         HuntAgeTarget      { get; init; } = 0.5f;
    public int                           MaxBoidSize        { get; init; } = 10;

    // Health
    public float MaxHealth          { get; init; } = 1.0f;
    public float AttackDamage       { get; init; } = 0f;
    public int   AttackCooldown     { get; init; } = 0;
    public float HealRate           { get; init; } = 0.001f;

    // Aging
    public int   MaturityAge        { get; init; } = 0;
    public int   MaxAge             { get; init; } = 0;
    public float AgeDeathVariance   { get; init; } = 0.1f;

    // Hardcoded behavior values — now data-driven
    public float HuntAttemptChance  { get; init; } = 0.3f;
    public float AnchorRadius       { get; init; } = 25f;
    public int   BoidCohesionRadius { get; init; } = 6;
    public float BoidSeparationDist { get; init; } = 2f;
    public int   HibernateSeason    { get; init; } = -1;
    public float HibernateChance    { get; init; } = 0f;
    public float SwimChance         { get; init; } = 0.05f;
    public int   RestMin            { get; init; } = 10;
    public int   RestMax            { get; init; } = 30;
}
