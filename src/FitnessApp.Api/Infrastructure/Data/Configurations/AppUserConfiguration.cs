using FitnessApp.Api.Features.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitnessApp.Api.Infrastructure.Data.Configurations;

public sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        // Identity already configures its own columns and indexes; only our additions go here.
        builder.Property(u => u.CreatedAtUtc).IsRequired();

        // Identity checks e-mail uniqueness in code only; two simultaneous registrations could both pass that
        // check. A unique index makes the database the final guard.
        builder.HasIndex(u => u.NormalizedEmail).HasDatabaseName("EmailIndex").IsUnique();
    }
}
