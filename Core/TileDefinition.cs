using System.Collections.Generic;

public sealed class TileDefinition
{
    public TileId   Id                             { get; init; }
    public string   Name                           { get; init; } = "";
    public char     LegacyChar                     { get; init; }
    public (int r, int g, int b) BaseColor         { get; init; }
    public float    MovementCost                   { get; init; } = 1.0f;
    public bool     IsBlocked                      => MovementCost >= float.MaxValue;
    public bool     IsWater                        { get; init; }
    public bool     IsLand                         { get; init; }
    // The depth variant of this tile — e.g. Ocean.DeepVariant = OceanShallow
    // Used by WaterDepth() to convert surrounded tiles in a single generic pass.
    public TileId?  DeepVariant                    { get; init; }
    // Creature category tags. Animals declare which tags they need (e.g. "beach").
    // Adding a new tile with the right tags automatically supports all matching creatures.
    public IReadOnlySet<string> CreatureCategories { get; init; } = new HashSet<string>();
    // Future fields (add here without touching any other file):
    // public bool  IsFlammable    { get; init; }
    // public float FertilityScore { get; init; }
    // public bool  IsNavigable    { get; init; }
    // public int   SpawnWeight    { get; init; }
}
