using FitnessApp.Api.Features.Auth;
using FitnessApp.Api.Features.Sessions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitnessApp.Api.Infrastructure.Data.Configurations;

public sealed class WorkoutSessionConfiguration : IEntityTypeConfiguration<WorkoutSession>
{
    public void Configure(EntityTypeBuilder<WorkoutSession> builder)
    {
        builder.ToTable("WorkoutSessions", t => t.HasCheckConstraint(
            "CK_WorkoutSessions_PausedSeconds", "\"PausedSeconds\" >= 0"));

        builder.Property(s => s.Name).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Notes).HasMaxLength(1000).IsRequired();
        builder.Property(s => s.Type).HasConversion<string>().HasMaxLength(16);
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(16);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(s => s.UserId)
            // A user's workouts are theirs alone and go with the account.
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Plan)
            .WithMany()
            .HasForeignKey(s => s.PlanId)
            // The workout keeps its own copy of the exercises, so it survives its plan being deleted.
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(s => s.Exercises)
            .WithOne(e => e.Session)
            .HasForeignKey(e => e.WorkoutSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // History: "this user's workouts, newest first"; the live screen: "this user's active workout".
        builder.HasIndex(s => new { s.UserId, s.StartedAtUtc });
        builder.HasIndex(s => new { s.UserId, s.Status });
    }
}
