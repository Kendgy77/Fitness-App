using FitnessApp.Api.Features.Auth;
using FitnessApp.Api.Features.Measurements;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitnessApp.Api.Infrastructure.Data.Configurations;

public sealed class BodyMeasurementConfiguration : IEntityTypeConfiguration<BodyMeasurement>
{
    public void Configure(EntityTypeBuilder<BodyMeasurement> builder)
    {
        // An entry without any value means nothing; the API checks this too.
        builder.ToTable("BodyMeasurements", t => t.HasCheckConstraint(
            "CK_BodyMeasurements_AnyValue",
            "COALESCE(\"WeightKg\", \"BodyFatPercent\", \"MuscleMassKg\", \"NeckCm\", \"ChestCm\", \"WaistCm\", \"HipsCm\", "
            + "\"BicepsLeftCm\", \"BicepsRightCm\", \"ForearmCm\", \"ThighLeftCm\", \"ThighRightCm\", \"CalfCm\") IS NOT NULL"));

        // numeric(5,1): up to 9999.9, one decimal place, for every value.
        foreach (var property in builder.Metadata.GetProperties().Where(p => p.ClrType == typeof(decimal?)))
        {
            property.SetPrecision(5);
            property.SetScale(1);
        }

        builder.Property(m => m.Notes).HasMaxLength(200).IsRequired();

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // One entry per user and day; also the index for "this user's measurements by date".
        builder.HasIndex(m => new { m.UserId, m.MeasuredOn }).IsUnique();
    }
}
