using FitnessApp.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

// One API instance and one PostgreSQL container are shared by all tests in this assembly.
// Tests stay independent by creating their own data (unique e-mails, own users).
[assembly: AssemblyFixture(typeof(ApiFactory))]

namespace FitnessApp.IntegrationTests.Infrastructure;

/// <summary>
/// Runs the real API in memory against a real PostgreSQL 16 started in Docker (Testcontainers),
/// so tests exercise the same SQL, constraints and indexes as production.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:16").Build();

    public async ValueTask InitializeAsync()
    {
        await _database.StartAsync();

        // Start the API once now, so it migrates the database before any test runs. Otherwise a test that
        // builds a second host (WithWebHostBuilder) can race the first one and both try to create the tables.
        _ = Server;
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
    }

    /// <summary>Strong enough to pass every password rule.</summary>
    public const string ValidPassword = "Trening123!";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", _database.GetConnectionString());
        builder.UseSetting("Jwt:Key", "integration-tests-signing-key-not-a-secret-0123456789");
        // All test requests share one client IP, so the per-IP auth limit is lifted here;
        // a dedicated test turns it back down to prove the limiter works.
        builder.UseSetting("RateLimit:AuthPermitLimit", "100000");
    }
}
