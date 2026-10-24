namespace Motorsports.Common;

/// <summary>Legacy rain impact stored on <c>Round.Rain</c> as <c>decimal(1,0)</c>.</summary>
public enum RainLevel
{
    NoRain = 0,
    NoRainButDampStart = 1,
    RainWithoutImpact = 2,
    RainWithMinorImpact = 3,
    RainWithConsiderableImpact = 4,
    FullWetEvent = 5,
    StoppedDueToHeavyRain = 6,
    CancelledDueToHeavyRain = 7,
}

public static class RainLevelDisplay
{
    public static string? Format(decimal? rain)
    {
        if (rain is not { } value)
        {
            return null;
        }

        return (RainLevel)(int)value switch
        {
            RainLevel.NoRain => "No rain",
            RainLevel.NoRainButDampStart => "No rain, but damp start",
            RainLevel.RainWithoutImpact => "Rain without impact",
            RainLevel.RainWithMinorImpact => "Rain with minor impact",
            RainLevel.RainWithConsiderableImpact => "Rain with considerable impact",
            RainLevel.FullWetEvent => "Full wet event",
            RainLevel.StoppedDueToHeavyRain => "Stopped due to heavy rain",
            RainLevel.CancelledDueToHeavyRain => "Cancelled due to heavy rain",
            _ => null,
        };
    }
}
