using System;
using System.Text;
using Internal;
using static Internal.GUI;

// Dirty-checking terminal framebuffer. Tracks (color, cellId) per cell;
// only writes to console when a cell changes. Use anywhere independent of Map.
public class Framebuffer
{
    private ((int r, int g, int b) color, int cellId)[,]? _buf;
    private readonly StringBuilder _row = new(2048);
    // private int _lastCamX = 0;
    // private int _lastCamY = 0;

    public void Invalidate() => _buf = null;

    // getCell(worldX, worldY):
    //   null               → skip this cell (flush pending run, move on)
    //   (color, cellId, chars, fg?) → render with background color; optional fg for icons
    // cellId drives dirty-check — pass a value that changes whenever rendered content changes.
    public void Render(
        Camera cam,
        int leftPad,
        int topPad,
        Func<int, int, ((int r, int g, int b) color, int cellId, string chars, (int r, int g, int b)? fg)?> getCell)
    {
        if (_buf == null || _buf.GetLength(0) != cam.Width || _buf.GetLength(1) != cam.Height)
            _buf = new ((int r, int g, int b) color, int cellId)[cam.Width, cam.Height];

        for (int sy = 0; sy < cam.Height; sy++)
        {
            bool inRun = false;
            for (int sx = 0; sx < cam.Width; sx++)
            {
                (int wx, int wy) = cam.ToWorld(sx, sy);
                var cell = getCell(wx, wy);
                if (cell == null)
                {
                    FlushRun(ref inRun);
                    continue;
                }
                var (color, cellId, chars, fg) = cell.Value;
                ref var slot = ref _buf![sx, sy];
                if (slot.color == color && slot.cellId == cellId)
                {
                    FlushRun(ref inRun);
                    continue;
                }
                slot = (color, cellId);
                if (!inRun)
                {
                    SetCursorPosition(leftPad + sx * 2, sy + topPad);
                    inRun = true;
                }
                _row.Append(SetBackgroundColor(color.r, color.g, color.b));
                if (fg.HasValue)
                    _row.Append(SetForegroundColor(fg.Value.r, fg.Value.g, fg.Value.b));
                _row.Append(chars);
            }
            FlushRun(ref inRun);
        }
        Write(ResetColor());
    }

    private void Shift(int dx, int dy)
    {
        if (_buf == null) return;
        int w = _buf.GetLength(0);
        int h = _buf.GetLength(1);

        int sxStart = dx >= 0 ? 0 : w - 1;
        int sxEnd   = dx >= 0 ? w : -1;
        int sxStep  = dx >= 0 ? 1 : -1;
        int syStart = dy >= 0 ? 0 : h - 1;
        int syEnd   = dy >= 0 ? h : -1;
        int syStep  = dy >= 0 ? 1 : -1;

        for (int sy = syStart; sy != syEnd; sy += syStep)
        for (int sx = sxStart; sx != sxEnd; sx += sxStep)
        {
            int srcX = sx + dx;
            int srcY = sy + dy;
            _buf[sx, sy] = (srcX >= 0 && srcX < w && srcY >= 0 && srcY < h)
                ? _buf[srcX, srcY]
                : ((-1, -1, -1), -1);
        }
    }
    private void FlushRun(ref bool inRun)
    {
        if (!inRun) return;
        Write(_row.ToString());
        _row.Clear();
        inRun = false;
    }
}
