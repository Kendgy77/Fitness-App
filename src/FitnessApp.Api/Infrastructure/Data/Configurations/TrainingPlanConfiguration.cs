using FitnessApp.Api.Features.Auth;
using FitnessApp.Api.Features.Plans;
using FitnessApp.Api.Infrastructure.Data.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitnessApp.Api.Infrastructure.Data.Configurations;

public sealed class TrainingPlanConfiguration : IEntityTypeConfiguration<TrainingPlan>
{
    public void Configure(EntityTypeBuilder<TrainingPlan> builder)
    {
        builder.ToTable("TrainingPlans");

        // Templates use fixed ids below 1000 (see PlanTemplates); plans created at runtime start at 1000.
        builder.Property(p => p.Id)
            .UseIdentityByDefaultColumn()
            .HasIdentityOptions(startValue: PlanTemplates.FirstRuntimeId);

        builder.Property(p => p.Name).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(1000).IsRequired();
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(16);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(p => p.UserId)
            // A user's plans are theirs alone and go with the account.
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Exercises)
            .WithOne()
            .HasForeignKey(e => e.TrainingPlanId)
            // A plan's exercise rows are part of the plan (composition).
            .OnDelete(DeleteBehavior.Cascade);

        // The plan list asks for "this user's plans, newest change first".
        builder.HasIndex(p => new { p.UserId, p.UpdatedAtUtc });

        builder.HasData(PlanTemplates.Plans);
    }
}
