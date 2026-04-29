using System.Text;
using Internal;

public class GuiBuffer
{
    private static readonly bool _isLinux = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux);

    private struct Cell
    {
        public (int r, int g, int b) bg, fg;
        public bool hasFg, hasBg;
        public char ch;
        public int version;
    }

    private Cell[,]? _front;
    private Cell[,]? _back;
    private int _w, _h;
    private readonly StringBuilder _sb = new(4096);

    public void Resize(int w, int h) { _front = null; _back = null; _w = w; _h = h; }
    public void Invalidate() => _front = null;

    private void Ensure()
    {
        int w = Console.WindowWidth;
        int h = Console.WindowHeight;
        if (_back == null || _back.GetLength(0) != w || _back.GetLength(1) != h)
        {
            _front = null;
            _back = new Cell[w, h];
            _w = w;
            _h = h;
        }
        if (_front == null)
            _front = new Cell[_w, _h];
    }

    public void Set(int x, int y, char ch,
        (int r, int g, int b) fg, bool hasFg,
        (int r, int g, int b) bg, bool hasBg,
        int version)
    {
        Ensure();
        if (x < 0 || y < 0 || x >= _w || y >= _h) return;
        ref Cell c = ref _back![x, y];
        c.ch = ch; c.fg = fg; c.hasFg = hasFg; c.bg = bg; c.hasBg = hasBg; c.version = version;
    }

    public void Write(int x, int y, string text,
        (int r, int g, int b) fg, bool hasFg,
        (int r, int g, int b) bg, bool hasBg,
        int version)
    {
        for (int i = 0; i < text.Length; i++)
            Set(x + i, y, text[i], fg, hasFg, bg, hasBg, version);
    }

    public void Fill(int x, int y, int w, int h,
        (int r, int g, int b) bg, int version)
    {
        for (int dy = 0; dy < h; dy++)
        for (int dx = 0; dx < w; dx++)
            Set(x + dx, y + dy, ' ', default, false, bg, true, version);
    }

    public void Clear(int x, int y, int w, int h, int version)
    {
        for (int dy = 0; dy < h; dy++)
        for (int dx = 0; dx < w; dx++)
            Set(x + dx, y + dy, ' ', default, false, default, false, version);
    }

    public void DrawBox(int x, int y, int w, int h,
        string title, (int r, int g, int b) color, int version)
    {
        if (w < 2 || h < 2) return;
        char tl = _isLinux ? '+' : '╔';
        char tr = _isLinux ? '+' : '╗';
        char bl = _isLinux ? '+' : '╚';
        char br = _isLinux ? '+' : '╝';
        char hz = _isLinux ? '-' : '═';
        char vt = _isLinux ? '|' : '║';

        Set(x, y, tl, color, true, default, false, version);
        Set(x + w - 1, y, tr, color, true, default, false, version);
        Set(x, y + h - 1, bl, color, true, default, false, version);
        Set(x + w - 1, y + h - 1, br, color, true, default, false, version);

        for (int dx = 1; dx < w - 1; dx++)
        {
            Set(x + dx, y, hz, color, true, default, false, version);
            Set(x + dx, y + h - 1, hz, color, true, default, false, version);
        }
        for (int dy = 1; dy < h - 1; dy++)
        {
            Set(x, y + dy, vt, color, true, default, false, version);
            Set(x + w - 1, y + dy, vt, color, true, default, false, version);
        }

        if (!string.IsNullOrWhiteSpace(title))
        {
            string decorated = $"{{ {title} }}";
            int pad = (w - 2 - decorated.Length) / 2;
            int tx = x + 1 + pad;
            if (tx >= x + 1 && tx + decorated.Length <= x + w - 1)
                Write(tx, y, decorated, (0, 140, 0), true, default, false, version);
        }
    }

    // Write "label" in labelColor, then "value" (padded to fill remaining width) in valueColor.
    public void WriteLine(int x, int y, string label, (int r, int g, int b) labelColor,
        string value, (int r, int g, int b) valueColor, int width, int version)
    {
        Write(x, y, label, labelColor, true, default, false, HashCode.Combine(version, -1));
        int valWidth = Math.Max(0, width - label.Length);
        string padded = value.PadRight(valWidth);
        if (padded.Length > valWidth) padded = padded[..valWidth];
        Write(x + label.Length, y, padded, valueColor, true, default, false, HashCode.Combine(version, value.GetHashCode()));
    }

    public void Flush()
    {
        Ensure();
        lock (GUI.ConsoleLock)
        {
            bool inRun = false;
            for (int y = 0; y < _h; y++)
            {
                inRun = false;
                for (int x = 0; x < _w; x++)
                {
                    ref Cell back = ref _back![x, y];
                    ref Cell front = ref _front![x, y];

                    bool dirty = back.version != 0 && (front.version != back.version ||
                        front.ch != back.ch || front.hasFg != back.hasFg || front.hasBg != back.hasBg);

                    if (dirty)
                    {
                        if (!inRun)
                        {
                            Console.SetCursorPosition(x, y);
                            inRun = true;
                        }
                        if (back.hasBg) _sb.Append(GUI.SetBackgroundColor(back.bg.r, back.bg.g, back.bg.b));
                        if (back.hasFg) _sb.Append(GUI.SetForegroundColor(back.fg.r, back.fg.g, back.fg.b));
                        _sb.Append(back.ch);
                        if (back.hasFg || back.hasBg) _sb.Append(GUI.ResetColor());
                        front = back;
                    }
                    else
                        FlushRun(ref inRun);

                    back = default;
                }
                FlushRun(ref inRun);
            }
        }
    }

    private void FlushRun(ref bool inRun)
    {
        if (!inRun) return;
        Console.Write(_sb.ToString());
        _sb.Clear();
        inRun = false;
    }
}
