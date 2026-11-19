using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FitnessApp.Api.Features.Auth;
using FitnessApp.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace FitnessApp.IntegrationTests.Auth;

/// <summary>WF01 (registration) and WF02 (login, logout, lockout). Test IDs refer to the thesis RTM.</summary>
public sealed class AuthTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task T01_register_with_valid_data_creates_account_and_logs_the_user_in()
    {
        var email = AuthHelpers.UniqueEmail("nowy.uzytkownik");

        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "Trening123!"), _ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(_ct);
        auth!.AccessToken.ShouldNotBeNullOrWhiteSpace();
        auth.User.Email.ShouldBe(email);
        auth.User.Roles.ShouldBe([Roles.User]);

        // The returned token works straight away: the user is logged in.
        var me = await GetMeAsync(auth.AccessToken);
        me.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task T02_register_with_an_existing_email_returns_400_and_creates_nothing()
    {
        var email = AuthHelpers.UniqueEmail();
        await _client.RegisterAsync(email, ct: _ct);

        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email.ToUpperInvariant(), "Inne1234Haslo"), _ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(_ct);
        problem!.Errors["email"].ShouldBe(["An account with this email already exists."]);
    }

    [Theory]
    [InlineData("Krot1")]           // too short
    [InlineData("bezcyfryABC")]     // no digit
    [InlineData("tylkomale123")]    // no upper-case letter
    [InlineData("TYLKODUZE123")]    // no lower-case letter
    public async Task Register_with_a_weak_password_returns_400_with_password_errors(string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(AuthHelpers.UniqueEmail(), password), _ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(_ct);
        problem!.Errors.Keys.ShouldContain(k => k.Equals("password", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Register_with_an_invalid_email_returns_400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("to-nie-jest-email", ApiFactory.ValidPassword), _ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task T03_login_with_correct_credentials_returns_a_token()
    {
        var email = AuthHelpers.UniqueEmail();
        await _client.RegisterAsync(email, ct: _ct);

        var response = await LoginAsync(email, ApiFactory.ValidPassword);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(_ct);
        (await GetMeAsync(auth!.AccessToken)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task T04_three_wrong_passwords_lock_the_account_for_15_minutes()
    {
        var email = AuthHelpers.UniqueEmail("jan.kowalski");
        await _client.RegisterAsync(email, ct: _ct);

        (await LoginAsync(email, "Bledne123")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await LoginAsync(email, "Bledne123")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        var third = await LoginAsync(email, "Bledne123");

        third.StatusCode.ShouldBe(HttpStatusCode.Locked);
        var problem = await third.Content.ReadFromJsonAsync<JsonElement>(_ct);
        problem.GetProperty("title").GetString().ShouldBe("Account locked");
        problem.GetProperty("retryAfter").GetInt32().ShouldBeInRange(14 * 60, 15 * 60);
        third.Headers.RetryAfter.ShouldNotBeNull();

        // While locked, even the correct password is refused.
        (await LoginAsync(email, ApiFactory.ValidPassword)).StatusCode.ShouldBe(HttpStatusCode.Locked);
    }

    [Fact]
    public async Task Login_works_again_once_the_lockout_has_ended()
    {
        var email = AuthHelpers.UniqueEmail();
        await _client.RegisterAsync(email, ct: _ct);
        for (var i = 0; i < 3; i++)
        {
            await LoginAsync(email, "Bledne123");
        }

        // Fast-forward: move the lockout end into the past instead of waiting 15 minutes.
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var user = await users.FindByEmailAsync(email);
            await users.SetLockoutEndDateAsync(user!, DateTimeOffset.UtcNow.AddSeconds(-1));
        }

        (await LoginAsync(email, ApiFactory.ValidPassword)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Successful_login_resets_the_failed_attempt_counter()
    {
        var email = AuthHelpers.UniqueEmail();
        await _client.RegisterAsync(email, ct: _ct);

        await LoginAsync(email, "Bledne123");
        await LoginAsync(email, "Bledne123");
        (await LoginAsync(email, ApiFactory.ValidPassword)).StatusCode.ShouldBe(HttpStatusCode.OK);

        // The counter started over, so one more wrong password does not lock the account.
        (await LoginAsync(email, "Bledne123")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Unknown_email_gets_the_same_answer_as_a_wrong_password()
    {
        var email = AuthHelpers.UniqueEmail();
        await _client.RegisterAsync(email, ct: _ct);

        var wrongPassword = await LoginAsync(email, "Bledne123");
        var unknownEmail = await LoginAsync(AuthHelpers.UniqueEmail("nikt"), "Bledne123");

        unknownEmail.StatusCode.ShouldBe(wrongPassword.StatusCode);
        var a = await wrongPassword.Content.ReadFromJsonAsync<ProblemDetails>(_ct);
        var b = await unknownEmail.Content.ReadFromJsonAsync<ProblemDetails>(_ct);
        b!.Title.ShouldBe(a!.Title);
    }

    [Fact]
    public async Task Me_without_a_token_returns_401()
    {
        var response = await _client.GetAsync("/api/auth/me", _ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_with_a_tampered_token_returns_401()
    {
        var auth = await _client.RegisterAsync(AuthHelpers.UniqueEmail(), ct: _ct);
        var tampered = auth.AccessToken[..^4] + (auth.AccessToken.EndsWith("AAAA", StringComparison.Ordinal) ? "BBBB" : "AAAA");

        (await GetMeAsync(tampered)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_returns_the_logged_in_user()
    {
        var email = AuthHelpers.UniqueEmail();
        var client = await factory.CreateAuthenticatedClientAsync(email, _ct);

        var me = await client.GetFromJsonAsync<UserResponse>("/api/auth/me", _ct);

        me!.Email.ShouldBe(email);
        me.Roles.ShouldBe([Roles.User]);
    }

    [Fact]
    public async Task Logout_requires_a_token_and_returns_204()
    {
        (await _client.PostAsync("/api/auth/logout", null, _ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        (await client.PostAsync("/api/auth/logout", null, _ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Auth_endpoints_are_rate_limited_per_client()
    {
        using var limited = factory.WithWebHostBuilder(b => b.UseSetting("RateLimit:AuthPermitLimit", "2"));
        var client = limited.CreateClient();
        var request = new LoginRequest(AuthHelpers.UniqueEmail(), "Bledne123");

        await client.PostAsJsonAsync("/api/auth/login", request, _ct);
        await client.PostAsJsonAsync("/api/auth/login", request, _ct);
        var third = await client.PostAsJsonAsync("/api/auth/login", request, _ct);

        third.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        third.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    private Task<HttpResponseMessage> LoginAsync(string email, string password) =>
        _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password), _ct);

    private async Task<HttpResponseMessage> GetMeAsync(string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(request, _ct);
    }
}
