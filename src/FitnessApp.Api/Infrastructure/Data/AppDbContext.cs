using FitnessApp.Api.Features.Auth;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Api.Infrastructure.Data;

/// <summary>
/// The app's session with PostgreSQL. It includes the Identity tables (users, roles, …). Each entity's
/// table setup lives in its own IEntityTypeConfiguration class under Infrastructure/Data/Configurations.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
