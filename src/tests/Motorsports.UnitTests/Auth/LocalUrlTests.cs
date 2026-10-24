using Motorsports.Common;
using Shouldly;

namespace Motorsports.UnitTests.Auth;

public class LocalUrlTests
{
    [Theory]
    [InlineData("/")]
    [InlineData("/seasons/1")]
    [InlineData("/rounds?x=1&y=/z")]
    public void Relative_paths_are_local(string url) => LocalUrl.IsLocal(url).ShouldBeTrue();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("//evil.com")]
    [InlineData("/\\evil.com")]
    [InlineData("\\\\evil.com")]
    [InlineData("https://evil.com")]
    [InlineData("javascript:alert(1)")]
    [InlineData("relative/path")]
    [InlineData("/ok\r\nSet-Cookie: x=1")]
    [InlineData("/a\\b")]
    public void Everything_else_is_not_local(string? url) => LocalUrl.IsLocal(url).ShouldBeFalse();

    [Fact]
    public void OrDefault_falls_back_for_unsafe_values()
    {
        LocalUrl.OrDefault("//evil.com").ShouldBe("/");
        LocalUrl.OrDefault("/home").ShouldBe("/home");
    }
}
