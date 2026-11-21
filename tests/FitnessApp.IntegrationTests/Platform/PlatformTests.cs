using System.Net;
using FitnessApp.IntegrationTests.Infrastructure;

namespace FitnessApp.IntegrationTests.Platform;

/// <summary>Cross-cutting behaviour every endpoint relies on: health, error format, security headers.</summary>
public sealed class PlatformTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Health_reports_healthy_when_the_database_is_reachable()
    {
        var response = await _client.GetAsync("/health", _ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(_ct)).ShouldBe("Healthy");
    }

    [Fact]
    public async Task Unknown_route_returns_404_as_problem_details_for_a_logged_in_user()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);

        var response = await client.GetAsync("/api/does-not-exist", _ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task Unknown_route_returns_401_for_an_anonymous_caller()
    {
        // Secure by default: anonymous callers can't even discover which routes exist.
        var response = await _client.GetAsync("/api/does-not-exist", _ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task The_web_app_can_read_the_server_time_from_another_origin()
    {
        // In production the web app and the API have different addresses. The live workout timer counts on the
        // server's clock (the Date header), which browsers only show to the page if CORS exposes it.
        using var app = factory.WithWebHostBuilder(b => b.UseSetting("Cors:AllowedOrigins:0", "https://app.example"));
        var client = app.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Origin", "https://app.example");

        var response = await client.SendAsync(request, _ct);

        // The Date header itself comes from the real web server (Kestrel), not from this in-memory test server.
        response.Headers.GetValues("Access-Control-Allow-Origin").ShouldBe(["https://app.example"]);
        response.Headers.GetValues("Access-Control-Expose-Headers").ShouldContain("Date");
    }

    [Fact]
    public async Task Responses_carry_security_headers()
    {
        var response = await _client.GetAsync("/health", _ct);

        response.Headers.GetValues("X-Content-Type-Options").ShouldBe(["nosniff"]);
        response.Headers.GetValues("X-Frame-Options").ShouldBe(["DENY"]);
        response.Headers.GetValues("Referrer-Policy").ShouldBe(["no-referrer"]);
    }
}
