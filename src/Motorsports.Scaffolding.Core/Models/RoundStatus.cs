using System.ComponentModel.DataAnnotations;

namespace Motorsports.Scaffolding.Core.Models {
  public enum RoundStatus {
    [Display(Name="Scheduled")]
    Scheduled = 0,
    [Display(Name="Ready to watch")]
    ReadyToWatch = 1,
    [Display(Name="Postponed")]
    Postponed = 10,
    [Display(Name="Finished")]
    Finished = 20,
    [Display(Name="Stopped")]
    Stopped = 21,
    [Display(Name="Cancelled")]
    Cancelled = 22
  }
}