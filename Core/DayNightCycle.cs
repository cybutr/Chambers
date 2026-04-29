public enum GradientDirection { TL_BR, BR_TL, BL_TR, TR_BL }

public class DayNightCycle
{
    public double TimeOfDay { get; set; } = 8.0;
    public double Season { get; set; }
    public double SunriseTime { get; set; } = 6.0;
    public double SunsetTime { get; set; } = 18.0;
    public int DayCount { get; set; }
    public GradientDirection CurrentGradientDirection { get; set; } = GradientDirection.TL_BR;

    public static readonly double EquinoxSunrise        = 6.0;
    public static readonly double EquinoxSunset         = 18.0;
    public static readonly double SummerSolsticeSunrise = 5.0;
    public static readonly double SummerSolsticeSunset  = 21.0;
    public static readonly double WinterSolsticeSunrise = 7.0;
    public static readonly double WinterSolsticeSunset  = 17.0;

    public void Advance(double deltaTime)
    {
        TimeOfDay += deltaTime * 24.0 / 720.0;
        if (TimeOfDay >= 24.0)
        {
            TimeOfDay -= 24.0;
            DayCount++;
        }
        int daysPerSeason = 10;
        Season += deltaTime / (daysPerSeason * 720.0);
        if (Season >= 4.0) Season -= 4.0;
        UpdateSunTimes();
    }

    public void UpdateSunTimes()
    {
        double summerSolstice = 1.8;
        double winterSolstice = 3.9;
        double progress;

        if (Season <= summerSolstice) progress = Season / summerSolstice;
        else progress = (Season - summerSolstice) / (winterSolstice - summerSolstice);

        double sunriseOffset = Math.Cos(progress * Math.PI) * (WinterSolsticeSunrise - EquinoxSunrise);
        double sunsetOffset  = Math.Cos(progress * Math.PI) * (WinterSolsticeSunset  - EquinoxSunset);

        SunriseTime = Math.Round(EquinoxSunrise + sunriseOffset, 2);
        SunsetTime  = Math.Round(EquinoxSunset  + sunsetOffset,  2);

        if (Math.Abs(Season - summerSolstice) < 0.01)
        {
            SunriseTime = SummerSolsticeSunrise;
            SunsetTime  = SummerSolsticeSunset;
        }
        else if (Math.Abs(Season - winterSolstice) < 0.01)
        {
            SunriseTime = WinterSolsticeSunrise;
            SunsetTime  = WinterSolsticeSunset;
        }
    }

    public double GetTransitionProgress()
    {
        double timeOfDay = TimeOfDay % 24.0;
        double transitionDuration = 1.0;

        double sunsetStart  = SunsetTime - transitionDuration;
        if (sunsetStart < 0) sunsetStart += 24.0;
        double sunsetEnd    = SunsetTime;
        double sunriseStart = SunriseTime;
        double sunriseEnd   = SunriseTime + transitionDuration;
        if (sunriseEnd >= 24.0) sunriseEnd -= 24.0;

        double transitionProgress;
        if (IsTimeBetween(timeOfDay, sunsetStart, sunsetEnd))
        {
            double totalDuration = (sunsetEnd - sunsetStart + 24.0) % 24.0;
            transitionProgress = ((timeOfDay - sunsetStart + 24.0) % 24.0) / totalDuration;
        }
        else if (IsTimeBetween(timeOfDay, sunriseStart, sunriseEnd))
        {
            double totalDuration = (sunriseEnd - sunriseStart + 24.0) % 24.0;
            transitionProgress = 1.0 - (timeOfDay - sunriseStart + 24.0) % 24.0 / totalDuration;
        }
        else if (IsNightTime(timeOfDay, sunsetEnd, sunriseStart)) transitionProgress = 1.0;
        else transitionProgress = 0.0;

        return Math.Clamp(transitionProgress, 0.0, 1.0);
    }

    private static bool IsTimeBetween(double time, double start, double end)
    {
        if (start <= end) return time >= start && time <= end;
        else return time >= start || time <= end;
    }
    private static bool IsNightTime(double time, double sunsetEnd, double sunriseStart) => IsTimeBetween(time, sunsetEnd, sunriseStart);
    public void UpdateGradientDirection()
    {
        if (TimeOfDay > 0.0 && TimeOfDay < 6.0) CurrentGradientDirection = GradientDirection.BR_TL;
        else if (TimeOfDay > 12.0 && TimeOfDay < 18.0) CurrentGradientDirection = GradientDirection.TL_BR;
    }

    public static double EaseInOutQuad(double t)
    {
        if (t < 0.5) return 2 * t * t;
        else return -1 + (4 - 2 * t) * t;
    }
}
