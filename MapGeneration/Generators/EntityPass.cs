public class EntityPass : IGenerationPass
{
    public string Name => "EntityPass";

    public void Execute(GenerationContext ctx)
    {
        #region entity seeding
        foreach (EntityDefinition def in EntityRegistry.Spawnable())
            SeedEntity(ctx, def);
        #endregion
    }

    private void SeedEntity(GenerationContext ctx, EntityDefinition def)
    {
        if (def.AllowedTiles.Count == 0) return;

        int w = ctx.width, h = ctx.height;
        List<(int x, int y)> candidates = [];

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                TileId t = ctx.mapData[x, y];
                if (t == TileId.Border) continue;
                if (ctx.overlayData[x, y] != EntityId.None) continue;
                if (!def.AllowedTiles.Contains(t)) continue;
                candidates.Add((x, y));
            }
        }

        if (candidates.Count == 0) return;

        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = ctx.rng.Next(i + 1);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }

        int count = ctx.rng.Next(def.Spawn.Min, def.Spawn.Max + 1);
        int place = Math.Min(count, candidates.Count);
        for (int i = 0; i < place; i++)
            ctx.overlayData[candidates[i].x, candidates[i].y] = def.Id;
    }
}
