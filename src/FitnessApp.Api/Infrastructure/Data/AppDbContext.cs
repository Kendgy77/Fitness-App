using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Api.Infrastructure.Data;

/// <summary>
/// The app's session with PostgreSQL. Each entity's table setup lives in its own
/// IEntityTypeConfiguration class under Infrastructure/Data/Configurations.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
