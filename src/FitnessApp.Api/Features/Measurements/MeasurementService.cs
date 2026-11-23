using FitnessApp.Api.Common;
using FitnessApp.Api.Common.Errors;
using FitnessApp.Api.Features.Profile;
using FitnessApp.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Api.Features.Measurements;

public sealed class MeasurementService(AppDbContext db, TimeProvider clock) : IMeasurementService
{
    private const string OneEntryPerDay = "You already have measurements for that day. Edit that entry instead.";

    public async Task<PagedResponse<MeasurementResponse>> ListAsync(string userId, MeasurementQuery query, CancellationToken ct)
    {
        var measurements = db.BodyMeasurements.AsNoTracking().Where(m => m.UserId == userId);
        if (query.From is { } from)
        {
            measurements = measurements.Where(m => m.MeasuredOn >= from);
        }

        if (query.To is { } to)
        {
            measurements = measurements.Where(m => m.MeasuredOn <= to);
        }

        var total = await measurements.CountAsync(ct);
        var rows = await measurements
            .OrderByDescending(m => m.MeasuredOn)
            .Skip(query.Skip).Take(query.PageSize)
            .ToListAsync(ct);

        var heightCm = await HeightAsync(userId, ct);
        return new PagedResponse<MeasurementResponse>([.. rows.Select(m => ToResponse(m, heightCm))], query.Page, query.PageSize, total);
    }

    public async Task<MeasurementResponse?> GetAsync(string userId, int id, CancellationToken ct)
    {
        var measurement = await db.BodyMeasurements.AsNoTracking().SingleOrDefaultAsync(m => m.Id == id && m.UserId == userId, ct);
        return measurement is null ? null : ToResponse(measurement, await HeightAsync(userId, ct));
    }

    public async Task<MeasurementResponse> CreateAsync(string userId, MeasurementRequest request, CancellationToken ct)
    {
        var profile = await db.UserProfiles.SingleOrDefaultAsync(p => p.UserId == userId, ct);
        var day = request.MeasuredOn!.Value;
        EnsureNotInFuture(day, profile);

        if (await db.BodyMeasurements.AnyAsync(m => m.UserId == userId && m.MeasuredOn == day, ct))
        {
            throw new ConflictException(OneEntryPerDay);
        }

        var now = clock.GetUtcNow();
        var measurement = new BodyMeasurement { UserId = userId, CreatedAtUtc = now };
        Apply(measurement, request, now);
        db.BodyMeasurements.Add(measurement);
        profile = await UpdateProfileWeightAsync(userId, profile, measurement, now, ct);

        await db.SaveChangesAsync(ct);
        return ToResponse(measurement, profile?.HeightCm);
    }

    public async Task<MeasurementResponse?> UpdateAsync(string userId, int id, MeasurementRequest request, CancellationToken ct)
    {
        var measurement = await db.BodyMeasurements.SingleOrDefaultAsync(m => m.Id == id && m.UserId == userId, ct);
        if (measurement is null)
        {
            return null;
        }

        var profile = await db.UserProfiles.SingleOrDefaultAsync(p => p.UserId == userId, ct);
        var day = request.MeasuredOn!.Value;
        EnsureNotInFuture(day, profile);

        if (day != measurement.MeasuredOn
            && await db.BodyMeasurements.AnyAsync(m => m.UserId == userId && m.MeasuredOn == day, ct))
        {
            throw new ConflictException(OneEntryPerDay);
        }

        var now = clock.GetUtcNow();
        Apply(measurement, request, now);
        profile = await UpdateProfileWeightAsync(userId, profile, measurement, now, ct);

        await db.SaveChangesAsync(ct);
        return ToResponse(measurement, profile?.HeightCm);
    }

    public async Task<bool> DeleteAsync(string userId, int id, CancellationToken ct)
    {
        // The profile keeps its weight: it is the user's current weight, not a copy of this entry.
        var deleted = await db.BodyMeasurements.Where(m => m.Id == id && m.UserId == userId).ExecuteDeleteAsync(ct);
        return deleted > 0;
    }

    /// <summary>
    /// The profile's weight is "my weight now", so it follows the newest measurement with a weight. An entry for
    /// an older day (filling in history) leaves it alone. Saved in the same SaveChanges as the measurement.
    /// </summary>
    private async Task<UserProfile?> UpdateProfileWeightAsync(
        string userId, UserProfile? profile, BodyMeasurement measurement, DateTimeOffset now, CancellationToken ct)
    {
        if (measurement.WeightKg is not { } weight)
        {
            return profile;
        }

        var newerWeight = await db.BodyMeasurements.AnyAsync(
            m => m.UserId == userId && m.Id != measurement.Id && m.WeightKg != null && m.MeasuredOn > measurement.MeasuredOn, ct);
        if (newerWeight)
        {
            return profile;
        }

        if (profile is null)
        {
            profile = new UserProfile { UserId = userId };
            db.UserProfiles.Add(profile);
        }

        profile.WeightKg = Math.Round(weight, 1, MidpointRounding.AwayFromZero);
        profile.UpdatedAtUtc = now;
        return profile;
    }

    /// <summary>"Today" is the user's today: in Warsaw, 23:30 UTC is already tomorrow.</summary>
    private void EnsureNotInFuture(DateOnly day, UserProfile? profile)
    {
        var today = UserTime.LocalDate(clock.GetUtcNow(), UserTime.Resolve(profile?.TimeZone));
        if (day > today)
        {
            throw new DomainValidationException("Measurements can't be in the future.", "measuredOn");
        }

        if (day.Year < 1900)
        {
            throw new DomainValidationException("Enter a real date.", "measuredOn");
        }
    }

    private Task<decimal?> HeightAsync(string userId, CancellationToken ct) =>
        db.UserProfiles.AsNoTracking().Where(p => p.UserId == userId).Select(p => p.HeightCm).SingleOrDefaultAsync(ct);

    private static void Apply(BodyMeasurement m, MeasurementRequest r, DateTimeOffset now)
    {
        m.MeasuredOn = r.MeasuredOn!.Value;
        m.WeightKg = Round(r.WeightKg);
        m.BodyFatPercent = Round(r.BodyFatPercent);
        m.MuscleMassKg = Round(r.MuscleMassKg);
        m.NeckCm = Round(r.NeckCm);
        m.ChestCm = Round(r.ChestCm);
        m.WaistCm = Round(r.WaistCm);
        m.HipsCm = Round(r.HipsCm);
        m.BicepsLeftCm = Round(r.BicepsLeftCm);
        m.BicepsRightCm = Round(r.BicepsRightCm);
        m.ForearmCm = Round(r.ForearmCm);
        m.ThighLeftCm = Round(r.ThighLeftCm);
        m.ThighRightCm = Round(r.ThighRightCm);
        m.CalfCm = Round(r.CalfCm);
        m.Notes = r.Notes?.Trim() ?? "";
        m.UpdatedAtUtc = now;
    }

    /// <summary>One decimal place is as precise as a scale or a tape measure gets.</summary>
    private static decimal? Round(decimal? value) => value is { } v ? Math.Round(v, 1, MidpointRounding.AwayFromZero) : null;

    private static MeasurementResponse ToResponse(BodyMeasurement m, decimal? heightCm) => new(
        m.Id,
        m.MeasuredOn,
        m.WeightKg,
        BmiCalculator.TryCalculate(m.WeightKg, heightCm),
        m.BodyFatPercent,
        m.MuscleMassKg,
        m.NeckCm,
        m.ChestCm,
        m.WaistCm,
        m.HipsCm,
        m.BicepsLeftCm,
        m.BicepsRightCm,
        m.ForearmCm,
        m.ThighLeftCm,
        m.ThighRightCm,
        m.CalfCm,
        m.Notes);
}
