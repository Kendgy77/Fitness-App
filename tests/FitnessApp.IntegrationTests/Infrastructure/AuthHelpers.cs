using System.Net.Http.Headers;
using System.Net.Http.Json;
using FitnessApp.Api.Features.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FitnessApp.IntegrationTests.Infrastructure;

public static class AuthHelpers
{
    /// <summary>A unique e-mail per call, so tests never collide on the shared database.</summary>
    public static string UniqueEmail(string prefix = "user") => $"{prefix}-{Guid.NewGuid():N}@test.local";

    public static async Task<AuthResponse> RegisterAsync(
        this HttpClient client, string email, string password = ApiFactory.ValidPassword, CancellationToken ct = default)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password), ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>(ct))!;
    }

    /// <summary>Registers a fresh user and returns a client that sends their token on every request.</summary>
    public static async Task<HttpClient> CreateAuthenticatedClientAsync<TEntryPoint>(
        this WebApplicationFactory<TEntryPoint> factory, string? email = null, CancellationToken ct = default)
        where TEntryPoint : class
    {
        var client = factory.CreateClient();
        var auth = await client.RegisterAsync(email ?? UniqueEmail(), ct: ct);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }

    /// <summary>A client logged in as a fresh user with the Admin role.</summary>
    public static async Task<HttpClient> CreateAdminClientAsync(this ApiFactory factory, CancellationToken ct = default)
    {
        var email = UniqueEmail("admin");
        var client = factory.CreateClient();
        await client.RegisterAsync(email, ct: ct);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var user = await users.FindByEmailAsync(email);
            (await users.AddToRoleAsync(user!, Roles.Admin)).Succeeded.ShouldBeTrue();
        }

        // Roles travel inside the token, so log in again to get a token that carries the new role.
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, ApiFactory.ValidPassword), ct);
        login.EnsureSuccessStatusCode();
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>(ct);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }
}
