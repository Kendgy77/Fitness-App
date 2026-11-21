using FitnessApp.Api.Features.Sessions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitnessApp.Api.Infrastructure.Data.Configurations;

public sealed class SessionExerciseConfiguration : IEntityTypeConfiguration<SessionExercise>
{
    public void Configure(EntityTypeBuilder<SessionExercise> builder)
    {
        builder.ToTable("SessionExercises");

        builder.Property(e => e.Notes).HasMaxLength(200).IsRequired();
        builder.Property(e => e.TargetWeightKg).HasPrecision(6, 2);

        // Not unique (unlike plans): rows are renumbered in place when one is removed during a workout, and a
        // unique index could reject a half-done renumbering. The service keeps the numbers 1, 2, 3, ….
        builder.HasIndex(e => new { e.WorkoutSessionId, e.Order });

        builder.HasOne(e => e.Exercise)
            .WithMany()
            .HasForeignKey(e => e.ExerciseId)
            // Same as plans: exercises are archived, never deleted from under a workout.
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(e => e.Sets)
            .WithOne()
            .HasForeignKey(x => x.SessionExerciseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
