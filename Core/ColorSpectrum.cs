using System;

public static class ColorSpectrum
{
    #region atmospheric
    // Cloud depth variations
    public static readonly (int r, int g, int b) CIRRUS_DEPTH_LIGHT = (230, 230, 255);
    public static readonly (int r, int g, int b) CIRRUS_DEPTH_MEDIUM = (200, 200, 235);
    public static readonly (int r, int g, int b) CIRRUS_DEPTH_DARK = (170, 170, 215);

    public static readonly (int r, int g, int b) ALTOCUMULUS_DEPTH_LIGHT = (220, 220, 250);
    public static readonly (int r, int g, int b) ALTOCUMULUS_DEPTH_MEDIUM = (190, 190, 230);
    public static readonly (int r, int g, int b) ALTOCUMULUS_DEPTH_DARK = (160, 160, 210);

    public static readonly (int r, int g, int b) CUMULUS_DEPTH_LIGHT = (255, 255, 255);
    public static readonly (int r, int g, int b) CUMULUS_DEPTH_MEDIUM = (220, 220, 220);
    public static readonly (int r, int g, int b) CUMULUS_DEPTH_DARK = (190, 190, 190);

    public static readonly (int r, int g, int b) CUMULONIMBUS_DEPTH_LIGHT = (240, 240, 240);
    public static readonly (int r, int g, int b) CUMULONIMBUS_DEPTH_MEDIUM = (200, 200, 200);
    public static readonly (int r, int g, int b) CUMULONIMBUS_DEPTH_DARK = (160, 160, 160);

    public static readonly (int r, int g, int b) NIMBOSTRATUS_DEPTH_LIGHT = (210, 210, 220);
    public static readonly (int r, int g, int b) NIMBOSTRATUS_DEPTH_MEDIUM = (180, 180, 190);
    public static readonly (int r, int g, int b) NIMBOSTRATUS_DEPTH_DARK = (150, 150, 160);

    public static readonly (int r, int g, int b) STRATUS_DEPTH_LIGHT = (200, 200, 210);
    public static readonly (int r, int g, int b) STRATUS_DEPTH_MEDIUM = (170, 170, 180);
    public static readonly (int r, int g, int b) STRATUS_DEPTH_DARK = (140, 140, 150);

    // Weather / Time tints
    public static readonly (int r, int g, int b) SKY_BLUE = (135, 206, 235);
    public static readonly (int r, int g, int b) NIGHT_TINT = (20, 20, 60);
    #endregion

    #region terrain
    // Land
    public static readonly (int r, int g, int b) PLAINS_GREEN = (148, 191, 115);
    public static readonly (int r, int g, int b) FOREST_GREEN = (82, 106, 64);
    public static readonly (int r, int g, int b) MOUNTAIN_GREY = (108, 117, 125);
    public static readonly (int r, int g, int b) MOUNTAIN_DEEP = (73, 80, 87);
    public static readonly (int r, int g, int b) SNOW_WHITE = (225, 228, 232);
    public static readonly (int r, int g, int b) SAND_YELLOW = (240, 218, 165);
    public static readonly (int r, int g, int b) SAND_DARK = (229, 193, 133);

    // Water & Waves
    public static readonly (int r, int g, int b) WATER_BLUE = (117, 147, 175);
    public static readonly (int r, int g, int b) WATER_DEEP = (71, 111, 149);
    public static readonly (int r, int g, int b) FOAMY_BLUE = (0, 134, 179);

    public static readonly (int r, int g, int b) WAVE_1 = (71, 111, 149);
    public static readonly (int r, int g, int b) WAVE_2 = (82, 120, 155);
    public static readonly (int r, int g, int b) WAVE_3 = (93, 129, 161);
    public static readonly (int r, int g, int b) WAVE_4 = (105, 138, 168);
    public static readonly (int r, int g, int b) WAVE_5 = (117, 147, 175);
    #endregion

    #region entities
    public static readonly (int r, int g, int b) CRAB_CRIMSON = (220, 20, 60);
    public static readonly (int r, int g, int b) TURTLE_GREEN = (82, 106, 64);
    public static readonly (int r, int g, int b) COW_BROWN = (139, 69, 19);
    public static readonly (int r, int g, int b) SHEEP_SLATE = (112, 128, 144);
    public static readonly (int r, int g, int b) WOLF_GREY = (73, 80, 87);
    public static readonly (int r, int g, int b) BEAR_SIENNA = (160, 82, 45);
    public static readonly (int r, int g, int b) GOAT_WHEAT = (245, 222, 179);
    public static readonly (int r, int g, int b) BIRD_SKY = (100, 149, 237); // Cornflower Blue
    public static readonly (int r, int g, int b) VILLAGER_PEACH = (255, 228, 196);
    #endregion

    #region standard
    // Aliases for Registry compatibility
    public static readonly (int r, int g, int b) NORMAL = (255, 228, 196);
    public static readonly (int r, int g, int b) RED = (178, 34, 34);
    public static readonly (int r, int g, int b) GREEN = (148, 191, 115);
    public static readonly (int r, int g, int b) DARK_GREEN = (82, 106, 64);
    public static readonly (int r, int g, int b) YELLOW = (240, 218, 165);
    public static readonly (int r, int g, int b) BLUE = (117, 147, 175);
    public static readonly (int r, int g, int b) DARK_GREY = (108, 117, 125);
    public static readonly (int r, int g, int b) GREY = (73, 80, 87);
    public static readonly (int r, int g, int b) WHITE = (225, 228, 232);
    public static readonly (int r, int g, int b) SILVER = (192, 192, 192);
    public static readonly (int r, int g, int b) BROWN = (139, 69, 19);
    public static readonly (int r, int g, int b) BRIGHT_RED = (255, 0, 0);
    public static readonly (int r, int g, int b) DARK_YELLOW = (229, 193, 133);
    public static readonly (int r, int g, int b) DARKER_BLUE = (71, 111, 149);
    public static readonly (int r, int g, int b) WHEAT = (245, 222, 179);
    public static readonly (int r, int g, int b) SIENNA = (160, 82, 45);
    public static readonly (int r, int g, int b) SLATE_GREY = (112, 128, 144);
    public static readonly (int r, int g, int b) LIGHT_BLUE = (173, 216, 230);
    public static readonly (int r, int g, int b) ANTIQUE_WHITE = (250, 235, 215);
    public static readonly (int r, int g, int b) LIGHT_YELLOW = (255, 255, 224);
    public static readonly (int r, int g, int b) BLUE_VIOLET = (138, 43, 226);
    public static readonly (int r, int g, int b) PURPLE = (128, 0, 128);
    public static readonly (int r, int g, int b) ORANGE = (255, 165, 0);
    public static readonly (int r, int g, int b) LIGHT_GREY = (211, 211, 211);
    public static readonly (int r, int g, int b) LIGHT_PINK = (255, 182, 193);
    public static readonly (int r, int g, int b) CRIMSON = (220, 20, 60);

    // Added for UI compatibility
    public static readonly (int r, int g, int b) BLACK = (0, 0, 0);
    public static readonly (int r, int g, int b) CYAN = (60, 179, 113);
    public static readonly (int r, int g, int b) LIGHT_CYAN = (224, 255, 255);
    public static readonly (int r, int g, int b) LIGHT_GREEN = (144, 238, 144);
    public static readonly (int r, int g, int b) BEIGE = (245, 245, 220);
    public static readonly (int r, int g, int b) MAGENTA = (199, 21, 133);
    public static readonly (int r, int g, int b) PINK = (255, 192, 203);
    public static readonly (int r, int g, int b) INDIAN_RED = (205, 92, 92);
    public static readonly (int r, int g, int b) BURNT_ORANGE = (204, 85, 0);
    public static readonly (int r, int g, int b) LIGHT_CORAL = (240, 128, 128);
    public static readonly (int r, int g, int b) PALE_TURQUOISE = (175, 238, 238);
    public static readonly (int r, int g, int b) LIGHT_STEEL_BLUE = (176, 196, 222);

    // Cloud types
    public static readonly (int r, int g, int b) CIRRUS = (225, 228, 232);
    public static readonly (int r, int g, int b) ALTOCUMULUS = (225, 229, 242);
    public static readonly (int r, int g, int b) CUMULUS = (233, 236, 239);
    public static readonly (int r, int g, int b) CUMULONIMBUS = (84, 90, 97);
    public static readonly (int r, int g, int b) NIMBOSTRATUS = (237, 242, 251);
    public static readonly (int r, int g, int b) STRATUS = (206, 212, 218);
    #endregion

    #region rendering
    public static (int r, int g, int b) DarkenColor((int r, int g, int b) color, int amount) =>
        (Math.Max(0, color.r - amount),
         Math.Max(0, color.g - amount),
         Math.Max(0, color.b - amount));

    public static (int r, int g, int b) BlendColor((int r, int g, int b) a, (int r, int g, int b) b, double t) =>
        (Math.Clamp((int)(a.r + (b.r - a.r) * t), 0, 255),
         Math.Clamp((int)(a.g + (b.g - a.g) * t), 0, 255),
         Math.Clamp((int)(a.b + (b.b - a.b) * t), 0, 255));
    #endregion

    #region dynamic
    public static (double h, double s, double v) ToHsv((int r, int g, int b) c)
    {
        double r = c.r / 255.0;
        double g = c.g / 255.0;
        double b = c.b / 255.0;
        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        double d = max - min;
        double h = 0;
        if (max == r && d > 0) h = 60 * (((g - b) / d) % 6);
        else if (max == g && d > 0) h = 60 * (((b - r) / d) + 2);
        else if (max == b && d > 0) h = 60 * (((r - g) / d) + 4);
        if (h < 0) h += 360;
        double s = max == 0 ? 0 : d / max;
        return (h, s, max);
    }

    public static (int r, int g, int b) FromHsv(double h, double s, double v)
    {
        int hi = Convert.ToInt32(Math.Floor(h / 60)) % 6;
        double f = h / 60 - Math.Floor(h / 60);
        v *= 255;
        int vInt = Convert.ToInt32(Math.Clamp(v, 0, 255));
        int p = Convert.ToInt32(Math.Clamp(v * (1 - s), 0, 255));
        int q = Convert.ToInt32(Math.Clamp(v * (1 - f * s), 0, 255));
        int t = Convert.ToInt32(Math.Clamp(v * (1 - (1 - f) * s), 0, 255));

        return hi switch
        {
            0 => (vInt, t, p),
            1 => (q, vInt, p),
            2 => (p, vInt, t),
            3 => (p, q, vInt),
            4 => (t, p, vInt),
            _ => (vInt, p, q)
        };
    }

    private static readonly object _syncRoot = new();
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (int r, int g, int b)> _cache = new();

    private static readonly Dictionary<string, (int r, int g, int b)> _colorVocab = new()
    {
        ["red"] = (180, 50, 50),       ["green"] = (130, 170, 100),   ["blue"] = (90, 130, 170),
        ["yellow"] = (220, 190, 120),  ["cyan"] = (60, 150, 160),     ["magenta"] = (160, 60, 120),
        ["orange"] = (190, 100, 40),   ["purple"] = (100, 50, 120),   ["pink"] = (210, 130, 140),
        ["brown"] = (120, 70, 30),     ["black"] = (20, 20, 20),      ["white"] = (230, 230, 235),
        ["grey"] = (110, 115, 120),    ["gray"] = (110, 115, 120),
        ["gold"] = (212, 175, 55),     ["teal"] = (56, 142, 142),     ["navy"] = (40, 55, 100),
        ["maroon"] = (110, 30, 40),    ["olive"] = (110, 115, 50),    ["lime"] = (150, 210, 80),
        ["coral"] = (200, 100, 80),    ["salmon"] = (210, 120, 100),  ["crimson"] = (180, 20, 40),
        ["violet"] = (130, 60, 180),   ["turquoise"] = (64, 180, 172),["lavender"] = (180, 160, 220),
        ["tan"] = (210, 180, 140),     ["sand"] = (220, 195, 150),    ["slate"] = (110, 120, 135),
        ["silver"] = (180, 185, 190),  ["mint"] = (150, 210, 180),    ["rust"] = (165, 70, 30),
        ["sienna"] = (140, 75, 45),    ["wheat"] = (230, 205, 165),   ["ivory"] = (230, 225, 210),
        ["plains"] = (148, 191, 115),  ["forest"] = (82, 106, 64),    ["mountain"] = (108, 117, 125),
        ["snow"] = (225, 228, 232),    ["beach"] = (240, 218, 165),   ["ocean"] = (130, 160, 188),
        ["river"] = (130, 160, 188),   ["lake"] = (130, 160, 188),    ["stream"] = (130, 160, 188),
        ["deep ocean"] = (95, 132, 165),["dark sand"] = (229, 193, 133),["deep mountain"] = (100, 110, 120),
        ["crab"] = (220, 20, 60),      ["turtle"] = (82, 106, 64),    ["cow"] = (139, 69, 19),
        ["sheep"] = (112, 128, 144),   ["wolf"] = (73, 80, 87),       ["bear"] = (160, 82, 45),
        ["goat"] = (245, 222, 179),    ["bird"] = (100, 149, 237),    ["villager"] = (255, 228, 196),
        ["fish"] = (0, 134, 179),
        ["cirrus"] = (225, 228, 232),  ["altocumulus"] = (225, 229, 242),["cumulus"] = (233, 236, 239),
        ["cumulonimbus"] = (84, 90, 97),["nimbostratus"] = (237, 242, 251),["stratus"] = (206, 212, 218),
        ["light blue"] = (173, 216, 230),["blue violet"] = (138, 43, 226),
        ["antique white"] = (250, 235, 215),["light yellow"] = (255, 255, 224),
    };

    private static readonly Dictionary<string, string> _materials = new()
    {
        ["water"] = "ocean : dark 20",
        ["grass"] = "plains : dark 10",
        ["rock"] = "mountain : dark 10",
        ["sky"] = "blue : bright 20",
    };

    public static void LoadBindings(string path)
    {
        if (!System.IO.File.Exists(path))
        {
            try
            {
                string dir = System.IO.Path.GetDirectoryName(path) ?? "";
                if (!string.IsNullOrEmpty(dir)) System.IO.Directory.CreateDirectory(dir);
                lock (_syncRoot)
                {
                    System.IO.File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(new {
                        bases = _colorVocab.ToDictionary(k => k.Key, v => $"{v.Value.r},{v.Value.g},{v.Value.b}"),
                        materials = _materials
                    }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                }
            }
            catch (System.IO.IOException) { }
            return;
        }

        var json = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(
            System.IO.File.ReadAllText(path));
        if (json == null) return;

        lock (_syncRoot)
        {
            if (json.TryGetValue("bases", out var bases))
                foreach (var b in bases)
                {
                    var p = b.Value.Split(',');
                    if (p.Length >= 3 &&
                        int.TryParse(p[0].Trim(), out int r) &&
                        int.TryParse(p[1].Trim(), out int g) &&
                        int.TryParse(p[2].Trim(), out int bv))
                        _colorVocab[b.Key] = (r, g, bv);
                }
            if (json.TryGetValue("materials", out var mats))
            {
                _materials.Clear();
                foreach (var m in mats) _materials[m.Key] = m.Value;
            }
        }
        _cache.Clear();
    }

    public static (int r, int g, int b) ParseDynamic(string text) => ParseCore(text, null) ?? (128, 128, 128);

    public static (int r, int g, int b)? TryParseDynamic(string text) => ParseCore(text, null);

    public static string GetName((int r, int g, int b) color)
    {
        string? best = null;
        double bestDist = double.MaxValue;
        lock (_syncRoot)
        {
            foreach (var kvp in _colorVocab)
            {
                double dr = kvp.Value.r - color.r;
                double dg = kvp.Value.g - color.g;
                double db = kvp.Value.b - color.b;
                double dist = dr * dr + dg * dg + db * db;
                if (dist < bestDist) { bestDist = dist; best = kvp.Key; }
            }
        }
        return best ?? "grey";
    }

    private static (int r, int g, int b)? ParseCore(string text, HashSet<string>? seen)
    {
        text = text.ToLowerInvariant().Trim();
        if (_cache.TryGetValue(text, out var hit)) return hit;

        if (text.Length == 7 && text[0] == '#' &&
            int.TryParse(text[1..3], System.Globalization.NumberStyles.HexNumber, null, out int hr) &&
            int.TryParse(text[3..5], System.Globalization.NumberStyles.HexNumber, null, out int hg) &&
            int.TryParse(text[5..7], System.Globalization.NumberStyles.HexNumber, null, out int hb))
        {
            _cache[text] = (hr, hg, hb);
            return (hr, hg, hb);
        }

        {
            var rp = text.Split(',');
            if (rp.Length == 3 &&
                int.TryParse(rp[0].Trim(), out int rr) &&
                int.TryParse(rp[1].Trim(), out int rg) &&
                int.TryParse(rp[2].Trim(), out int rb))
            {
                var rgb = (Math.Clamp(rr, 0, 255), Math.Clamp(rg, 0, 255), Math.Clamp(rb, 0, 255));
                _cache[text] = rgb;
                return rgb;
            }
        }

        seen ??= [];
        if (!seen.Add(text)) return null;

        string eval = text;
        lock (_syncRoot)
        {
            foreach (var mat in _materials)
                if (eval.Contains("$" + mat.Key))
                    eval = eval.Replace("$" + mat.Key, mat.Value);
            if (eval == text && _materials.TryGetValue(text, out var alias))
                eval = alias;
        }

        if (eval != text)
        {
            var resolved = ParseCore(eval, seen);
            if (resolved.HasValue) _cache[text] = resolved.Value;
            return resolved;
        }

        string[] parts = eval.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return null;

        HashSet<(int r, int g, int b)> found = [];
        string base0 = parts[0];

        lock (_syncRoot)
        {
            if (_colorVocab.TryGetValue(base0, out var exact))
                found.Add(exact);
            if (found.Count == 0)
                foreach (var kvp in _colorVocab)
                    if (base0.Contains(kvp.Key) || base0.Contains(kvp.Key + "ish"))
                        found.Add(kvp.Value);
        }

        if (found.Count == 0) return null;

        double hSum = 0, sSum = 0, vSum = 0;
        foreach (var c in found) { var (h, s, v) = ToHsv(c); hSum += h; sSum += s; vSum += v; }
        (double h, double s, double v) cur = (hSum / found.Count, sSum / found.Count, vSum / found.Count);

        if (base0.Contains("light") || base0.Contains("bright")) cur.v = Math.Min(1.0, cur.v + 0.2);
        if (base0.Contains("dark") || base0.Contains("deep")) cur.v = Math.Max(0.05, cur.v - 0.2);
        if (base0.Contains("pale") || base0.Contains("pastel") || base0.Contains("muted")) cur.s = Math.Max(0.05, cur.s - 0.2);
        if (base0.Contains("vibrant") || base0.Contains("rich")) cur.s = Math.Min(1.0, cur.s + 0.2);

        for (int i = 1; i < parts.Length; i++)
        {
            string[] mod = parts[i].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (mod.Length == 0) continue;
            double val = mod.Length > 1 && double.TryParse(mod[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double pv) ? pv / 100.0 : 0.2;
            switch (mod[0])
            {
                case "dark":      case "darken":     cur.v = Math.Max(0.05, cur.v - val); break;
                case "bright":    case "brighten":   cur.v = Math.Min(1.0,  cur.v + val); break;
                case "pale":      case "desaturate": cur.s = Math.Max(0.05, cur.s - val); break;
                case "vibrant":   case "saturate":   cur.s = Math.Min(1.0,  cur.s + val); break;
                case "hue":       cur.h = (cur.h + val * 360) % 360; break;
                case "complement":cur.h = (cur.h + 180) % 360; break;
                case "night":
                {
                    var nb = BlendColor(FromHsv(cur.h, cur.s, cur.v), NIGHT_TINT, val);
                    (cur.h, cur.s, cur.v) = ToHsv(nb);
                    break;
                }
                case "blend":
                    if (mod.Length >= 2)
                    {
                        var bt = ParseCore(mod[1], seen);
                        if (bt.HasValue)
                        {
                            double t = mod.Length >= 3 && double.TryParse(mod[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double bv) ? bv / 100.0 : 0.5;
                            var bb = BlendColor(FromHsv(cur.h, cur.s, cur.v), bt.Value, t);
                            (cur.h, cur.s, cur.v) = ToHsv(bb);
                        }
                    }
                    break;
            }
        }

        var result = FromHsv(cur.h, cur.s, cur.v);
        _cache[text] = result;
        return result;
    }

    public static (int r, int g, int b) ApplyNoise((int r, int g, int b) c, int noise) =>
        (Math.Clamp(c.r + noise, 0, 255), Math.Clamp(c.g + noise, 0, 255), Math.Clamp(c.b + noise, 0, 255));

    public static (int r, int g, int b) RandomVariation((int r, int g, int b) c, Random rng, int variance) =>
        ApplyNoise(c, rng.Next(-variance, variance + 1));
    #endregion
}
