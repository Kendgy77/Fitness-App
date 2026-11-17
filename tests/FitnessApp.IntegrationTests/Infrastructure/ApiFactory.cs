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

    public async ValueTask InitializeAsync() => await _database.StartAsync();

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", _database.GetConnectionString());
    }
}
