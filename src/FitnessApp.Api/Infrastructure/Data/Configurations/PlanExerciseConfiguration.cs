using FitnessApp.Api.Features.Plans;
using FitnessApp.Api.Infrastructure.Data.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitnessApp.Api.Infrastructure.Data.Configurations;

public sealed class PlanExerciseConfiguration : IEntityTypeConfiguration<PlanExercise>
{
    public void Configure(EntityTypeBuilder<PlanExercise> builder)
    {
        builder.ToTable("PlanExercises");

        builder.Property(e => e.Id)
            .UseIdentityByDefaultColumn()
            .HasIdentityOptions(startValue: PlanTemplates.FirstRuntimeId);

        builder.Property(e => e.TargetWeightKg).HasPrecision(6, 2);
        builder.Property(e => e.Notes).HasMaxLength(200).IsRequired();

        // Two rows can't share a position in the same plan.
        builder.HasIndex(e => new { e.TrainingPlanId, e.Order }).IsUnique();

        builder.HasOne(e => e.Exercise)
            .WithMany()
            .HasForeignKey(e => e.ExerciseId)
            // An exercise used in a plan can't be deleted from under it (exercises are archived, never deleted).
            // NO ACTION, not RESTRICT: the check runs at the end of the statement, so deleting a user, which
            // cascades to both their plans and their custom exercises, works whatever order the rows go in.
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasData(PlanTemplates.Exercises);
    }
}
