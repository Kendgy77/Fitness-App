using System.Net.Http.Headers;
using System.Net.Http.Json;
using FitnessApp.Api.Features.Auth;
using Microsoft.AspNetCore.Mvc.Testing;

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
}
