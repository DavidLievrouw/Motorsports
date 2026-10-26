using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Motorsports.Auth;
using Motorsports.CharacteristicsTests.Fixtures;
using Shouldly;

namespace Motorsports.CharacteristicsTests;

[Collection(MariaDbCollection.Name)]
public sealed partial class AuthCharacteristicsTests(MariaDbFixture fixture)
{
    private const string Password = "S3cret pässword!";

    private sealed class AuthApp(
        string connectionString,
        int loginLimit,
        bool secureCookies,
        string environment = "Development"
    ) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.UseEnvironment(environment);
            builder.UseSetting("ConnectionStrings:Motorsports", connectionString);
            builder.UseSetting("Auth:SecureCookies", secureCookies.ToString());
            builder.UseSetting("Auth:LoginPermitLimit", loginLimit.ToString());
        }

        public HttpClient NewClient() =>
            CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
    }

    private static string LocationPathOf(HttpResponseMessage response) =>
        response.Headers.Location!.IsAbsoluteUri
            ? response.Headers.Location.PathAndQuery
            : response.Headers.Location.OriginalString;

    private async Task<AuthApp> StartAsync(int loginLimit = 1000, bool secureCookies = false)
    {
        await fixture.ResetSchemaAsync();
        return new AuthApp(fixture.ConnectionString, loginLimit, secureCookies);
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
    public async Task Blazor_bootstrap_script_is_available_without_login()
    {
        await using var app = await StartAsync();
        var response = await app.NewClient()
            .GetAsync("/_framework/blazor.web.js", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("text/javascript");
    }

    [Fact]
    public async Task Responses_include_security_headers()
    {
        await using var app = await StartAsync();
        var client = app.NewClient();
        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);
        response.Headers.Contains("Content-Security-Policy-Report-Only").ShouldBeFalse();
        var contentSecurityPolicy = response.Headers.GetValues("Content-Security-Policy").Single();

        contentSecurityPolicy.ShouldContain("default-src 'self'");
        contentSecurityPolicy.ShouldContain("base-uri 'self'");
        contentSecurityPolicy.ShouldContain("object-src 'none'");
        contentSecurityPolicy.ShouldContain("frame-ancestors 'none'");
        contentSecurityPolicy.ShouldContain("form-action 'self'");
        contentSecurityPolicy.ShouldContain("script-src 'self' 'nonce-");
        contentSecurityPolicy.ShouldContain("style-src 'self' 'unsafe-inline'");
        contentSecurityPolicy.ShouldContain("connect-src 'self' ws: wss:");
        contentSecurityPolicy.ShouldNotContain("unsafe-eval");
        response.Headers.GetValues("Permissions-Policy").ShouldContain("camera=(), microphone=(), geolocation=()");
        response.Headers.GetValues("Referrer-Policy").ShouldContain("strict-origin-when-cross-origin");
        response.Headers.GetValues("X-Content-Type-Options").ShouldContain("nosniff");
        response.Headers.GetValues("X-Frame-Options").ShouldContain("DENY");

        var loginPage = await client.GetAsync("/login", TestContext.Current.CancellationToken);
        var page = await loginPage.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        loginPage.Headers.Contains("Content-Security-Policy-Report-Only").ShouldBeFalse();
        var loginCsp = loginPage
            .Headers.GetValues("Content-Security-Policy")
            .Single(policy => policy.Contains("default-src 'self'", StringComparison.Ordinal));
        var nonce = Regex.Match(loginCsp, "'nonce-([^']+)'").Groups[1].Value;
        nonce.ShouldNotBeEmpty();
        page.ShouldContain($"<script type=\"importmap\" nonce=\"{nonce}\">");
        page.ShouldContain($"<script nonce=\"{nonce}\" src=");
    }

    [Fact]
    public async Task Production_host_filter_rejects_unexpected_host_headers()
    {
        await using var app = new AuthApp(fixture.ConnectionString, 1000, false, "Production");
        var client = app.NewClient();

        using var allowedRequest = new HttpRequestMessage(HttpMethod.Get, "/health");
        allowedRequest.Headers.Host = "ms.dalion.eu";
        using var rejectedRequest = new HttpRequestMessage(HttpMethod.Get, "/health");
        rejectedRequest.Headers.Host = "evil.example";
        var allowed = await client.SendAsync(allowedRequest, TestContext.Current.CancellationToken);
        var rejected = await client.SendAsync(rejectedRequest, TestContext.Current.CancellationToken);

        allowed.StatusCode.ShouldBe(HttpStatusCode.OK);
        rejected.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Data_protection_cookies_survive_another_application_instance()
    {
        await fixture.ResetSchemaAsync();
        await SeedUserAsync("shared-keys");
        await using var first = new AuthApp(fixture.ConnectionString, 1000, false);
        await using var second = new AuthApp(fixture.ConnectionString, 1000, false);
        var firstClient = first.NewClient();
        var login = await LoginAsync(firstClient, "shared-keys", Password);
        var authCookie = login
            .Headers.GetValues("Set-Cookie")
            .Single(cookie => cookie.StartsWith("ms.auth=", StringComparison.Ordinal))
            .Split(';', 2)[0];

        var secondClient = second.NewClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("Cookie", authCookie);
        var response = await secondClient.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        await using var context = await fixture
            .CreateContextFactory()
            .CreateDbContextAsync(TestContext.Current.CancellationToken);
        (await context.DataProtectionKeys.CountAsync(TestContext.Current.CancellationToken)).ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Untrusted_forwarded_for_headers_do_not_bypass_login_rate_limiting()
    {
        await using var app = await StartAsync(loginLimit: 2);
        app.Services.GetRequiredService<IOptions<AuthOptions>>().Value.LoginPermitLimit.ShouldBe(2);
        var client = app.NewClient();
        var token = await TokenAsync(client, "/login");

        for (var index = 0; index < 2; index++)
        {
            using var request = LoginRequest(token, $"203.0.113.{index + 1}");
            (await client.SendAsync(request, TestContext.Current.CancellationToken)).StatusCode.ShouldBe(
                HttpStatusCode.Redirect
            );
        }

        using var limited = LoginRequest(token, "203.0.113.200");
        (await client.SendAsync(limited, TestContext.Current.CancellationToken)).StatusCode.ShouldBe(
            HttpStatusCode.TooManyRequests
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
        await using var app = await StartAsync(secureCookies: true);
        await SeedUserAsync("legacy", prf);
        var client = app.NewClient();
        client.BaseAddress = new Uri("https://localhost");
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
        cookie.ShouldContain("secure", Case.Insensitive);
        (await V3HashAsync("legacy")).ShouldNotBeNullOrEmpty();
        (await client.GetAsync("/", TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.OK);

        // The v3 hash now verifies on its own; a second client logs in again.
        var secondClient = app.NewClient();
        secondClient.BaseAddress = new Uri("https://localhost");
        var again = await LoginAsync(secondClient, "legacy", Password);
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

    private static HttpRequestMessage LoginRequest(string token, string forwardedFor)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/login")
        {
            Content = new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["Username"] = "nobody",
                    ["Password"] = "x",
                    ["ReturnUrl"] = "",
                    ["__RequestVerificationToken"] = token,
                }
            ),
        };
        request.Headers.Add("X-Forwarded-For", forwardedFor);
        return request;
    }
}
