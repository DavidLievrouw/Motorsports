using NetArchTest.Rules;
using Shouldly;

namespace Motorsports.ArchitectureTests;

public class LayerTests
{
    private static readonly System.Reflection.Assembly[] ReferenceSlices =
    [
        Motorsports.Sports.SportsModule.Assembly,
        Motorsports.Venues.VenuesModule.Assembly,
        Motorsports.Teams.TeamsModule.Assembly,
        Motorsports.Participants.ParticipantsModule.Assembly,
        Motorsports.Seasons.SeasonsModule.Assembly,
        Motorsports.Rounds.RoundsModule.Assembly,
    ];

    private static readonly string[] SliceNamespaces =
    [
        "Motorsports.Auth",
        "Motorsports.Sports",
        "Motorsports.Venues",
        "Motorsports.Teams",
        "Motorsports.Participants",
        "Motorsports.Seasons",
        "Motorsports.Rounds",
    ];

    [Fact]
    public void Reference_slices_depend_only_on_common_infrastructure_and_shared_ui()
    {
        foreach (var assembly in ReferenceSlices)
        {
            var own = assembly.GetName().Name!;
            var forbidden = SliceNamespaces.Where(n => n != own).Append("Motorsports.Web").ToArray();
            var result = Types.InAssembly(assembly).ShouldNot().HaveDependencyOnAny(forbidden).GetResult();

            result.IsSuccessful.ShouldBeTrue($"{own} has a forbidden dependency");
        }
    }

    [Fact]
    public void Infrastructure_and_common_do_not_depend_on_reference_slices()
    {
        var forbidden = SliceNamespaces.Skip(1).ToArray();
        foreach (
            var assembly in new[]
            {
                typeof(Motorsports.Common.Result<>).Assembly,
                typeof(Motorsports.Infrastructure.MotorsportsDbContext).Assembly,
            }
        )
        {
            Types
                .InAssembly(assembly)
                .ShouldNot()
                .HaveDependencyOnAny(forbidden)
                .GetResult()
                .IsSuccessful.ShouldBeTrue();
        }
    }

    [Fact]
    public void Every_routable_page_in_the_reference_slices_has_an_explicit_authorize_attribute()
    {
        var routed = ReferenceSlices
            .SelectMany(a => a.GetTypes())
            .Where(t => t.GetCustomAttributes(typeof(Microsoft.AspNetCore.Components.RouteAttribute), false).Length > 0)
            .ToList();

        routed.Count.ShouldBeGreaterThanOrEqualTo(14);
        routed
            .Where(t =>
                t.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), false).Length == 0
            )
            .Select(t => t.FullName)
            .ShouldBeEmpty();
    }

    [Fact]
    public void Common_does_not_depend_on_other_projects()
    {
        var result = Types
            .InAssembly(typeof(Motorsports.Common.Result<>).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("Motorsports.Infrastructure", "Motorsports.Ui", "Motorsports.Web")
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
