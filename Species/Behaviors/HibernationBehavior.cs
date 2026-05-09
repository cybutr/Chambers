using System.Collections.Generic;

public sealed class HibernationBehavior
{
    public int              TriggerSeason   { get; init; } = 3;
    public HashSet<TileId>? PreferredTiles  { get; init; }
    public float            HibernateChance { get; init; } = 1f;

    public bool IsHibernating(in SimContext ctx, TileId currentTile, System.Random rng) =>
        ctx.Season == TriggerSeason &&
        (PreferredTiles == null || PreferredTiles.Contains(currentTile)) &&
        rng.NextDouble() < HibernateChance;

    public bool ShouldSeekHibernationSpot(in SimContext ctx, TileId currentTile) =>
        ctx.Season == TriggerSeason &&
        PreferredTiles != null &&
        !PreferredTiles.Contains(currentTile);
}
