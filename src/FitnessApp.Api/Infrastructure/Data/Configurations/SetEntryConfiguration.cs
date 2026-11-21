using FitnessApp.Api.Features.Sessions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitnessApp.Api.Infrastructure.Data.Configurations;

public sealed class SetEntryConfiguration : IEntityTypeConfiguration<SetEntry>
{
    public void Configure(EntityTypeBuilder<SetEntry> builder)
    {
        // The API validates these ranges; the checks guard every other way into the database.
        builder.ToTable("SetEntries", t =>
        {
            t.HasCheckConstraint("CK_SetEntries_Reps", "\"Reps\" BETWEEN 0 AND 200");
            t.HasCheckConstraint("CK_SetEntries_WeightKg", "\"WeightKg\" IS NULL OR \"WeightKg\" BETWEEN 0 AND 1000");
            t.HasCheckConstraint("CK_SetEntries_Rpe", "\"Rpe\" IS NULL OR \"Rpe\" BETWEEN 1 AND 10");
        });

        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.WeightKg).HasPrecision(6, 2);

        // Not unique for the same reason as SessionExercise.Order: deleting a set renumbers the ones after it.
        builder.HasIndex(x => new { x.SessionExerciseId, x.SetNumber });
    }
}
