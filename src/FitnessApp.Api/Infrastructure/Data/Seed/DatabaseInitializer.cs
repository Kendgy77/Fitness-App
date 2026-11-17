using FitnessApp.Api.Features.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Api.Infrastructure.Data.Seed;

/// <summary>Settings for the Development-only admin account. Values come from user-secrets, never from git.</summary>
public sealed class SeedAdminSettings
{
    public const string SectionName = "SeedAdmin";

    public string? Email { get; init; }
    public string? Password { get; init; }
}

public static class DatabaseInitializer
{
    /// <summary>
    /// Runs at startup. Migrations are applied automatically only in Development and Testing; in production
    /// they are a separate deploy step, so a bad migration can't take the running app down.
    /// Seeding is idempotent: running it twice changes nothing.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var environment = app.Environment;

        if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
        {
            await services.GetRequiredService<AppDbContext>().Database.MigrateAsync();
        }

        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in Roles.All)
        {
            if (!await roles.RoleExistsAsync(role))
            {
                await roles.CreateAsync(new IdentityRole(role));
            }
        }

        if (environment.IsDevelopment())
        {
            await SeedDevelopmentAdminAsync(services, app.Configuration);
        }
    }

    private static async Task SeedDevelopmentAdminAsync(IServiceProvider services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(SeedAdminSettings.SectionName).Get<SeedAdminSettings>();
        if (string.IsNullOrWhiteSpace(settings?.Email) || string.IsNullOrWhiteSpace(settings.Password))
        {
            return;
        }

        var users = services.GetRequiredService<UserManager<AppUser>>();
        if (await users.FindByEmailAsync(settings.Email) is not null)
        {
            return;
        }

        var admin = new AppUser
        {
            UserName = settings.Email,
            Email = settings.Email,
            EmailConfirmed = true,
            CreatedAtUtc = services.GetRequiredService<TimeProvider>().GetUtcNow(),
        };
        var created = await users.CreateAsync(admin, settings.Password);
        if (!created.Succeeded)
        {
            throw new InvalidOperationException(
                "Could not create the development admin: " + string.Join(" ", created.Errors.Select(e => e.Description)));
        }

        await users.AddToRolesAsync(admin, [Roles.User, Roles.Admin]);
    }
}
