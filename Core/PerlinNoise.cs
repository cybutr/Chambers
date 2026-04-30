using System;
using System.Collections;
public static class Perlin
{
    private readonly static int[] permutation = [ 151,160,137,91,90,15,
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
    ];

    private static readonly int[][] grad3 = [
        [1,1,0], [-1,1,0], [1,-1,0], [-1,-1,0],
        [1,0,1], [-1,0,1], [1,0,-1], [-1,0,-1],
        [0,1,1], [0,-1,1], [0,1,-1], [0,-1,-1]
    ];

    private static int[] p;
    static Perlin()
    {
        p = new int[512];
        for (int i = 0; i < 512; i++)
        {
            p[i] = permutation[i & 255];
        }
    }

    private static void RandomizePermutation(int seed)
    {
        Random rng = new(seed);
        for (int i = 0; i < 512; i++)
        {
            p[i] = permutation[rng.Next(256)];
        }
    }

    public static double[,] GeneratePerlinNoise(int width, int height, double scale, int seed)
    {
        RandomizePermutation(seed); // Randomize permutation each time noise is generated
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

    private static double Fade(double t) => t * t * t * (t * (t * 6 - 15) + 10);

    private static double Lerp(double a, double b, double t) => a + t * (b - a);

    private static double Grad(int hash, double x, double y)
    {
        int h = hash & 15;
        double u = h < 8 ? x : y;
        double v = h < 4 ? y : h == 12 || h == 14 ? x : 0;
        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }
    
    // Generate Simplex Noise with fBm (multiple octaves)
    public static double[,] GenerateSimplexFBM(int width, int height, float scale, int octaves, float persistence, float lacunarity, int seed)
    {
        RandomizePermutation(seed);
        double[,] noiseMap = new double[width, height];
        
        float maxNoiseHeight = 0;
        float amplitude = 1;
        float frequency = 1;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;
                
                // Generate noise for each octave and sum
                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = x / scale * frequency;
                    float sampleY = y / scale * frequency;
                    
                    // Get noise value using simplex noise
                    double simplexValue = SimplexNoise(sampleX, sampleY);
                    noiseHeight += (float)simplexValue * amplitude;
                    
                    // Prepare for the next octave
                    amplitude *= persistence;
                    frequency *= lacunarity;
                }
                
                if (noiseHeight > maxNoiseHeight) maxNoiseHeight = noiseHeight;
                
                noiseMap[x, y] = noiseHeight;
            }
        }
        
        // Normalize the noise map
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                noiseMap[x, y] = (noiseMap[x, y] + 1) / (2f * maxNoiseHeight);
            }
        }
        
        return noiseMap;
    }
    
    // Simplex noise implementation
    private static double SimplexNoise(float x, float y)
    {
        const double F2 = 0.366025403; // 0.5*(sqrt(3.0)-1.0)
        const double G2 = 0.211324865; // (3.0-sqrt(3.0))/6.0
        
        // Skew input space
        double s = (x + y) * F2;
        int i = FastFloor(x + s);
        int j = FastFloor(y + s);
        
        double t = (i + j) * G2;
        double X0 = i - t;
        double Y0 = j - t;
        double x0 = x - X0;
        double y0 = y - Y0;
        
        // Determine simplex cell
        int i1, j1;
        if (x0 > y0) { i1 = 1; j1 = 0; } // lower triangle
        else { i1 = 0; j1 = 1; } // upper triangle
        
        double x1 = x0 - i1 + G2;
        double y1 = y0 - j1 + G2;
        double x2 = x0 - 1.0 + 2.0 * G2;
        double y2 = y0 - 1.0 + 2.0 * G2;
        
        // Calculate noise contribution from each corner
        int ii = i & 255;
        int jj = j & 255;
        int gi0 = p[ii + p[jj]] % 12;
        int gi1 = p[ii + i1 + p[jj + j1]] % 12;
        int gi2 = p[ii + 1 + p[jj + 1]] % 12;
        
        double n0, n1, n2;
        
        // Calculate noise contributions from each corner
        double t0 = 0.5 - x0 * x0 - y0 * y0;
        if (t0 < 0) n0 = 0.0;
        else {
            t0 *= t0;
            n0 = t0 * t0 * Dot(grad3[gi0], x0, y0);
        }
        
        double t1 = 0.5 - x1 * x1 - y1 * y1;
        if (t1 < 0) n1 = 0.0;
        else {
            t1 *= t1;
            n1 = t1 * t1 * Dot(grad3[gi1], x1, y1);
        }
        
        double t2 = 0.5 - x2 * x2 - y2 * y2;
        if (t2 < 0) n2 = 0.0;
        else {
            t2 *= t2;
            n2 = t2 * t2 * Dot(grad3[gi2], x2, y2);
        }
        
        // Add contributions from each corner and scale to [-1,1]
        return 70.0 * (n0 + n1 + n2);
    }
    
    private static int FastFloor(double x) => x > 0 ? (int)x : (int)x - 1;
    
    private static double Dot(int[] g, double x, double y) => g[0] * x + g[1] * y;
}