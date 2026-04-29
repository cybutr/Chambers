using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Runtime.InteropServices;
using Internal;

public enum SerializationFormat { Binary = 0, Json = 1 }

partial class Program
{
    #region map saving
    // Compress JSON string to GZip
    private static byte[] CompressString(string json)
    {
        using var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var outputStream = new MemoryStream();
        using (var gzip = new GZipStream(outputStream, CompressionLevel.Optimal))
        {
            inputStream.CopyTo(gzip);
        }
        return outputStream.ToArray();
    }

    // Decompress GZip back to JSON string
    private static string DecompressToString(byte[] compressed)
    {
        using var inputStream = new MemoryStream(compressed);
        using var gzip = new GZipStream(inputStream, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip, Encoding.UTF8);
        return reader.ReadToEnd();
    }
    public class Char2DArrayJsonConverter : JsonConverter<char[,]>
    {
        public override char[,] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var lines = JsonSerializer.Deserialize<List<string>>(ref reader, options) ?? [];
            if (lines.Count == 0) return new char[0, 0];

            int height = lines.Count;
            int width = lines[0].Length;
            var result = new char[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    result[x, y] = lines[y][x];
                }
            }
            return result;
        }
        public override void Write(Utf8JsonWriter writer, char[,] value, JsonSerializerOptions options)
        {
            int width = value.GetLength(0);
            int height = value.GetLength(1);
            var lines = new List<string>(height);

            for (int y = 0; y < height; y++)
            {
                var row = new char[width];
                for (int x = 0; x < width; x++)
                {
                    row[x] = value[x, y];
                }
                lines.Add(new string(row));
            }
            JsonSerializer.Serialize(writer, lines, new JsonSerializerOptions(options)
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }
    }
    public class Bool2DArrayJsonConverter : JsonConverter<bool[,]>
    {
        public override bool[,] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var lines = JsonSerializer.Deserialize<List<string>>(ref reader, options) ?? [];
            if (lines.Count == 0) return new bool[0, 0];

            int height = lines.Count;
            int width = lines[0].Length;
            var result = new bool[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    result[x, y] = (lines[y][x] == '1');
                }
            }
            return result;
        }
        public override void Write(Utf8JsonWriter writer, bool[,] value, JsonSerializerOptions options)
        {
            int width = value.GetLength(0);
            int height = value.GetLength(1);
            var lines = new List<string>(height);

            for (int y = 0; y < height; y++)
            {
                var row = new char[width];
                for (int x = 0; x < width; x++)
                {
                    row[x] = value[x, y] ? '1' : '0';
                }
                lines.Add(new string(row));
            }
            JsonSerializer.Serialize(writer, lines, options);
        }
    }
    public class Int2DArrayJsonConverter : JsonConverter<int[,]>
    {
        public override int[,] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var lines = JsonSerializer.Deserialize<List<string>>(ref reader, options) ?? [];
            if (lines.Count == 0) return new int[0, 0];

            int height = lines.Count;
            var splittedLines = new List<List<int>>();
            foreach (var line in lines)
            {
                var rowData = new List<int>();
                var parts = line.Split(['(', ')'], StringSplitOptions.RemoveEmptyEntries);
                parts = [.. parts.Select(p => p.Trim()).Where(p => !string.IsNullOrWhiteSpace(p))];
                foreach (var chunk in parts)
                {
                    rowData.Add(int.Parse(chunk));
                }
                splittedLines.Add(rowData);
            }

            int width = splittedLines.Max(r => r.Count);
            var result = new int[height, width];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < splittedLines[y].Count; x++)
                {
                    result[y, x] = splittedLines[y][x];
                }
            }
            return result;
        }
        public override void Write(Utf8JsonWriter writer, int[,] value, JsonSerializerOptions options)
        {
            int height = value.GetLength(0);
            int width = value.GetLength(1);
            var lines = new List<string>(height);

            for (int y = 0; y < height; y++)
            {
                var row = new StringBuilder();
                for (int x = 0; x < width; x++)
                {
                    row.Append($"({value[y, x]})");
                }
                lines.Add(row.ToString());
            }
            JsonSerializer.Serialize(writer, lines, new JsonSerializerOptions(options)
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }
    }
    public class BoolJsonConverter : JsonConverter<bool>
    {
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number) return reader.GetInt32() != 0;
            throw new JsonException("Expected 0 or 1");
        }
        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options) => writer.WriteNumberValue(value ? 1 : 0);
    }
    public class ValueTupleIntKeyConverter<TValue> : JsonConverter<Dictionary<(int, int), TValue>>
    {
        public override Dictionary<(int, int), TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dict = new Dictionary<(int, int), TValue>();
            var intermediate = JsonSerializer.Deserialize<Dictionary<string, TValue>>(ref reader, options);
            if (intermediate != null)
            {
                foreach (var kvp in intermediate)
                {
                    var parts = kvp.Key.Split('|');
                    var key = (int.Parse(parts[0]), int.Parse(parts[1]));
                    dict[key] = kvp.Value;
                }
            }
            return dict;
        }
        public override void Write(Utf8JsonWriter writer, Dictionary<(int, int), TValue> value, JsonSerializerOptions options)
        {
            var intermediate = value.ToDictionary(k => $"{k.Key.Item1}|{k.Key.Item2}", v => v.Value);
            JsonSerializer.Serialize(writer, intermediate, options);
        }
    }
    public class ValueTupleIntDoubleKeyConverter : JsonConverter<Dictionary<(int, int), double>>
    {
        public override Dictionary<(int, int), double> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dict = new Dictionary<(int, int), double>();
            var intermediate = JsonSerializer.Deserialize<Dictionary<string, double>>(ref reader, options);
            if (intermediate != null)
            {
                foreach (var kvp in intermediate) { var parts = kvp.Key.Split('|'); var key = (int.Parse(parts[0]), int.Parse(parts[1])); dict[key] = kvp.Value; }
            }
            return dict;
        }
        public override void Write(Utf8JsonWriter writer, Dictionary<(int, int), double> value, JsonSerializerOptions options)
        {
            var intermediate = value.ToDictionary(k => $"{k.Key.Item1}|{k.Key.Item2}", v => v.Value);
            JsonSerializer.Serialize(writer, intermediate, options);
        }
    }
    public class Double2DArrayJsonConverter : JsonConverter<double[,]>
    {
        public override double[,] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var lines = JsonSerializer.Deserialize<List<string>>(ref reader, options) ?? [];
            if (lines.Count == 0) return new double[0, 0];

            int height = lines.Count;
            var splittedLines = new List<List<double>>();
            foreach (var line in lines)
            {
                var rowData = new List<double>();
                var parts = line.Split(['(', ')'], StringSplitOptions.RemoveEmptyEntries);
                parts = [.. parts.Select(p => p.Trim()).Where(p => !string.IsNullOrWhiteSpace(p))];
                foreach (var chunk in parts) rowData.Add(double.Parse(chunk, CultureInfo.InvariantCulture));
                splittedLines.Add(rowData);
            }

            int width = splittedLines.Max(r => r.Count);
            var result = new double[height, width];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < splittedLines[y].Count; x++)
                {
                    result[y, x] = splittedLines[y][x];
                }
            }
            return result;
        }
        public override void Write(Utf8JsonWriter writer, double[,] value, JsonSerializerOptions options)
        {
            int height = value.GetLength(0);
            int width = value.GetLength(1);
            var lines = new List<string>(height);

            for (int y = 0; y < height; y++)
            {
                var row = new StringBuilder();
                for (int x = 0; x < width; x++)
                {
                    row.Append($"({value[y, x].ToString(CultureInfo.InvariantCulture)})");
                }
                lines.Add(row.ToString());
            }
            JsonSerializer.Serialize(writer, lines, new JsonSerializerOptions(options)
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }
    }

    public class TileIdArrayJsonConverter : JsonConverter<TileId[,]>
    {
        public override TileId[,] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var lines = JsonSerializer.Deserialize<List<string>>(ref reader, options) ?? [];
            if (lines.Count == 0) return new TileId[0, 0];
            int height = lines.Count;
            int width = lines[0].Length;
            var result = new TileId[width, height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    result[x, y] = TileRegistry.FromChar(lines[y][x]);
            return result;
        }
        public override void Write(Utf8JsonWriter writer, TileId[,] value, JsonSerializerOptions options)
        {
            int width = value.GetLength(0);
            int height = value.GetLength(1);
            var lines = new List<string>(height);
            for (int y = 0; y < height; y++)
            {
                var row = new char[width];
                for (int x = 0; x < width; x++)
                    row[x] = TileRegistry.ToChar(value[x, y]);
                lines.Add(new string(row));
            }
            JsonSerializer.Serialize(writer, lines, new JsonSerializerOptions(options)
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }
    }
    public class CloudTypeArrayJsonConverter : JsonConverter<CloudType[,]>
    {
        private static readonly Dictionary<char, CloudType> _byChar = new()
        {
            ['\0'] = CloudType.None,
            [' ']  = CloudType.None,
            ['1']  = CloudType.Cirrus,
            ['2']  = CloudType.Altocumulus,
            ['3']  = CloudType.Cumulus,
            ['4']  = CloudType.Cumulonimbus,
            ['5']  = CloudType.Nimbostratus,
            ['6']  = CloudType.Stratus,
        };
        // Indexed by (int)CloudType
        private static readonly char[] _toChar = ['\0', '3', '6', '1', '4', '5', '2'];

        public override CloudType[,] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var lines = JsonSerializer.Deserialize<List<string>>(ref reader, options) ?? [];
            if (lines.Count == 0) return new CloudType[0, 0];
            int height = lines.Count;
            int width = lines[0].Length;
            var result = new CloudType[width, height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    result[x, y] = _byChar.GetValueOrDefault(lines[y][x], CloudType.None);
            return result;
        }
        public override void Write(Utf8JsonWriter writer, CloudType[,] value, JsonSerializerOptions options)
        {
            int width = value.GetLength(0);
            int height = value.GetLength(1);
            var lines = new List<string>(height);
            for (int y = 0; y < height; y++)
            {
                var row = new char[width];
                for (int x = 0; x < width; x++)
                    row[x] = _toChar[(int)value[x, y]];
                lines.Add(new string(row));
            }
            JsonSerializer.Serialize(writer, lines, new JsonSerializerOptions(options)
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }
    }

    public class EntityIdArrayJsonConverter : JsonConverter<EntityId[,]>
    {
        private static readonly Dictionary<char, EntityId> _byChar = new()
        {
            [' ']  = EntityId.None,
            ['\0'] = EntityId.None,
            ['c']  = EntityId.Crab,
            ['T']  = EntityId.Turtle,
            ['C']  = EntityId.Cow,
            ['S']  = EntityId.Sheep,
            ['W']  = EntityId.Wolf,
            ['B']  = EntityId.Bear,
            ['G']  = EntityId.Goat,
            ['F']  = EntityId.Fish,
            ['A']  = EntityId.Bird,
            ['V']  = EntityId.Villager,
        };
        private static readonly char[] _toChar = [' ', 'c', 'T', 'C', 'S', 'W', 'B', 'G', 'F', 'A', 'V'];

        public override EntityId[,] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var lines = JsonSerializer.Deserialize<List<string>>(ref reader, options) ?? [];
            if (lines.Count == 0) return new EntityId[0, 0];
            int height = lines.Count;
            int width = lines[0].Length;
            var result = new EntityId[width, height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    result[x, y] = _byChar.GetValueOrDefault(lines[y][x], EntityId.None);
            return result;
        }
        public override void Write(Utf8JsonWriter writer, EntityId[,] value, JsonSerializerOptions options)
        {
            int width = value.GetLength(0);
            int height = value.GetLength(1);
            var lines = new List<string>(height);
            for (int y = 0; y < height; y++)
            {
                var row = new char[width];
                for (int x = 0; x < width; x++)
                    row[x] = _toChar[(int)value[x, y]];
                lines.Add(new string(row));
            }
            JsonSerializer.Serialize(writer, lines, new JsonSerializerOptions(options)
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }
    }

    // ─── Noise regeneration ──────────────────────────────────────────────────
    private static void RegenerateNoise(Map map)
    {
        var tmpRng = new Random(map.seed);
        map.noise          = Perlin.GeneratePerlinNoise(map.width, map.height, map.conf.NoiseScale,      tmpRng.Next());
        map.tempatureNoise = Perlin.GeneratePerlinNoise(map.width, map.height, map.conf.NoiseScale * 25, tmpRng.Next());
        map.humidityNoise  = Perlin.GeneratePerlinNoise(map.width, map.height, map.conf.NoiseScale * 12, tmpRng.Next());
    }

    // ─── Dispatcher ──────────────────────────────────────────────────────────
    public static void SaveMap(Map map)
    {
        if (SaveFormat == SerializationFormat.Binary) SaveMapBinary(map);
        else SaveMapJson(map);
    }
    public static Map? LoadMap(string filePath)
    {
        return Path.GetExtension(filePath).ToLowerInvariant() == ".chmb"
            ? LoadMapBinary(filePath)
            : LoadMapJson(filePath);
    }

    // ─── JSON (legacy) ───────────────────────────────────────────────────────
    public static void SaveMapJson(Map map)
    {
        var folderPath = Path.Combine(Environment.CurrentDirectory, "Data/Saves");
        Directory.CreateDirectory(folderPath);

        string fileName = $"{map.conf.Name}.json";
        string fullPath = Path.Combine(folderPath, fileName);

        var options = new JsonSerializerOptions { WriteIndented = true, IncludeFields = true };
        options.Converters.Add(new Char2DArrayJsonConverter());
        options.Converters.Add(new Bool2DArrayJsonConverter());
        options.Converters.Add(new BoolJsonConverter());
        options.Converters.Add(new Int2DArrayJsonConverter());
        options.Converters.Add(new Double2DArrayJsonConverter());
        options.Converters.Add(new ValueTupleIntKeyConverter<int>());
        options.Converters.Add(new ValueTupleIntDoubleKeyConverter());
        options.Converters.Add(new TileIdArrayJsonConverter());
        options.Converters.Add(new CloudTypeArrayJsonConverter());
        options.Converters.Add(new EntityIdArrayJsonConverter());

        string json = JsonSerializer.Serialize(map, options);
        File.WriteAllText(fullPath, json);
    }
    public static Map? LoadMapJson(string filePath)
    {
        var options = new JsonSerializerOptions { IncludeFields = true };
        options.Converters.Add(new Char2DArrayJsonConverter());
        options.Converters.Add(new Bool2DArrayJsonConverter());
        options.Converters.Add(new BoolJsonConverter());
        options.Converters.Add(new Int2DArrayJsonConverter());
        options.Converters.Add(new Double2DArrayJsonConverter());
        options.Converters.Add(new ValueTupleIntKeyConverter<int>());
        options.Converters.Add(new ValueTupleIntDoubleKeyConverter());
        options.Converters.Add(new TileIdArrayJsonConverter());
        options.Converters.Add(new CloudTypeArrayJsonConverter());
        options.Converters.Add(new EntityIdArrayJsonConverter());

        string json = File.ReadAllText(filePath);
        Map? loaded = JsonSerializer.Deserialize<Map>(json, options);
        if (loaded != null)
        {
            RegenerateNoise(loaded);
            loaded.ReinitializeTransientData();
        }
        return loaded;
    }

    // ─── Binary helpers ───────────────────────────────────────────────────────
    private static JsonSerializerOptions MakeEmbeddedJsonOptions()
    {
        var opts = new JsonSerializerOptions { WriteIndented = false, IncludeFields = true };
        opts.Converters.Add(new BoolJsonConverter());
        return opts;
    }
    private static void WriteJsonSection(BinaryWriter w, object obj, JsonSerializerOptions opts)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(obj, opts));
        w.Write(bytes.Length);
        w.Write(bytes);
    }
    private static T? ReadJsonSection<T>(BinaryReader r, JsonSerializerOptions opts)
    {
        int len = r.ReadInt32();
        string json = Encoding.UTF8.GetString(r.ReadBytes(len));
        return JsonSerializer.Deserialize<T>(json, opts);
    }
    private static void WriteArray2DByte<T>(BinaryWriter w, T[,] arr, int width, int height, Func<int, int, byte> get)
    {
        w.Write(width); w.Write(height);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                w.Write(get(x, y));
    }
    private static void WriteArray2DInt(BinaryWriter w, int[,] arr, int width, int height)
    {
        w.Write(width); w.Write(height);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                w.Write(arr[x, y]);
    }
    private static void WriteArray2DDouble(BinaryWriter w, double[,] arr, int width, int height)
    {
        w.Write(width); w.Write(height);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                w.Write(arr[x, y]);
    }

    // ─── Binary save ──────────────────────────────────────────────────────────
    public static void SaveMapBinary(Map map)
    {
        var folderPath = Path.Combine(Environment.CurrentDirectory, "Data/Saves");
        Directory.CreateDirectory(folderPath);
        string fullPath = Path.Combine(folderPath, map.conf.Name + ".chmb");

        using var fileStream = File.Create(fullPath);
        // 5-byte uncompressed header
        fileStream.WriteByte((byte)'C');
        fileStream.WriteByte((byte)'H');
        fileStream.WriteByte((byte)'M');
        fileStream.WriteByte((byte)'B');
        fileStream.WriteByte(3); // version

        using var gzip = new GZipStream(fileStream, CompressionLevel.Optimal, leaveOpen: true);
        using var w = new BinaryWriter(gzip, Encoding.UTF8, leaveOpen: false);

        var jOpts = MakeEmbeddedJsonOptions();

        // ── Scalars ──
        w.Write(map.width);
        w.Write(map.height);
        w.Write(map.cloudDataWidth);
        w.Write(map.cloudDataHeight);
        w.Write(map.cloudDataOffsetX);
        w.Write(map.cloudDataOffsetY);
        w.Write(map.seed);
        w.Write(map.topPadding);
        w.Write(map.bottomPadding);
        w.Write(map.leftPadding);
        w.Write(map.rightPadding);
        w.Write(map.SavedConsoleWidth);
        w.Write(map.SavedConsoleHeight);
        w.Write(map.cloudFormations);
        w.Write(map.cloudShadowOffsetX);
        w.Write(map.cloudShadowOffsetY);
        w.Write(map.DayCount);
        w.Write(map.time);
        w.Write(map.avarageTempature);
        w.Write(map.avarageHumidity);
        w.Write(map.totalTemp);
        w.Write(map.totalHum);
        w.Write(map.timeSinceLastCloudSpawn);
        w.Write(map.TimeSinceLastWeatherChange);
        w.Write(map.deltaTime);
        w.Write(map.MinTimeBetweenChanges);
        w.Write(map.WindChangeTimer);
        w.Write(map.WindTargetDirection);
        w.Write(map.MinWindChangeInterval);
        w.Write(map.MaxWindChangeInterval);
        w.Write(map.IsTurning);
        w.Write(map.isCloudsRendering);
        w.Write(map.isCloudsShadowsRendering);
        w.Write(map.shouldSimulationContinue);
        w.Write(map.debug);
        w.Write((int)map.CurrentGradientDirection);
        w.Write(map.SelectColor.r);
        w.Write(map.SelectColor.g);
        w.Write(map.SelectColor.b);
        w.Write(map.shadowRadius); // readonly = 3, written for stream alignment

        // ── JSON-embedded objects ──
        WriteJsonSection(w, map.conf, jOpts);
        WriteJsonSection(w, map.weather, jOpts);
        WriteJsonSection(w, map.dayNight, jOpts);
        WriteJsonSection(w, map.waves, jOpts);
        WriteJsonSection(w, map.crabs, jOpts);
        WriteJsonSection(w, map.turtles, jOpts);
        WriteJsonSection(w, map.cows, jOpts);
        WriteJsonSection(w, map.sheeps, jOpts);

        // ── actualOutputBuffer ──
        w.Write(map.actualOutputBuffer.Count);
        foreach (string s in map.actualOutputBuffer)
        {
            byte[] sb = Encoding.UTF8.GetBytes(s);
            w.Write(sb.Length);
            w.Write(sb);
        }

        // ── Sets / Dicts ──
        w.Write(map.cloudSizes.Count);
        foreach (var kvp in map.cloudSizes) { w.Write(kvp.Key.x); w.Write(kvp.Key.y); w.Write(kvp.Value); }

        // ── 2D arrays ──
        WriteArray2DByte(w, map.mapData, map.width, map.height, (x, y) => (byte)map.mapData[x, y]);
        WriteArray2DByte(w, map.overlayData, map.width, map.height, (x, y) => (byte)map.overlayData[x, y]);
        WriteArray2DByte(w, map.cloudData, map.cloudDataWidth, map.cloudDataHeight, (x, y) => (byte)map.cloudData[x, y]);
        WriteArray2DInt(w, map.cloudDepthData, map.cloudDataWidth, map.cloudDataHeight);
        WriteArray2DDouble(w, map.precipitationData, map.cloudDataWidth, map.cloudDataHeight);
        WriteArray2DDouble(w, map.previousPrecipitationData, map.cloudDataWidth, map.cloudDataHeight);
        WriteArray2DInt(w, map.temperatureData, map.width, map.height);
        WriteArray2DInt(w, map.humidityData, map.width, map.height);
        // noise, tempatureNoise, humidityNoise — skipped, regenerated on load
        // rng — skipped, reconstructed on load
    }

    // ─── Binary load ──────────────────────────────────────────────────────────
    public static Map? LoadMapBinary(string filePath)
    {
        using var fileStream = File.OpenRead(filePath);

        // Read 5-byte uncompressed header
        byte[] magic = new byte[4];
        fileStream.ReadExactly(magic, 0, 4);
        if (magic[0] != 'C' || magic[1] != 'H' || magic[2] != 'M' || magic[3] != 'B')
            throw new InvalidDataException($"Not a valid .chmb file: {filePath}");
        int version = fileStream.ReadByte();
        if (version != 3)
            throw new InvalidDataException($"Unsupported .chmb version {version}: {filePath}");

        using var gzip = new GZipStream(fileStream, CompressionMode.Decompress, leaveOpen: true);
        using var r = new BinaryReader(gzip, Encoding.UTF8, leaveOpen: false);

        var jOpts = MakeEmbeddedJsonOptions();
        var map = new Map
        {
            // ── Scalars ──
            width = r.ReadInt32(),
            height = r.ReadInt32(),
            cloudDataWidth = r.ReadInt32(),
            cloudDataHeight = r.ReadInt32(),
            cloudDataOffsetX = r.ReadInt32(),
            cloudDataOffsetY = r.ReadInt32(),
            seed = r.ReadInt32(),
            topPadding = r.ReadInt32(),
            bottomPadding = r.ReadInt32(),
            leftPadding = r.ReadInt32(),
            rightPadding = r.ReadInt32(),
            SavedConsoleWidth = r.ReadInt32(),
            SavedConsoleHeight = r.ReadInt32(),
            cloudFormations = r.ReadInt32(),
            cloudShadowOffsetX = r.ReadInt32(),
            cloudShadowOffsetY = r.ReadInt32(),
            DayCount = r.ReadInt32(),
            time = r.ReadDouble(),
            avarageTempature = r.ReadDouble(),
            avarageHumidity = r.ReadDouble(),
            totalTemp = r.ReadDouble(),
            totalHum = r.ReadDouble(),
            timeSinceLastCloudSpawn = r.ReadDouble(),
            TimeSinceLastWeatherChange = r.ReadDouble(),
            deltaTime = r.ReadDouble(),
            MinTimeBetweenChanges = r.ReadDouble(),
            WindChangeTimer = r.ReadDouble(),
            WindTargetDirection = r.ReadDouble(),
            MinWindChangeInterval = r.ReadDouble(),
            MaxWindChangeInterval = r.ReadDouble(),
            IsTurning = r.ReadBoolean(),
            isCloudsRendering = r.ReadBoolean(),
            isCloudsShadowsRendering = r.ReadBoolean(),
            shouldSimulationContinue = r.ReadBoolean(),
            debug = r.ReadBoolean(),
            CurrentGradientDirection = (GradientDirection)r.ReadInt32()
        };
        int selR = r.ReadInt32(), selG = r.ReadInt32(), selB = r.ReadInt32();
        map.SelectColor = (selR, selG, selB);
        r.ReadInt32(); // shadowRadius — readonly = 3, discard

        // ── JSON-embedded objects ──
        map.conf    = ReadJsonSection<Config>(r, jOpts)         ?? map.conf;
        map.weather = ReadJsonSection<Weather>(r, jOpts)        ?? map.weather;
        map.dayNight = ReadJsonSection<DayNightCycle>(r, jOpts) ?? map.dayNight;
        map.waves   = ReadJsonSection<List<Wave>>(r, jOpts)     ?? map.waves;
        map.crabs   = ReadJsonSection<List<Crab>>(r, jOpts)  ?? map.crabs;
        map.turtles = ReadJsonSection<List<Turtle>>(r, jOpts) ?? map.turtles;
        map.cows    = ReadJsonSection<List<Cow>>(r, jOpts)   ?? map.cows;
        map.sheeps  = ReadJsonSection<List<Sheep>>(r, jOpts) ?? map.sheeps;

        // ── actualOutputBuffer ──
        int bufCount = r.ReadInt32();
        map.actualOutputBuffer = new List<string>(bufCount);
        for (int i = 0; i < bufCount; i++)
        {
            int len = r.ReadInt32();
            map.actualOutputBuffer.Add(Encoding.UTF8.GetString(r.ReadBytes(len)));
        }

        // ── Sets / Dicts ──
        int count;

        count = r.ReadInt32();
        map.cloudSizes = new Dictionary<(int x, int y), int>(count);
        for (int i = 0; i < count; i++) map.cloudSizes.Add((r.ReadInt32(), r.ReadInt32()), r.ReadInt32());

        // ── 2D arrays ──
        map.mapData    = ReadArray2DByte(r, (b) => (TileId)b);
        map.overlayData = ReadArray2DByte(r, (b) => (EntityId)b);
        map.cloudData   = ReadArray2DByte(r, (b) => (CloudType)b);
        map.cloudDepthData            = ReadArray2DInt(r);
        map.precipitationData         = ReadArray2DDouble(r);
        map.previousPrecipitationData = ReadArray2DDouble(r);
        map.temperatureData           = ReadArray2DInt(r);
        map.humidityData              = ReadArray2DInt(r);
        if (version == 1) { ReadArray2DBool(r); ReadArray2DBool(r); } // discard legacy cloudIsNight / cloudIsDarkening

        // ── Post-load setup ──
        RegenerateNoise(map);
        map.ReinitializeTransientData();
        map.rng = new Random(map.seed);

        return map;
    }

    private static T[,] ReadArray2DByte<T>(BinaryReader r, Func<byte, T> convert)
    {
        int width = r.ReadInt32(), height = r.ReadInt32();
        var arr = new T[width, height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                arr[x, y] = convert(r.ReadByte());
        return arr;
    }
    private static int[,] ReadArray2DInt(BinaryReader r)
    {
        int width = r.ReadInt32(), height = r.ReadInt32();
        var arr = new int[width, height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                arr[x, y] = r.ReadInt32();
        return arr;
    }
    private static double[,] ReadArray2DDouble(BinaryReader r)
    {
        int width = r.ReadInt32(), height = r.ReadInt32();
        var arr = new double[width, height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                arr[x, y] = r.ReadDouble();
        return arr;
    }
    private static bool[,] ReadArray2DBool(BinaryReader r)
    {
        int width = r.ReadInt32(), height = r.ReadInt32();
        var arr = new bool[width, height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                arr[x, y] = r.ReadByte() != 0;
        return arr;
    }

    // Saving with compression
    /*         public static void SaveMap(Map map)
            {
                var savePath = Path.Combine(Environment.CurrentDirectory, "Data/Saves");
                Directory.CreateDirectory(savePath);
                var fileName = $"{map.conf.Name}.json.gz";
                var fullPath = Path.Combine(savePath, fileName);

                var options = new JsonSerializerOptions { IncludeFields = true };
                options.Converters.Add(new Char2DArrayJsonConverter());
                options.Converters.Add(new Bool2DArrayJsonConverter());
                options.Converters.Add(new BoolJsonConverter());
                options.Converters.Add(new Int2DArrayJsonConverter());
                options.Converters.Add(new Double2DArrayJsonConverter());
                options.Converters.Add(new ValueTupleIntKeyConverter<int>());
                options.Converters.Add(new ValueTupleIntDoubleKeyConverter());

                var json = JsonSerializer.Serialize(map, options);
                using var fileStream = File.Create(fullPath);
                using var gzip = new GZipStream(fileStream, CompressionMode.Compress);
                using var writer = new StreamWriter(gzip);
                writer.Write(json);
            }
            public static Map? LoadMap(string filePath)
            {
                var options = new JsonSerializerOptions { IncludeFields = true };
                options.Converters.Add(new Char2DArrayJsonConverter());
                options.Converters.Add(new Bool2DArrayJsonConverter());
                options.Converters.Add(new BoolJsonConverter());
                options.Converters.Add(new Int2DArrayJsonConverter());
                options.Converters.Add(new Double2DArrayJsonConverter());
                options.Converters.Add(new ValueTupleIntKeyConverter<int>());
                options.Converters.Add(new ValueTupleIntDoubleKeyConverter());

                using var fileStream = File.OpenRead(filePath);
                using var gzip = new GZipStream(fileStream, CompressionMode.Decompress);
                using var reader = new StreamReader(gzip);
                var json = reader.ReadToEnd();
                return JsonSerializer.Deserialize<Map>(json, options);
            }
     */
    public static void DeleteMap(string filePath) =>  File.Delete(filePath);
    public static string? RenameChamberFile(string oldFilePath, string newName, Map chamber)
    {
        try
        {
            string ext = SaveFormat == SerializationFormat.Binary ? ".chmb" : ".json";
            string directory = Path.GetDirectoryName(oldFilePath) ?? "Data/Saves";
            string newFilePath = Path.Combine(directory, newName + ext);

            // Update chamber config first
            chamber.conf.Name = newName;

            // If the file doesn't need to be renamed (same path), just save
            if (string.Equals(oldFilePath, newFilePath, StringComparison.OrdinalIgnoreCase))
            {
                SaveMap(chamber);
                return newFilePath;
            }

            // If target file already exists, delete it
            if (File.Exists(newFilePath)) File.Delete(newFilePath);

            // Move/rename the file if source exists
            if (File.Exists(oldFilePath))  File.Move(oldFilePath, newFilePath);

            // Save the updated chamber to ensure config is persisted
            SaveMap(chamber);

            return newFilePath;
        }
        catch
        {
            // If renaming fails, just save with the new name
            chamber.conf.Name = newName;
            SaveMap(chamber);
            string ext = SaveFormat == SerializationFormat.Binary ? ".chmb" : ".json";
            return Path.Combine("Data/Saves", newName + ext);
        }
    }

    public static void SynchronizeAllChamberFiles()
    {
        var folderPath = Path.Combine(Environment.CurrentDirectory, "Data/Saves");
        if (!Directory.Exists(folderPath)) return;

        var filesToRename = new List<(string oldPath, string newPath, Map chamber)>();

        // Scan all files and check for mismatches
        foreach (var file in Directory.GetFiles(folderPath, "*.chmb")
                                    .Concat(Directory.GetFiles(folderPath, "*.json")))
        {
            Map? chamber = LoadMap(file);
            if (chamber != null)
            {
                string actualFileName = Path.GetFileNameWithoutExtension(file);
                string configName = chamber.conf.Name;

                // If there's a mismatch, prepare for rename
                if (actualFileName != configName)
                {
                    string newPath = Path.Combine(folderPath, configName + ".json");
                    filesToRename.Add((file, newPath, chamber));
                }
            }
        }

        // Perform renames
        foreach (var (oldPath, newPath, chamber) in filesToRename)
        {
            try
            {
                // If target exists, delete it (avoid conflicts)
                if (File.Exists(newPath) && !string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase)) File.Delete(newPath);

                File.Move(oldPath, newPath);
            }
            catch
            {
                // If rename fails, at least ensure the config matches the current file name
                chamber.conf.Name = Path.GetFileNameWithoutExtension(oldPath);
                SaveMap(chamber);
            }
        }
    }

    public static string GetUniqueChamberName(string baseName, IEnumerable<string> existingNames)
    {
        var existingSet = existingNames.ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!existingSet.Contains(baseName)) return baseName;

        int counter = 1;
        string candidateName;
        do
        {
            candidateName = $"{baseName}{counter}";
            counter++;
        } while (existingSet.Contains(candidateName));

        return candidateName;
    }

    public static void LoadAllMapsFromFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath)) return;

        // .chmb listed first so it takes priority when both formats share a name
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.GetFiles(folderPath, "*.chmb")
                                    .Concat(Directory.GetFiles(folderPath, "*.json")))
        {
            string name = Path.GetFileNameWithoutExtension(file);
            if (!seen.Add(name)) continue;

            Map? loadedMap = LoadMap(file);
            if (loadedMap != null)
            {
                loadedMap.conf.Name = name;
                allChambers.Add(loadedMap);
            }
        }
    }

    #region config persistence
    private static readonly string _configPath = Path.Combine("Data", "Config", "last_config.json");

    public static void SaveUserConfig(Config conf)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);
            // Exclude Seed and Name — they are session-specific
            var saved = new
            {
                conf.Width,
                conf.Height,
                conf.NoiseScale,
                conf.ErosionFactor,
                conf.MinBiomeSize,
                conf.MinLakeSize,
                conf.MinRiverWidth,
                conf.MaxRiverWidth,
                conf.MinMountainWidth,
                conf.MaxMountainWidth,
                conf.RiverFlowChance,
                conf.PlainsHeightThreshold,
                conf.ForestHeightThreshold,
                conf.MountainHeightThreshold,
                conf.EnableRivers,
                conf.EnableLakes,
                conf.EnableMountainRanges,
                conf.EnableTempatureBiomeChanges,
                conf.EnableHumidityBiomeChanges,
                conf.BiomeBlend,
                conf.NoiseType,
                conf.EnableWildfires,
                conf.EnableSecrets,
                conf.DoTimeCycle,
                conf.DoWeatherCycle,
                conf.GenerateStructrs,
                conf.EnableVillages,
                conf.EnableCities,
                conf.EnableDungeons,
                conf.GenerateAnimals,
                conf.EnablePredators,
                conf.EnableAnimalMovement,
                conf.EnableAnimalBreeding,
                conf.EnableAnimalDeath,
                conf.EnableAnimalExtinction,
                conf.EnableAnimalMigration,
                conf.EnableAnimalHunting,
                conf.EnableAnimalDomestication,
                conf.EnableTornadoes,
                conf.EnableEarthquakes,
                conf.EnableVolcanoes,
                conf.EnableFloods,
                conf.EnableMeteors,
                conf.EnableRobberies,
                conf.EnableMurders,
                conf.EnableRiots,
                conf.EnablePlagues,
                conf.EnableWars,
                conf.EnableTrade,
                conf.EnableCurrency,
                conf.EnableTaxes,
                conf.EnableBanks,
                conf.DisplayShadows,
                conf.DisplayWaves,
                conf.NumberOfWaves,
                conf.ShouldSave,
            };
            string json = JsonSerializer.Serialize(saved, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_configPath, json);
        }
        catch { /* non-fatal */ }
    }

    public static void TryLoadUserConfig(Config target)
    {
        try
        {
            if (!File.Exists(_configPath)) return;
            string json = File.ReadAllText(_configPath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            void TrySetInt(string key, Action<int> setter)
            { if (root.TryGetProperty(key, out var v)) setter(v.GetInt32()); }
            void TrySetDouble(string key, Action<double> setter)
            { if (root.TryGetProperty(key, out var v)) setter(v.GetDouble()); }
            void TrySetBool(string key, Action<bool> setter)
            { if (root.TryGetProperty(key, out var v)) setter(v.GetBoolean()); }

            TrySetInt("Width", v => target.Width = v);
            TrySetInt("Height", v => target.Height = v);
            TrySetDouble("NoiseScale", v => target.NoiseScale = v);
            TrySetDouble("ErosionFactor", v => target.ErosionFactor = v);
            TrySetInt("MinBiomeSize", v => target.MinBiomeSize = v);
            TrySetInt("MinLakeSize", v => target.MinLakeSize = v);
            TrySetInt("MinRiverWidth", v => target.MinRiverWidth = v);
            TrySetInt("MaxRiverWidth", v => target.MaxRiverWidth = v);
            TrySetInt("MinMountainWidth", v => target.MinMountainWidth = v);
            TrySetInt("MaxMountainWidth", v => target.MaxMountainWidth = v);
            TrySetDouble("RiverFlowChance", v => target.RiverFlowChance = v);
            TrySetDouble("PlainsHeightThreshold", v => target.PlainsHeightThreshold = v);
            TrySetDouble("ForestHeightThreshold", v => target.ForestHeightThreshold = v);
            TrySetDouble("MountainHeightThreshold", v => target.MountainHeightThreshold = v);
            TrySetBool("EnableRivers", v => target.EnableRivers = v);
            TrySetBool("EnableLakes", v => target.EnableLakes = v);
            TrySetBool("EnableMountainRanges", v => target.EnableMountainRanges = v);
            TrySetBool("EnableTempatureBiomeChanges", v => target.EnableTempatureBiomeChanges = v);
            TrySetBool("EnableHumidityBiomeChanges", v => target.EnableHumidityBiomeChanges = v);
            TrySetInt("BiomeBlend", v => target.BiomeBlend = v);
            TrySetInt("NoiseType", v => target.NoiseType = v);
            TrySetBool("EnableWildfires", v => target.EnableWildfires = v);
            TrySetBool("EnableSecrets", v => target.EnableSecrets = v);
            TrySetBool("DoTimeCycle", v => target.DoTimeCycle = v);
            TrySetBool("DoWeatherCycle", v => target.DoWeatherCycle = v);
            TrySetBool("GenerateStructrs", v => target.GenerateStructrs = v);
            TrySetBool("EnableVillages", v => target.EnableVillages = v);
            TrySetBool("EnableCities", v => target.EnableCities = v);
            TrySetBool("EnableDungeons", v => target.EnableDungeons = v);
            TrySetBool("GenerateAnimals", v => target.GenerateAnimals = v);
            TrySetBool("EnablePredators", v => target.EnablePredators = v);
            TrySetBool("EnableAnimalMovement", v => target.EnableAnimalMovement = v);
            TrySetBool("EnableAnimalBreeding", v => target.EnableAnimalBreeding = v);
            TrySetBool("EnableAnimalDeath", v => target.EnableAnimalDeath = v);
            TrySetBool("EnableAnimalExtinction", v => target.EnableAnimalExtinction = v);
            TrySetBool("EnableAnimalMigration", v => target.EnableAnimalMigration = v);
            TrySetBool("EnableAnimalHunting", v => target.EnableAnimalHunting = v);
            TrySetBool("EnableAnimalDomestication", v => target.EnableAnimalDomestication = v);
            TrySetBool("EnableTornadoes", v => target.EnableTornadoes = v);
            TrySetBool("EnableEarthquakes", v => target.EnableEarthquakes = v);
            TrySetBool("EnableVolcanoes", v => target.EnableVolcanoes = v);
            TrySetBool("EnableFloods", v => target.EnableFloods = v);
            TrySetBool("EnableMeteors", v => target.EnableMeteors = v);
            TrySetBool("EnableRobberies", v => target.EnableRobberies = v);
            TrySetBool("EnableMurders", v => target.EnableMurders = v);
            TrySetBool("EnableRiots", v => target.EnableRiots = v);
            TrySetBool("EnablePlagues", v => target.EnablePlagues = v);
            TrySetBool("EnableWars", v => target.EnableWars = v);
            TrySetBool("EnableTrade", v => target.EnableTrade = v);
            TrySetBool("EnableCurrency", v => target.EnableCurrency = v);
            TrySetBool("EnableTaxes", v => target.EnableTaxes = v);
            TrySetBool("EnableBanks", v => target.EnableBanks = v);
            TrySetBool("DisplayShadows", v => target.DisplayShadows = v);
            TrySetBool("DisplayWaves", v => target.DisplayWaves = v);
            TrySetInt("NumberOfWaves", v => target.NumberOfWaves = v);
            TrySetBool("ShouldSave", v => target.ShouldSave = v);
        }
        catch { /* non-fatal */ }
    }
    #endregion
    #endregion
}
