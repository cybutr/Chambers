public readonly struct SimContext
{
    public bool     IsNight              { get; init; }
    public double   Time                 { get; init; }
    public double   SunriseTime          { get; init; }
    public double   SunsetTime           { get; init; }
    public int      Season               { get; init; }
    public float[,] FearMap              { get; init; }
    public Func<int, int, Species?>? GetSpeciesAt { get; init; }
    public bool     EnablePredators      { get; init; }
    public bool     EnableAnimalHunting  { get; init; }
    public bool     EnableAnimalDeath    { get; init; }
    public bool     EnableAnimalBreeding { get; init; }
}
