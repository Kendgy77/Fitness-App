using FitnessApp.Api.Features.Auth;
using FitnessApp.Api.Features.Exercises;
using FitnessApp.Api.Infrastructure.Data.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitnessApp.Api.Infrastructure.Data.Configurations;

public sealed class ExerciseConfiguration : IEntityTypeConfiguration<Exercise>
{
    public void Configure(EntityTypeBuilder<Exercise> builder)
    {
        builder.ToTable("Exercises");

        // The seeded catalog uses fixed ids 1–999. Inserting fixed ids doesn't move PostgreSQL's identity
        // counter, so without this the first exercise created at runtime would get id 1 and fail.
        builder.Property(e => e.Id)
            .UseIdentityByDefaultColumn()
            .HasIdentityOptions(startValue: ExerciseCatalog.FirstRuntimeId);

        // Natural alphabetical order (ICU root collation), the same on every server: the default collation
        // depends on how the database was created (C.UTF-8 would sort "Zercher" before "arm curl").
        builder.Property(e => e.Name).HasMaxLength(100).IsRequired().UseCollation("und-x-icu");
        builder.Property(e => e.Description).HasMaxLength(2000).IsRequired();
        builder.Property(e => e.Muscles).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Category).HasConversion<string>().HasMaxLength(16);
        builder.Property(e => e.Difficulty).HasConversion<string>().HasMaxLength(16);
        builder.Property(e => e.Equipment).HasConversion<string>().HasMaxLength(16);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            // Custom exercises belong to their user and go with the account. Global ones have no owner.
            .OnDelete(DeleteBehavior.Cascade);

        // Last line of defence for unique names, per owner, among active exercises. NULLS NOT DISTINCT makes
        // two global exercises (UserId NULL) count as the same owner. The service also checks this,
        // ignoring case, and returns a friendly 409.
        builder.HasIndex(e => new { e.UserId, e.Name })
            .IsUnique()
            .AreNullsDistinct(false)
            .HasFilter("\"IsArchived\" = false");

        builder.HasData(ExerciseCatalog.All);
    }
}
