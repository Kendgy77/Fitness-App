using FitnessApp.Api.Common.Errors;
using FitnessApp.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Api.Features.Profile;

public sealed class ProfileService(AppDbContext db, TimeProvider clock) : IProfileService
{
    private const int MinAgeYears = 13;
    private const int MaxAgeYears = 120;

    public async Task<ProfileResponse> GetAsync(string userId, CancellationToken ct)
    {
        // The user id comes from the token, never from the URL, so a user can only ever read their own profile.
        var profile = await db.UserProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.UserId == userId, ct);

        return profile?.ToResponse(Today()) ?? ProfileMappings.Empty;
    }

    public async Task<ProfileResponse> UpdateAsync(string userId, UpdateProfileRequest request, CancellationToken ct)
    {
        var today = Today();
        ValidateBirthDate(request.BirthDate, today);
        var timeZone = NormalizeTimeZone(request.TimeZone);

        var profile = await db.UserProfiles.SingleOrDefaultAsync(p => p.UserId == userId, ct);
        if (profile is null)
        {
            profile = new UserProfile { UserId = userId };
            db.UserProfiles.Add(profile);
        }

        profile.BirthDate = request.BirthDate;
        profile.Sex = request.Sex;
        // The columns keep one decimal place; rounding here means the response shows exactly what was stored.
        profile.HeightCm = RoundToTenth(request.HeightCm);
        profile.WeightKg = RoundToTenth(request.WeightKg);
        profile.Goal = request.Goal;
        profile.TimeZone = timeZone;
        profile.UpdatedAtUtc = clock.GetUtcNow();

        await db.SaveChangesAsync(ct);
        return profile.ToResponse(today);
    }

    private DateOnly Today() => DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

    private static void ValidateBirthDate(DateOnly? birthDate, DateOnly today)
    {
        if (birthDate is { } date && AgeCalculator.AgeOn(date, today) is < MinAgeYears or > MaxAgeYears)
        {
            throw new DomainValidationException(
                $"Enter a valid birth date (age {MinAgeYears} to {MaxAgeYears} years).", "birthDate");
        }
    }

    /// <summary>Accepts IANA ids ("Europe/Warsaw"), which work the same on Linux servers and in browsers.</summary>
    private static string NormalizeTimeZone(string? timeZone)
    {
        if (string.IsNullOrWhiteSpace(timeZone))
        {
            return UserProfile.DefaultTimeZone;
        }

        var id = timeZone.Trim();
        if (!TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone) || !zone.HasIanaId)
        {
            throw new DomainValidationException("Unknown time zone.", "timeZone");
        }

        return id;
    }

    private static decimal? RoundToTenth(decimal? value) =>
        value is { } v ? Math.Round(v, 1, MidpointRounding.AwayFromZero) : null;
}
