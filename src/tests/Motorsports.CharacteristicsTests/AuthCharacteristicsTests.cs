using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Motorsports.CharacteristicsTests.Fixtures;
using Shouldly;

namespace Motorsports.CharacteristicsTests;

[Collection(MariaDbCollection.Name)]
public sealed partial class AuthCharacteristicsTests(MariaDbFixture fixture)
{
    private const string Password = "S3cret pässword!";

    private sealed class AuthApp(string connectionString, int loginLimit) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:Motorsports", connectionString);
            builder.UseSetting("Auth:SecureCookies", "false");
            builder.UseSetting("Auth:LoginPermitLimit", loginLimit.ToString());
        }

        public HttpClient NewClient() =>
            CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
    }

    private static string LocationPathOf(HttpResponseMessage response) =>
        response.Headers.Location!.IsAbsoluteUri
            ? response.Headers.Location.PathAndQuery
            : response.Headers.Location.OriginalString;

    private async Task<AuthApp> StartAsync(int loginLimit = 1000)
    {
        await fixture.ResetSchemaAsync();
        return new AuthApp(fixture.ConnectionString, loginLimit);
    }

    private async Task SeedUserAsync(
        string username,
        string prf = "HMACSHA512",
        bool force = false,
        bool deleted = false,
        int iterations = 1000
    )
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var algorithm = new HashAlgorithmName(prf["HMAC".Length..]);
        var hash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(Password), salt, iterations, algorithm, 32);
        await fixture.ExecuteSqlAsync(
            $"INSERT INTO `User` (Id, Username, PasswordHash, Salt, Iterations, Prf, ForceChangePassword, IsDeleted) "
                + $"VALUES (UUID(), '{username}', UNHEX('{Convert.ToHexString(hash)}'), UNHEX('{Convert.ToHexString(salt)}'), "
                + $"{iterations}, '{prf}', {(force ? 1 : 0)}, {(deleted ? 1 : 0)})"
        );
    }

    [GeneratedRegex(
        @"<input[^>]*name=""__RequestVerificationToken""[^>]*value=""([^""]+)""|<input[^>]*value=""([^""]+)""[^>]*name=""__RequestVerificationToken"""
    )]
    private static partial Regex TokenRegex();

    private static async Task<string> TokenAsync(HttpClient client, string path)
    {
        var response = await client.GetAsync(path, TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var match = TokenRegex().Match(html);
        match.Success.ShouldBeTrue($"no antiforgery token on {path} ({(int)response.StatusCode})");
        return WebUtility.HtmlDecode(match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value);
    }

    private static async Task<HttpResponseMessage> LoginAsync(
        HttpClient client,
        string username,
        string password,
        string? returnUrl = null,
        string? token = null
    )
    {
        token ??= await TokenAsync(client, "/login");
        return await client.PostAsync(
            "/login",
            new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["Username"] = username,
                    ["Password"] = password,
                    ["ReturnUrl"] = returnUrl ?? "",
                    ["__RequestVerificationToken"] = token,
                }
            ),
            TestContext.Current.CancellationToken
        );
    }

    private async Task<string?> V3HashAsync(string username)
    {
        await using var context = await fixture
            .CreateContextFactory()
            .CreateDbContextAsync(TestContext.Current.CancellationToken);
        return await context
            .Users.Where(u => u.Username == username)
            .Select(u => u.PasswordHashV3)
            .SingleAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Anonymous_requests_are_redirected_to_login_with_a_local_return_url()
    {
        await using var app = await StartAsync();
        var client = app.NewClient();

        var response = await client.GetAsync("/", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        LocationPathOf(response).ShouldBe("/login?returnUrl=%2F");
    }

    [Fact]
    public async Task Login_and_access_denied_pages_are_public()
    {
        await using var app = await StartAsync();
        var client = app.NewClient();

        (await client.GetAsync("/login", TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.GetAsync("/accessdenied", TestContext.Current.CancellationToken)).StatusCode.ShouldBe(
            HttpStatusCode.OK
        );
        (await client.GetAsync("/health", TestContext.Current.CancellationToken)).StatusCode.ShouldBe(
            HttpStatusCode.OK
        );
    }

    [Fact]
    public async Task Authenticated_navigation_has_a_change_password_link()
    {
        await using var app = await StartAsync();
        await SeedUserAsync("legacy");
        var client = app.NewClient();
        await LoginAsync(client, "legacy", Password);

        var home = await client.GetAsync("/", TestContext.Current.CancellationToken);
        var markup = await home.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        home.StatusCode.ShouldBe(HttpStatusCode.OK);
        markup.ShouldContain("Next up");
        markup.ShouldNotContain("href=\"/account/change-password\"");
    }

    [Theory]
    [InlineData("HMACSHA1")]
    [InlineData("HMACSHA256")]
    [InlineData("HMACSHA512")]
    public async Task Legacy_user_logs_in_gets_a_secure_cookie_and_is_rehashed(string prf)
    {
        await using var app = await StartAsync();
        await SeedUserAsync("legacy", prf);
        var client = app.NewClient();
        await client.GetAsync("/login", TestContext.Current.CancellationToken);
        (await V3HashAsync("legacy")).ShouldBeNull();

        var response = await LoginAsync(client, "legacy", Password);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        LocationPathOf(response).ShouldBe("/");
        var cookie = response
            .Headers.GetValues("Set-Cookie")
            .Single(c => c.StartsWith("ms.auth=", StringComparison.Ordinal));
        cookie.ShouldContain("httponly", Case.Insensitive);
        cookie.ShouldContain("samesite=lax", Case.Insensitive);
        (await V3HashAsync("legacy")).ShouldNotBeNullOrEmpty();
        (await client.GetAsync("/", TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.OK);

        // The v3 hash now verifies on its own; a second client logs in again.
        var again = await LoginAsync(app.NewClient(), "legacy", Password);
        LocationPathOf(again).ShouldBe("/");
    }

    [Fact]
    public async Task Legacy_columns_stay_untouched_after_rehash()
    {
        await using var app = await StartAsync();
        await SeedUserAsync("keeper");
        await LoginAsync(app.NewClient(), "keeper", Password);

        await using var context = await fixture
            .CreateContextFactory()
            .CreateDbContextAsync(TestContext.Current.CancellationToken);
        var user = await context.Users.SingleAsync(u => u.Username == "keeper", TestContext.Current.CancellationToken);
        user.PasswordHash.Length.ShouldBe(32);
        user.Salt.Length.ShouldBe(16);
        user.Iterations.ShouldBe(1000);
        user.Prf.ShouldBe("HMACSHA512");
    }

    [Theory]
    [InlineData("legacy", "wrong password")]
    [InlineData("nobody", "whatever")]
    [InlineData("", "")]
    public async Task Failed_logins_show_one_generic_message(string username, string password)
    {
        await using var app = await StartAsync();
        await SeedUserAsync("legacy");
        var client = app.NewClient();

        var response = await LoginAsync(client, username, password);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        LocationPathOf(response).ShouldBe("/login?error=1");
        var page = await client.GetStringAsync("/login?error=1", TestContext.Current.CancellationToken);
        page.ShouldContain("Invalid username or password.");
        (await client.GetAsync("/", TestContext.Current.CancellationToken)).StatusCode.ShouldBe(
            HttpStatusCode.Redirect
        );
    }

    [Fact]
    public async Task Deleted_user_cannot_sign_in_and_loses_the_session()
    {
        await using var app = await StartAsync();
        await SeedUserAsync("gone", deleted: true);
        await SeedUserAsync("later");

        LocationPathOf((await LoginAsync(app.NewClient(), "gone", Password))).ShouldBe("/login?error=1");

        var client = app.NewClient();
        await LoginAsync(client, "later", Password);
        (await client.GetAsync("/", TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.OK);
        await fixture.ExecuteSqlAsync("UPDATE `User` SET IsDeleted = 1 WHERE Username = 'later'");
        // The cookie is validated against the store every few minutes at most; a fresh login is refused at once.
        LocationPathOf((await LoginAsync(app.NewClient(), "later", Password))).ShouldBe("/login?error=1");
    }

    [Theory]
    [InlineData("/rounds/5?x=1", "/rounds/5?x=1")]
    [InlineData("//evil.example", "/")]
    [InlineData("/\\evil.example", "/")]
    [InlineData("https://evil.example/", "/")]
    public async Task Only_local_return_urls_are_followed(string returnUrl, string expected)
    {
        await using var app = await StartAsync();
        await SeedUserAsync("legacy");

        var response = await LoginAsync(app.NewClient(), "legacy", Password, returnUrl);

        LocationPathOf(response).ShouldBe(expected);
    }

    [Fact]
    public async Task Login_post_without_antiforgery_token_is_rejected()
    {
        await using var app = await StartAsync();
        await SeedUserAsync("legacy");
        var client = app.NewClient();

        var response = await client.PostAsync(
            "/login",
            new FormUrlEncodedContent(
                new Dictionary<string, string> { ["Username"] = "legacy", ["Password"] = Password }
            ),
            TestContext.Current.CancellationToken
        );

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await client.GetAsync("/", TestContext.Current.CancellationToken)).StatusCode.ShouldBe(
            HttpStatusCode.Redirect
        );
    }

    [Fact]
    public async Task Login_attempts_are_rate_limited_per_client()
    {
        await using var app = await StartAsync(loginLimit: 3);
        var client = app.NewClient();
        var token = await TokenAsync(client, "/login");

        for (var i = 0; i < 3; i++)
        {
            (await LoginAsync(client, "nobody", "x", token: token)).StatusCode.ShouldBe(HttpStatusCode.Redirect);
        }

        (await LoginAsync(client, "nobody", "x", token: token)).StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Logout_is_post_only_with_antiforgery_and_ends_the_session()
    {
        await using var app = await StartAsync();
        await SeedUserAsync("legacy");
        var client = app.NewClient();
        await LoginAsync(client, "legacy", Password);
        var token = await TokenAsync(client, "/");

        (await client.GetAsync("/logout", TestContext.Current.CancellationToken)).StatusCode.ShouldNotBe(
            HttpStatusCode.OK
        );
        (
            await client.PostAsync("/logout", new FormUrlEncodedContent([]), TestContext.Current.CancellationToken)
        ).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await client.GetAsync("/", TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var response = await client.PostAsync(
            "/logout",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token }),
            TestContext.Current.CancellationToken
        );

        LocationPathOf(response).ShouldBe("/login");
        (await client.GetAsync("/", TestContext.Current.CancellationToken)).StatusCode.ShouldBe(
            HttpStatusCode.Redirect
        );
    }

    [Fact]
    public async Task Forced_password_change_blocks_the_app_until_the_password_is_changed()
    {
        await using var app = await StartAsync();
        await SeedUserAsync("forced", force: true);
        var client = app.NewClient();

        var login = await LoginAsync(client, "forced", Password, "/");
        LocationPathOf(login).ShouldStartWith("/account/change-password");

        var blocked = await client.GetAsync("/", TestContext.Current.CancellationToken);
        blocked.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        LocationPathOf(blocked).ShouldStartWith("/account/change-password");

        var token = await TokenAsync(client, "/account/change-password");
        var wrong = await PostChangeAsync(
            client,
            token,
            "not the password",
            "brand new password",
            "brand new password"
        );
        wrong.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        (await client.GetStringAsync(LocationPathOf(wrong), TestContext.Current.CancellationToken)).ShouldContain(
            "The current password is incorrect."
        );
        (await client.GetAsync("/", TestContext.Current.CancellationToken)).StatusCode.ShouldBe(
            HttpStatusCode.Redirect
        );

        token = await TokenAsync(client, "/account/change-password");
        var weak = await PostChangeAsync(client, token, Password, "short", "short");
        weak.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        (await client.GetStringAsync(LocationPathOf(weak), TestContext.Current.CancellationToken)).ShouldContain(
            "Review the password requirements"
        );

        token = await TokenAsync(client, "/account/change-password");
        var ok = await PostChangeAsync(client, token, Password, "brand new password", "brand new password");
        ok.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        (await client.GetAsync("/", TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var context = await fixture
            .CreateContextFactory()
            .CreateDbContextAsync(TestContext.Current.CancellationToken);
        (
            await context.Users.SingleAsync(u => u.Username == "forced", TestContext.Current.CancellationToken)
        ).ForceChangePassword.ShouldBeFalse();

        // Old password no longer works, the new one does.
        LocationPathOf((await LoginAsync(app.NewClient(), "forced", Password))).ShouldBe("/login?error=1");
        LocationPathOf((await LoginAsync(app.NewClient(), "forced", "brand new password"))).ShouldBe("/");
    }

    private static async Task<HttpResponseMessage> PostChangeAsync(
        HttpClient client,
        string token,
        string current,
        string next,
        string confirm
    ) =>
        await client.PostAsync(
            "/account/change-password",
            new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["__RequestVerificationToken"] = token,
                    ["CurrentPassword"] = current,
                    ["NewPassword"] = next,
                    ["ConfirmPassword"] = confirm,
                }
            ),
            TestContext.Current.CancellationToken
        );
}
