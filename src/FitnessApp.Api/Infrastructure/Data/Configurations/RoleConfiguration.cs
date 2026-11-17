using FitnessApp.Api.Features.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitnessApp.Api.Infrastructure.Data.Configurations;

/// <summary>
/// The roles are part of the schema, seeded by a migration with fixed ids. Creating them at startup instead
/// races when two app instances start at once (both see "missing" and both insert).
/// </summary>
public sealed class RoleConfiguration : IEntityTypeConfiguration<IdentityRole>
{
    public void Configure(EntityTypeBuilder<IdentityRole> builder) =>
        builder.HasData(
            new IdentityRole
            {
                Id = "2c8e6f0a-5b1d-4c3e-9a7f-1d2b3c4e5f60",
                Name = Roles.User,
                NormalizedName = Roles.User.ToUpperInvariant(),
                ConcurrencyStamp = "b1f0c2d3-0001-4000-8000-000000000001",
            },
            new IdentityRole
            {
                Id = "7d4a9b1c-2e3f-4a5b-8c6d-7e8f9a0b1c2d",
                Name = Roles.Admin,
                NormalizedName = Roles.Admin.ToUpperInvariant(),
                ConcurrencyStamp = "b1f0c2d3-0002-4000-8000-000000000002",
            });
}
