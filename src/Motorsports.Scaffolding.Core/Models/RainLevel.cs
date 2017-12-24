using System.ComponentModel.DataAnnotations;

namespace Motorsports.Scaffolding.Core.Models {
  public enum RainLevel {
    [Display(Name="No rain")]
    NoRain,
    [Display(Name="No rain, but damp start")]
    NoRainButDampStart,
    [Display(Name="Rain without impact")]
    RainWithoutImpact,
    [Display(Name="Rain with minor impact")]
    RainWithMinorImpact,
    [Display(Name="Rain with considerable impact")]
    RainWithConsiderableImpact,
    [Display(Name="Full wet event")]
    FullWetEvent,
    [Display(Name="Stopped due to heavy rain")]
    StoppedDueToHeavyRain,
    [Display(Name="Cancelled due to heavy rain")]
    CancelledDueToHeavyRain
  }
}