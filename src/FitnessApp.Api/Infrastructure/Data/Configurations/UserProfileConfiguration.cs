using FitnessApp.Api.Features.Auth;
using FitnessApp.Api.Features.Profile;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitnessApp.Api.Infrastructure.Data.Configurations;

public sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("UserProfiles");

        // The user's id is the key, so a second profile for the same user is impossible (1:1).
        builder.HasKey(p => p.UserId);
        builder.HasOne<AppUser>()
            .WithOne()
            .HasForeignKey<UserProfile>(p => p.UserId)
            // The profile can't exist without its user (thesis: composition), so it goes when the account goes.
            .OnDelete(DeleteBehavior.Cascade);

        // numeric(4,1): up to 999.9, one decimal place (183.5 cm, 78.4 kg).
        builder.Property(p => p.HeightCm).HasPrecision(4, 1);
        builder.Property(p => p.WeightKg).HasPrecision(4, 1);

        // Enums are stored as text ("Male", "LoseWeight"): readable in the database, and reordering the enum
        // in code can't silently change the meaning of existing rows.
        builder.Property(p => p.Sex).HasConversion<string>().HasMaxLength(16);
        builder.Property(p => p.Goal).HasConversion<string>().HasMaxLength(32);

        builder.Property(p => p.TimeZone).HasMaxLength(64).IsRequired();
        builder.Property(p => p.UpdatedAtUtc).IsRequired();
    }
}
