using System;
using System.Collections;
public static class Perlin
{
    private readonly static int[] permutation = { 151,160,137,91,90,15,
    131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,
    8,99,37,240,21,10,23,190, 6,148,247,120,234,75,0,26,
    197,62,94,252,219,203,117,35,11,32,57,177,33,88,237,149,
    56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,
    48,27,166,77,146,158,231,83,111,229,122,60,211,133,230,
    220,105,92,41,55,46,245,40,244,102,143,54, 65,25,63,161,
    1,216,80,73,209,76,132,187,208, 89,18,169,200,196,135,
    130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,
    226,250,124,123,5,202,38,147,118,126,255,82,85,212,207,
    206,59,227,47,16,58,17,182,189,28,42,223,183,170,213,119,
    248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,
    9,129,22,39,253, 19,98,108,110,79,113,224,232,178,185,
    112,104,218,246,97,228,251,34,242,193,238,210,144,12,191,
    179,162,241, 81,51,145,235,249,14,239,107,49,192,214, 31,
    181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,
    254,138,236,205,93,222,114,67,29,24,72,243,141,128,195,
    78,66,215,61,156,180
    };

    private static int[] p;

    static Perlin()
    {
        p = new int[512];
        RandomizePermutation();
    }

    private static void RandomizePermutation()
    {
        Random rng = new Random();
        for (int i = 0; i < 512; i++)
        {
            p[i] = permutation[rng.Next(256)];
        }
    }

    public static double[,] GeneratePerlinNoise(int width, int height, double scale)
    {
        RandomizePermutation(); // Randomize permutation each time noise is generated
        double[,] noise = new double[width, height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double sampleX = x / scale;
                double sampleY = y / scale;
                double perlinValue = PerlinNoise(sampleX, sampleY);
                noise[x, y] = perlinValue;
            }
        }
        return noise;
    }

    private static double PerlinNoise(double x, double y)
    {
        int xi = (int)Math.Floor(x) & 255;
        int yi = (int)Math.Floor(y) & 255;
        double xf = x - Math.Floor(x);
        double yf = y - Math.Floor(y);
        double u = Fade(xf);
        double v = Fade(yf);

        int aa = p[p[xi] + yi];
        int ab = p[p[xi] + yi + 1];
        int ba = p[p[xi + 1] + yi];
        int bb = p[p[xi + 1] + yi + 1];

        double x1, x2, y1;
        x1 = Lerp(Grad(aa, xf, yf), Grad(ba, xf - 1, yf), u);
        x2 = Lerp(Grad(ab, xf, yf - 1), Grad(bb, xf - 1, yf - 1), u);
        y1 = Lerp(x1, x2, v);

        return (y1 + 1) / 2; // Normalize to 0.0 - 1.0
    }

    private static double Fade(double t)
    {
        return t * t * t * (t * (t * 6 - 15) + 10);
    }

    private static double Lerp(double a, double b, double t)
    {
        return a + t * (b - a);
    }

    private static double Grad(int hash, double x, double y)
    {
        int h = hash & 15;
        double u = h < 8 ? x : y;
        double v = h < 4 ? y : h == 12 || h == 14 ? x : 0;
        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }
}