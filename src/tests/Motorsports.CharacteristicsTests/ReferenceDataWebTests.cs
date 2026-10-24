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
        "/seasons",
        "/seasons/create",
        "/seasons/details/1",
        "/seasons/edit/1",
        "/seasons/delete/1",
        "/seasons/1/entries",
        "/seasons/1/entries/create",
        "/season-entries/details/1/1",
        "/season-entries/edit/1/1",
        "/season-entries/delete/1/1",
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
        var missingImage = await client.GetAsync("/sports/image/Nope", TestContext.Current.CancellationToken);
        missingImage.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        missingImage.Headers.Location!.OriginalString.ShouldBe("/img/notfound.png");
        var fallback = await client.GetAsync("/img/notfound.png", TestContext.Current.CancellationToken);
        fallback.StatusCode.ShouldBe(HttpStatusCode.OK);
        fallback.Content.Headers.ContentType!.MediaType.ShouldBe("image/png");

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
                + "INSERT INTO `Venue` (Name, Country) VALUES ('Autódromo Hermanos Rodríguez, Mexico City', 'GB');"
                + "INSERT INTO `Team` (Name, Sport, Country) VALUES ('Ferrari', 'F1', 'GB');"
                + "INSERT INTO `Season` (Sport, Label) VALUES ('F1', '2026');"
                + "INSERT INTO `SeasonEntry` (Season, Team, Name) VALUES (1, 1, 'Scuderia Ferrari');"
                + "INSERT INTO `Participant` (Title, FirstName, LastName, Country) VALUES ('', 'Alex', 'Driver', 'GB');"
                + "INSERT INTO `SeasonWinner` (Season, Participant) VALUES (1, 1);"
                + "INSERT INTO `Status` (Name, Step) VALUES ('Finished', 20), ('Scheduled', 0);"
                + "INSERT INTO `Round` (`Date`, Number, Name, Season, Venue, Status, Rating, Rain) VALUES "
                + "('2026-03-31', 1, 'Completed round', 1, 'Autódromo Hermanos Rodríguez, Mexico City', 'Finished', 6, 2), "
                + "('2026-04-01', 2, 'Upcoming round', 1, 'Autódromo Hermanos Rodríguez, Mexico City', 'Scheduled', NULL, NULL)"
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
        var venueMarkup = System.Net.WebUtility.HtmlDecode(
            await accentedVenue.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)
        );
        venueMarkup.ShouldContain("Autódromo Hermanos Rodríguez, Mexico City");
        venueMarkup.ShouldContain("March 31, 2026");
        venueMarkup.ShouldContain("6 / 10");
        venueMarkup.ShouldContain("1 - Completed round");
        venueMarkup.ShouldContain("2 - Upcoming round");
        venueMarkup.ShouldContain("Rain without impact");
        venueMarkup.ShouldNotContain("2026-03-31");

        var sportMarkup = System.Net.WebUtility.HtmlDecode(
            await (
                await client.GetAsync("/sports/details/F1", TestContext.Current.CancellationToken)
            ).Content.ReadAsStringAsync(TestContext.Current.CancellationToken)
        );
        sportMarkup.ShouldContain("April 1, 2026");

        var seasonMarkup = System.Net.WebUtility.HtmlDecode(
            await (
                await client.GetAsync("/seasons/details/1", TestContext.Current.CancellationToken)
            ).Content.ReadAsStringAsync(TestContext.Current.CancellationToken)
        );
        seasonMarkup.ShouldContain("March 31, 2026");

        var participantMarkup = System.Net.WebUtility.HtmlDecode(
            await (
                await client.GetAsync("/participants/details/1", TestContext.Current.CancellationToken)
            ).Content.ReadAsStringAsync(TestContext.Current.CancellationToken)
        );
        participantMarkup.ShouldContain("Sports");
        participantMarkup.ShouldContain("F1");

        var seasonEntryEdit = System.Net.WebUtility.HtmlDecode(
            await (
                await client.GetAsync("/season-entries/edit/1/1", TestContext.Current.CancellationToken)
            ).Content.ReadAsStringAsync(TestContext.Current.CancellationToken)
        );
        seasonEntryEdit.ShouldContain("Ferrari (2026 - Scuderia Ferrari)");
        seasonEntryEdit.ShouldContain("readonly");

        foreach (
            var route in new[]
            {
                "/sports/details/Nope",
                "/venues/details/Nope",
                "/teams/details/99",
                "/participants/details/99",
                "/seasons/details/99",
                "/season-entries/details/99/99",
            }
        )
        {
            var response = await client.GetAsync(route, TestContext.Current.CancellationToken);
            response.StatusCode.ShouldBe(HttpStatusCode.NotFound, route);
        }

        foreach (
            var route in new[]
            {
                "/venues",
                "/teams",
                "/participants",
                "/sports/create",
                "/teams/create",
                "/seasons",
                "/seasons/create",
                "/seasons/details/1",
                "/seasons/edit/1",
                "/seasons/delete/1",
                "/seasons/1/entries",
                "/seasons/1/entries/create",
                "/season-entries/details/1/1",
                "/season-entries/edit/1/1",
                "/season-entries/delete/1/1",
            }
        )
        {
            (await client.GetAsync(route, TestContext.Current.CancellationToken)).StatusCode.ShouldBe(
                HttpStatusCode.OK,
                route
            );
        }
    }

    [Fact]
    public async Task Signed_in_header_places_account_actions_in_the_profile_menu()
    {
        await using var app = await StartAsync();
        await SeedUserAsync("editor");
        var client = app.NewClient();
        (await LoginAsync(client, "editor", Password)).StatusCode.ShouldBe(HttpStatusCode.Redirect);

        var home = await client.GetStringAsync("/", TestContext.Current.CancellationToken);

        home.ShouldContain("mud-menu");
        home.ShouldContain("id=\"logout-form\"");
        home.ShouldNotContain("/account/change-password");
        home.ShouldNotContain("Log out");
    }
}
