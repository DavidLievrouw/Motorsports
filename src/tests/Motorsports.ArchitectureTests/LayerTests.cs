using NetArchTest.Rules;
using Shouldly;

namespace Motorsports.ArchitectureTests;

public class LayerTests
{
    [Fact]
    public void Common_does_not_depend_on_other_projects()
    {
        var result = Types
            .InAssembly(typeof(Motorsports.Common.Result<>).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("Motorsports.Infrastructure", "Motorsports.Web")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Infrastructure_does_not_depend_on_web()
    {
        var result = Types
            .InAssembly(typeof(Motorsports.Infrastructure.MotorsportsDbContext).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Motorsports.Web")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Infrastructure_does_not_depend_on_slices()
    {
        var result = Types
            .InAssembly(typeof(Motorsports.Infrastructure.MotorsportsDbContext).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Motorsports.Auth")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Auth_slice_does_not_depend_on_web()
    {
        var result = Types
            .InAssembly(typeof(Motorsports.Auth.AuthOptions).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Motorsports.Web")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }
}
