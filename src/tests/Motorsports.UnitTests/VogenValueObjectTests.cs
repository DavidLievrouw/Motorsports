using Motorsports.Common;
using Shouldly;

namespace Motorsports.UnitTests;

public sealed class VogenValueObjectTests
{
    [Fact]
    public void Sport_name_exposes_known_instances()
    {
        new[]
        {
            SportName.F1.Value,
            SportName.FE.Value,
            SportName.IndyCar.Value,
            SportName.MotoGP.Value,
            SportName.NASCAR.Value,
            SportName.WEC.Value,
        }.ShouldBe(["F1", "FE", "IndyCar", "MotoGP", "NASCAR", "WEC"]);
    }

    [Fact]
    public void Status_name_exposes_all_known_instances()
    {
        new[]
        {
            StatusName.Scheduled.Value,
            StatusName.ReadyToWatch.Value,
            StatusName.Postponed.Value,
            StatusName.Finished.Value,
            StatusName.Stopped.Value,
            StatusName.Cancelled.Value,
        }.ShouldBe(["Scheduled", "ReadyToWatch", "Postponed", "Finished", "Stopped", "Cancelled"]);
    }
}
