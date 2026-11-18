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
    public async Task Unknown_route_returns_404_as_problem_details()
    {
        var response = await _client.GetAsync("/api/does-not-exist", _ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
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
