using System;
using System.Collections.Generic;
using System.Linq;

public class HerdManager
{
    public Dictionary<int, Herd> Herds  { get; set; } = [];
    public int                   NextId { get; set; } = 0;

    public Herd? GetHerd(int id) => Herds.TryGetValue(id, out var h) ? h : null;

    public Herd CreateSingletonHerd(Species s, EntityDefinition def, Random rng)
    {
        int cap = rng.Next(def.MinHerdSize, def.MaxHerdSize + 1);
        var herd = new Herd
        {
            Id         = NextId++,
            Species    = s.EntityId,
            Count      = 1,
            SizeCap    = cap,
            FounderIds = [s.seedOffset],
            Genes      = SeedGenes(rng),
        };
        Herds[herd.Id] = herd;
        s.HerdId = herd.Id;
        return herd;
    }

    public void AddMember(Species s, int herdId)
    {
        if (!Herds.TryGetValue(herdId, out var herd)) return;
        s.HerdId = herdId;
        herd.Count++;
    }

    public void RemoveMember(Species s)
    {
        if (s.HerdId == -1) return;
        if (Herds.TryGetValue(s.HerdId, out var herd))
        {
            herd.Count--;
            if (herd.Count <= 0) Herds.Remove(herd.Id);
        }
        s.HerdId = -1;
    }

    public void RebuildFromSpecies(List<Species> all)
    {
        foreach (var h in Herds.Values) h.Count = 0;
        foreach (var s in all)
            if (s.HerdId != -1 && Herds.TryGetValue(s.HerdId, out var h))
                h.Count++;
        foreach (var id in Herds.Keys.Where(k => Herds[k].Count == 0).ToList())
            Herds.Remove(id);
    }

    public void UpdateAll(List<Species> all, Species?[,] grid, int w, int h,
                          Func<EntityId, EntityDefinition> getDef, Random rng)
    {
        var merges = new HashSet<(int a, int b)>();
        foreach (var s in all)
        {
            if (s.HerdId == -1) continue;
            var def = getDef(s.EntityId);
            int r = def.BoidCohesionRadius > 0 ? def.BoidCohesionRadius : def.BreedRadius * 2;
            if (r <= 0) continue;

            int x1 = Math.Max(0, s.X - r), x2 = Math.Min(w - 1, s.X + r);
            int y1 = Math.Max(0, s.Y - r), y2 = Math.Min(h - 1, s.Y + r);
            for (int x = x1; x <= x2; x++)
                for (int y = y1; y <= y2; y++)
                {
                    var n = grid[x, y];
                    if (n == null || n.HerdId == s.HerdId || n.HerdId == -1) continue;
                    if (n.EntityId != s.EntityId) continue;
                    int a = Math.Min(s.HerdId, n.HerdId);
                    int b = Math.Max(s.HerdId, n.HerdId);
                    merges.Add((a, b));
                }
        }

        foreach (var (a, b) in merges)
        {
            if (!Herds.TryGetValue(a, out var ha) || !Herds.TryGetValue(b, out var hb)) continue;
            var (keep, lose) = ha.Count >= hb.Count ? (ha, hb) : (hb, ha);
            foreach (var s2 in all)
                if (s2.HerdId == lose.Id) s2.HerdId = keep.Id;
            keep.Count += lose.Count;
            Herds.Remove(lose.Id);
        }

        foreach (var herd in Herds.Values.ToList())
            if (herd.Count > herd.SizeCap) SplitHerd(herd, all, getDef, rng);
    }

    private void SplitHerd(Herd herd, List<Species> all, Func<EntityId, EntityDefinition> getDef, Random rng)
    {
        var members = all.Where(s => s.HerdId == herd.Id).ToList();
        int excess  = members.Count - herd.SizeCap;
        if (excess <= 0) return;

        float cx = 0f, cy = 0f;
        foreach (var m in members) { cx += m.X; cy += m.Y; }
        cx /= members.Count; cy /= members.Count;

        var split = members
            .OrderByDescending(s => (s.X - cx) * (s.X - cx) + (s.Y - cy) * (s.Y - cy))
            .Take(excess)
            .ToList();

        var def    = getDef(herd.Species);
        int newCap = def.MinHerdSize > 0
            ? rng.Next(def.MinHerdSize, def.MaxHerdSize + 1)
            : excess;

        var newHerd = new Herd
        {
            Id         = NextId++,
            Species    = herd.Species,
            Count      = excess,
            SizeCap    = newCap,
            Generation = herd.Generation + 1,
            FounderIds = split.Select(s => s.seedOffset).ToList(),
            Genes      = new Dictionary<string, float>(herd.Genes),
        };
        Herds[newHerd.Id] = newHerd;

        foreach (var s in split) s.HerdId = newHerd.Id;
        herd.Count -= excess;
    }

    private static Dictionary<string, float> SeedGenes(Random rng) => new()
    {
        ["Aggression"]        = rng.NextSingle(),
        ["MigrationUrge"]     = rng.NextSingle(),
        ["FeedingEfficiency"] = rng.NextSingle(),
        ["HerdLoyalty"]       = rng.NextSingle(),
    };

    public string GetSummary()
    {
        if (Herds.Count == 0) return "No herds";
        var groups = Herds.Values.GroupBy(h => h.Species).OrderBy(g => g.Key);
        return string.Join(" | ", groups.Select(g =>
            $"{g.Key}: {g.Count()} herds (avg {g.Average(h => h.Count):F1}/{g.Average(h => h.SizeCap):F0})"));
    }
}
