using System;
using System.Collections.Generic;
using System.Linq;
using Internal;
using static Internal.GUI;

public partial class Map
{
    public void DisplayMap(bool displayGUI = true)
    {
        if (camera == null) InitializeCamera();
        int lp = displayGUI ? leftPadding : 0;
        int tp = displayGUI ? topPadding  : 2;

        int actualVw = Math.Min(camera!.Width,  width  - camera.X);
        int actualVh = Math.Min(camera!.Height, height - camera.Y);

        if (displayGUI)
        {
            int fullW  = Console.WindowWidth;
            int availH = Math.Max(0, Console.WindowHeight - GUIConfig.BottomPadding - topPadding);
            if (actualVw * 2 < fullW)  lp += (fullW  - actualVw * 2) / 2;
            if (actualVh     < availH) tp += (availH  - actualVh)     / 2;
        }

        _fb.Render(camera!, lp, tp, (wx, wy) =>
        {
            if (wx < 0 || wx >= width || wy < 0 || wy >= height) return null;
            if (displayGUI)
            {
                int sx = wx - camera.X;
                int sy = wy - camera.Y;
                if (sx == 0 || sy == 0 || sx == actualVw - 1 || sy == actualVh - 1)
                    return null;
            }
            var color      = CompositeColor(wx, wy);
            var entity     = overlayData[wx, wy];
            bool showEntity = entity != EntityId.None && !IsTileUnderCloud(wx, wy);
            if (showEntity)
            {
                var def          = EntityRegistry.Get(entity);
                var s            = _species.GetAt(wx, wy);
                var (vr, vg, vb) = s?.colorVariation ?? (0, 0, 0);
                var pulse        = s?.colorPulse ?? 0f;
                var pt           = s?.colorPulseTarget ?? (255, 0, 0);
                var fg           = ColorSpectrum.BlendColor(
                                    (Math.Clamp(def.BaseColor.r + vr, 0, 255),
                                    Math.Clamp(def.BaseColor.g + vg, 0, 255),
                                    Math.Clamp(def.BaseColor.b + vb, 0, 255)),
                                    pt, pulse);
                return (color, HashCode.Combine((int)entity, color.r, color.g, color.b, vr, (int)(pulse * 20)), def.Icon, fg);
            }
            return (color, 0, "  ", null);
        });

        if (displayGUI)
        {
            DrawViewportBorder(actualVw, actualVh, lp, tp);
            DisplayGUI();
        }
    }

    private void DrawViewportBorder(int vw, int vh, int lp, int tp)
    {
        var color = TileRegistry.Get(TileId.Border).BaseColor;
        string block = SetBackgroundColor(color.r, color.g, color.b) + "  ";
        string row = string.Concat(Enumerable.Repeat(block, vw)) + ResetColor();

        SetCursorPosition(lp, tp);
        Write(row);
        SetCursorPosition(lp, tp + vh - 1);
        Write(row);

        string cell = block + ResetColor();
        for (int sy = 1; sy < vh - 1; sy++)
        {
            SetCursorPosition(lp, tp + sy);
            Write(cell);
            SetCursorPosition(lp + (vw - 1) * 2, tp + sy);
            Write(cell);
        }
    }

    private (int r, int g, int b) CompositeColor(int wx, int wy)
    {
        var color = GetColor(mapData[wx, wy], wx, wy);
        int darkness = darknessData[wx, wy];
        if (darkness > 0) color = ColorSpectrum.DarkenColor(color, darkness);
        double shadow = shadowData[wx, wy];
        if (shadow > 0 && isCloudsShadowsRendering && !IsTileUnderCloud(wx, wy))
            color = ColorSpectrum.DarkenColor(color, (int)(shadowIntensityFactor * shadow));
        double waveI = waveIntensityData[wx, wy];
        if (waveI > 0)
        {
            var waveColor = GetColor(TileId.Ocean, wx, wy);
            if (darkness > 0) waveColor = ColorSpectrum.DarkenColor(waveColor, darkness);
            if (shadow > 0 && isCloudsShadowsRendering && !IsTileUnderCloud(wx, wy))
                waveColor = ColorSpectrum.DarkenColor(waveColor, (int)(shadowIntensityFactor * shadow));
            color = ColorSpectrum.BlendColor(color, waveColor, waveI);
        }
        if (isCloudsRendering)
        {
            (int cloudX, int cloudY) = MapDataCordsToCloudData(wx, wy);
            if (IsInCloudBounds(cloudX, cloudY) && cloudData[cloudX, cloudY] != CloudType.None && !IsThereBorderTile(wx, wy))
                color = GetCloudColor(cloudX, cloudY);
        }
        return color;
    }

    #region display functions
    private bool IsThereAnOverlayTile(int x, int y) => overlayData[x, y] != EntityId.None;
    private bool IsThereBorderTile(int x, int y) => mapData[x, y] == TileId.Border;
    private (int r, int g, int b) GetColor(TileId tile, int x, int y)
    {
        var def = TileRegistry.Get(tile);
        var baseColor = def.BaseColor;

        if (tile == TileId.Border) return baseColor;

        if (def.IsWater)
        {
            double waterTemp = (avarageTempature - 0.6) * 100;
            double waterHum  = (avarageHumidity  - 0.6) * 8;
            return (
                Math.Clamp(baseColor.r - (int)waterHum,  0, 255),
                Math.Clamp(baseColor.g + (int)waterTemp, 0, 255),
                Math.Clamp(baseColor.b - (int)waterHum,  0, 255)
            );
        }

        if (tile == TileId.Mountain || tile == TileId.MountainDeep)
        {
            double mountainTemp = (avarageTempature - 0.5) * 60;
            double mountainHum  = (avarageHumidity  - 0.5) * 28;
            return (
                Math.Clamp(baseColor.r + (int)mountainTemp,    0, 255),
                Math.Clamp(baseColor.g + (int)mountainHum,     0, 255),
                Math.Clamp(baseColor.b - (int)mountainHum * 2, 0, 255)
            );
        }

        int rAdj = 0, gAdj = 0, bAdj = 0;
        if (conf.EnableTempatureBiomeChanges)
        {
            switch (temperatureData[x, y])
            {
                case 1: rAdj -= 20; bAdj -= 20; break;
                case 2: gAdj -=  5; bAdj -= 15; break;
                case 3: gAdj +=  5; bAdj += 10; break;
                case 4: rAdj +=  5; gAdj += 10; break;
                case 5: rAdj += 10; gAdj += 15; break;
            }
        }
        if (conf.EnableHumidityBiomeChanges)
        {
            switch (humidityData[x, y])
            {
                case 1: gAdj -= 15; bAdj -=  5; break;
                case 2: gAdj -= 10;              break;
                case 3:                           break;
                case 4: gAdj += 15; bAdj += 10; break;
                case 5: gAdj += 20; bAdj +=  5; break;
            }
        }
        var adjusted = (
            Math.Clamp(baseColor.r + rAdj, 0, 255),
            Math.Clamp(baseColor.g + gAdj, 0, 255),
            Math.Clamp(baseColor.b + bAdj, 0, 255)
        );
        if (tile == TileId.Plains || tile == TileId.Forest)
        {
            double[] dh = [0,    -12,  -35,  20  ];
            double[] ds = [0.10, -0.08, 0.05, -0.30];
            double[] dv = [0.06,  0.03,-0.05, -0.12];
            double sn = dayNight.Season % 4.0;
            int    si = (int)sn;
            double sf = sn - si;
            int    ni = (si + 1) % 4;
            double hA = dh[si] * (1 - sf) + dh[ni] * sf;
            double sA = ds[si] * (1 - sf) + ds[ni] * sf;
            double vA = dv[si] * (1 - sf) + dv[ni] * sf;
            var (h, s, v) = ColorSpectrum.ToHsv(adjusted);
            return ColorSpectrum.FromHsv((h + hA + 360) % 360, Math.Clamp(s + sA, 0, 1), Math.Clamp(v + vA, 0, 1));
        }
        return adjusted;
    }
    #endregion
    #region temperature and humidity noise
    public void RenderTemperatureNoise()
    {
        if (camera == null) InitializeCamera();
        _fb.Render(camera!, leftPadding, topPadding, (wx, wy) =>
        {
            if (wx < 0 || wx >= width || wy < 0 || wy >= height) return null;
            int v = temperatureData[wx, wy];
            return (TemperatureZoneToColor(v), v, "  ", null);
        });
    }
    public void RenderHumidityNoise()
    {
        if (camera == null) InitializeCamera();
        _fb.Render(camera!, leftPadding, topPadding, (wx, wy) =>
        {
            if (wx < 0 || wx >= width || wy < 0 || wy >= height) return null;
            int v = humidityData[wx, wy];
            return (HumidityZoneToColor(v), v, "  ", null);
        });
    }
    private (int r, int g, int b) TemperatureZoneToColor(int zone)
    {
        return zone switch
        {
            1 => ColorSpectrum.LIGHT_BLUE,
            2 => ColorSpectrum.BLUE,
            3 => ColorSpectrum.BLUE_VIOLET,
            4 => ColorSpectrum.PURPLE,
            5 => ColorSpectrum.RED,
            _ => ColorSpectrum.WHITE
        };
    }
    private (int r, int g, int b) HumidityZoneToColor(int zone)
    {
        return zone switch
        {
            1 => ColorSpectrum.ANTIQUE_WHITE,
            2 => ColorSpectrum.LIGHT_YELLOW,
            3 => ColorSpectrum.YELLOW,
            4 => ColorSpectrum.GREEN,
            5 => ColorSpectrum.DARK_GREEN,
            _ => ColorSpectrum.WHITE
        };
    }
    #endregion
}
