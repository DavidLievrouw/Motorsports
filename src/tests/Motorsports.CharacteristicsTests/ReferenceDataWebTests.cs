using System.Net;
using Shouldly;

namespace Motorsports.CharacteristicsTests;

public sealed partial class AuthCharacteristicsTests
{
    private static readonly string[] ReferenceRoutes =
    [
        "/sports",
        "/sports/create",
        "/sports/details/F1",
        "/sports/edit/F1",
        "/venues",
        "/venues/create",
        "/venues/details/Spa",
        "/venues/edit/Spa",
        "/teams",
        "/teams/create",
        "/teams/details/1",
        "/teams/edit/1",
        "/teams/delete/1",
        "/participants",
        "/participants/create",
        "/participants/details/1",
        "/participants/edit/1",
    ];

    [Fact]
    public async Task Reference_data_routes_redirect_anonymous_users_to_login()
    {
        await using var app = await StartAsync();
        var client = app.NewClient();

        foreach (var route in ReferenceRoutes)
        {
            var response = await client.GetAsync(route, TestContext.Current.CancellationToken);

            response.StatusCode.ShouldBe(HttpStatusCode.Redirect, route);
            LocationPathOf(response).ShouldStartWith("/login", Case.Sensitive, route);
        }
    }

    [Fact]
    public async Task Sport_images_are_served_by_endpoint_flags_prefix_countries_and_change_password_keeps_layout()
    {
        await using var app = await StartAsync();
        var anonymousClient = app.NewClient(); // starts the host, which applies the migrations
        await fixture.ExecuteSqlAsync(
            "INSERT INTO `Country` (ISO, Name, NiceName) VALUES ('GB','UNITED KINGDOM','United Kingdom');"
                + "INSERT INTO `Sport` (Name, FullName, Image) VALUES ('F1','Formula One', 0x89504E470D0A1A0A01);"
                + "INSERT INTO `Venue` (Name, Country) VALUES ('Silverstone', 'GB')"
        );
        await SeedUserAsync("editor");
        var anonymous = await anonymousClient.GetAsync("/sports/image/F1", TestContext.Current.CancellationToken);
        anonymous.StatusCode.ShouldBe(HttpStatusCode.Redirect);

        var client = app.NewClient();
        (await LoginAsync(client, "editor", Password)).StatusCode.ShouldBe(HttpStatusCode.Redirect);

        var image = await client.GetAsync("/sports/image/F1", TestContext.Current.CancellationToken);
        image.StatusCode.ShouldBe(HttpStatusCode.OK);
        image.Content.Headers.ContentType!.MediaType.ShouldBe("image/png");
        (await image.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken)).Length.ShouldBe(9);
        (await client.GetAsync("/sports/image/Nope", TestContext.Current.CancellationToken)).StatusCode.ShouldBe(
            HttpStatusCode.NotFound
        );

        var list = await client.GetStringAsync("/sports", TestContext.Current.CancellationToken);
        list.ShouldContain("/sports/image/F1");
        list.ShouldNotContain("base64");

        var venues = await client.GetStringAsync("/venues", TestContext.Current.CancellationToken);
        System.Net.WebUtility.HtmlDecode(venues).ShouldContain("\U0001F1EC\U0001F1E7 United Kingdom");

        var password = await client.GetStringAsync("/account/change-password", TestContext.Current.CancellationToken);
        password.ShouldContain("mud-appbar");
        password.ShouldContain("ms-footer");
    }

    [Fact]
    public async Task Reference_data_pages_render_for_signed_in_users_and_unknown_ids_return_not_found()
    {
        await using var app = await StartAsync();
        await fixture.ExecuteSqlAsync(
            "INSERT INTO `Country` (ISO, Name, NiceName) VALUES ('GB','UNITED KINGDOM','United Kingdom');"
                + "INSERT INTO `Sport` (Name, FullName) VALUES ('F1','Formula One');"
                + "INSERT INTO `Venue` (Name, Country) VALUES ('Autódromo Hermanos Rodríguez, Mexico City', 'GB')"
        );
        await SeedUserAsync("editor");
        var client = app.NewClient();
        (await LoginAsync(client, "editor", Password)).StatusCode.ShouldBe(HttpStatusCode.Redirect);

        var list = await client.GetAsync("/sports", TestContext.Current.CancellationToken);
        list.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await list.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldContain("Formula One");
        var detail = await client.GetAsync("/sports/details/F1", TestContext.Current.CancellationToken);
        detail.StatusCode.ShouldBe(HttpStatusCode.OK);
        var accentedVenue = await client.GetAsync(
            $"/venues/details/{Uri.EscapeDataString("Autódromo Hermanos Rodríguez, Mexico City")}",
            TestContext.Current.CancellationToken
        );
        accentedVenue.StatusCode.ShouldBe(HttpStatusCode.OK);
        System
            .Net.WebUtility.HtmlDecode(
                await accentedVenue.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)
            )
            .ShouldContain("Autódromo Hermanos Rodríguez, Mexico City");

        foreach (
            var route in new[]
            {
                "/sports/details/Nope",
                "/venues/details/Nope",
                "/teams/details/99",
                "/participants/details/99",
            }
        )
        {
            var response = await client.GetAsync(route, TestContext.Current.CancellationToken);
            response.StatusCode.ShouldBe(HttpStatusCode.NotFound, route);
        }

        foreach (var route in new[] { "/venues", "/teams", "/participants", "/sports/create", "/teams/create" })
        {
            (await client.GetAsync(route, TestContext.Current.CancellationToken)).StatusCode.ShouldBe(
                HttpStatusCode.OK,
                route
            );
        }
    }
}
