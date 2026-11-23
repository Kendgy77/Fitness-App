using FitnessApp.Api.Features.Auth;
using FitnessApp.Api.Features.Exercises;
using FitnessApp.Api.Features.Measurements;
using FitnessApp.Api.Features.Plans;
using FitnessApp.Api.Features.Profile;
using FitnessApp.Api.Features.Sessions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Api.Infrastructure.Data;

/// <summary>
/// The app's session with PostgreSQL. It includes the Identity tables (users, roles, …). Each entity's
/// table setup lives in its own IEntityTypeConfiguration class under Infrastructure/Data/Configurations.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    public DbSet<Exercise> Exercises => Set<Exercise>();

    public DbSet<TrainingPlan> TrainingPlans => Set<TrainingPlan>();

    public DbSet<PlanExercise> PlanExercises => Set<PlanExercise>();

    public DbSet<WorkoutSession> WorkoutSessions => Set<WorkoutSession>();

    public DbSet<SessionExercise> SessionExercises => Set<SessionExercise>();

    public DbSet<SetEntry> SetEntries => Set<SetEntry>();

    public DbSet<BodyMeasurement> BodyMeasurements => Set<BodyMeasurement>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // unaccent(): accent-insensitive search in the exercise library ("wioslowanie" finds "Wiosłowanie").
        builder.HasPostgresExtension("unaccent");
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
