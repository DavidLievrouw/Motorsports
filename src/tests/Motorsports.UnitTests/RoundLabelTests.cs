using Motorsports.Common;
using Shouldly;

namespace Motorsports.UnitTests;

public class RoundLabelTests
{
    [Fact]
    public void Format_uses_number_and_optional_name()
    {
        RoundLabel.Format(12, null).ShouldBe("12");
        RoundLabel.Format(12, "  ").ShouldBe("12");
        RoundLabel.Format(12, "Indianapolis 500").ShouldBe("12 - Indianapolis 500");
    }
}
