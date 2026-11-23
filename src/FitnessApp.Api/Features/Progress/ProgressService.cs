using FitnessApp.Api.Common;
using FitnessApp.Api.Features.Exercises;
using FitnessApp.Api.Features.Profile;
using FitnessApp.Api.Features.Sessions;
using FitnessApp.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Api.Features.Progress;


public sealed class ProgressService(AppDbContext db, TimeProvider clock) : IProgressService
{
    /// <summary>One row per logged set of a finished workout.</summary>
    private sealed record SetRow(
        int SessionId, DateTimeOffset StartedAtUtc, int ExerciseId, string ExerciseName, ExerciseCategory Category,
        SetType Type, int Reps, decimal? WeightKg)
    {
        public LoggedSet ToLogged() => new(SessionId, StartedAtUtc, Type, Reps, WeightKg);
    }

    public async Task<ProgressSummaryResponse> GetSummaryAsync(string userId, CancellationToken ct)
    {
        var profile = await db.UserProfiles.AsNoTracking().SingleOrDefaultAsync(p => p.UserId == userId, ct);
        var zone = UserTime.Resolve(profile?.TimeZone);
        var today = UserTime.LocalDate(clock.GetUtcNow(), zone);
        var weekStart = UserTime.WeekStart(today);
        var monthStart = new DateOnly(today.Year, today.Month, 1);

        var workouts = await db.WorkoutSessions.AsNoTracking()
            .Where(s => s.UserId == userId && s.Status == SessionStatus.Completed && s.StartedAtUtc != null)
            .Select(s => s.StartedAtUtc!.Value)
            .ToListAsync(ct);
        var days = workouts.Select(w => UserTime.LocalDate(w, zone)).ToList();

        var since = LocalMidnightUtc(weekStart < monthStart ? weekStart : monthStart, zone);
        var recentSets = await SetsAsync(userId, since, ct);
        decimal VolumeFrom(DateOnly first) =>
            StrengthCalculator.Volume(recentSets.Where(s => UserTime.LocalDate(s.StartedAtUtc, zone) >= first).Select(s => s.ToLogged()));

        // The latest weighed measurement, else the weight typed into the profile.
        var latest = await db.BodyMeasurements.AsNoTracking()
            .Where(m => m.UserId == userId && m.WeightKg != null)
            .OrderByDescending(m => m.MeasuredOn)
            .Select(m => new { m.WeightKg, m.MeasuredOn })
            .FirstOrDefaultAsync(ct);
        var weight = latest?.WeightKg ?? profile?.WeightKg;

        return new ProgressSummaryResponse(
            WorkoutsThisWeek: days.Count(d => d >= weekStart),
            WorkoutsThisMonth: days.Count(d => d >= monthStart),
            WorkoutsTotal: days.Count,
            StreakWeeks: StreakWeeks(days, weekStart),
            VolumeThisWeekKg: VolumeFrom(weekStart),
            VolumeThisMonthKg: VolumeFrom(monthStart),
            WeightKg: weight,
            WeightMeasuredOn: latest?.MeasuredOn,
            Bmi: BmiCalculator.TryCalculate(weight, profile?.HeightCm),
            LastWorkoutAtUtc: workouts.Count > 0 ? workouts.Max() : null);
    }

    /// <summary>
    /// Weeks in a row with at least one workout, counting back from this week. A week that isn't over yet doesn't
    /// break the streak: on Monday morning last week's streak still stands.
    /// </summary>
    private static int StreakWeeks(List<DateOnly> days, DateOnly thisWeek)
    {
        var trainedWeeks = days.Select(UserTime.WeekStart).ToHashSet();
        var week = trainedWeeks.Contains(thisWeek) ? thisWeek : thisWeek.AddDays(-7);
        var streak = 0;
        while (trainedWeeks.Contains(week))
        {
            streak++;
            week = week.AddDays(-7);
        }

        return streak;
    }

    public async Task<IReadOnlyList<WeeklyVolume>> GetWeeklyVolumeAsync(string userId, int weeks, CancellationToken ct)
    {
        var zone = await ZoneAsync(userId, ct);
        var thisWeek = UserTime.WeekStart(UserTime.LocalDate(clock.GetUtcNow(), zone));
        var firstWeek = thisWeek.AddDays(-7 * (weeks - 1));
        var sets = await SetsAsync(userId, LocalMidnightUtc(firstWeek, zone), ct);

        var byWeek = sets.ToLookup(s => UserTime.WeekStart(UserTime.LocalDate(s.StartedAtUtc, zone)));

        // Every week appears, also the empty ones, so a chart shows the gaps.
        return [.. Enumerable.Range(0, weeks).Select(i =>
        {
            var week = firstWeek.AddDays(7 * i);
            var inWeek = byWeek[week].ToList();
            return new WeeklyVolume(
                week,
                StrengthCalculator.Volume(inWeek.Select(s => s.ToLogged())),
                inWeek.Select(s => s.SessionId).Distinct().Count(),
                inWeek.Count(s => StrengthCalculator.CountsForVolume(s.Type)));
        })];
    }

    public async Task<IReadOnlyList<ExerciseProgressRow>> GetExerciseRankingAsync(string userId, ProgressPeriod period, CancellationToken ct)
    {
        var sets = await SetsAsync(userId, Since(period), ct);

        return [.. sets
            .GroupBy(s => (s.ExerciseId, s.ExerciseName, s.Category))
            .Select(g =>
            {
                var logged = g.Select(s => s.ToLogged()).ToList();
                return new ExerciseProgressRow(
                    g.Key.ExerciseId,
                    g.Key.ExerciseName,
                    g.Key.Category,
                    g.Select(s => s.SessionId).Distinct().Count(),
                    StrengthCalculator.Volume(logged),
                    StrengthCalculator.BestOneRepMax(logged),
                    StrengthCalculator.OneRepMaxChangePercent(logged),
                    g.Max(s => s.StartedAtUtc));
            })
            .OrderByDescending(r => r.Workouts).ThenBy(r => r.Name, StringComparer.CurrentCulture)];
    }

    public async Task<ExerciseStatsResponse?> GetExerciseStatsAsync(
        string userId, int exerciseId, ProgressPeriod period, CancellationToken ct)
    {
        var exercise = await db.Exercises.AsNoTracking()
            .Where(e => e.Id == exerciseId && (e.UserId == null || e.UserId == userId))
            .Select(e => new { e.Name, e.Category })
            .SingleOrDefaultAsync(ct);
        if (exercise is null)
        {
            return null;
        }

        var since = Since(period);
        var sets = (await SetsAsync(userId, since, ct, exerciseId)).Select(s => s.ToLogged()).ToList();

        var points = sets
            .GroupBy(s => (s.SessionId, s.StartedAtUtc))
            .OrderBy(g => g.Key.StartedAtUtc)
            .Select(g =>
            {
                var top = g.Where(s => StrengthCalculator.CountsForRecords(s.Type) && s.WeightKg is > 0)
                    .OrderByDescending(s => s.WeightKg).ThenByDescending(s => s.Reps).FirstOrDefault();
                return new ExerciseSessionPoint(
                    g.Key.SessionId,
                    g.Key.StartedAtUtc,
                    StrengthCalculator.BestOneRepMax(g),
                    top?.WeightKg,
                    top?.Reps,
                    StrengthCalculator.Volume(g),
                    g.Count(s => StrengthCalculator.CountsForVolume(s.Type)));
            })
            .ToList();

        // Workouts per week over the period (or since the first time, for "all"), at least one week.
        var from = since ?? (points.Count > 0 ? points[0].StartedAtUtc : clock.GetUtcNow());
        var weeks = Math.Max(1m, (decimal)(clock.GetUtcNow() - from).TotalDays / 7m);

        var working = sets.Where(s => StrengthCalculator.CountsForVolume(s.Type) && s.Reps > 0).ToList();
        RepRangeCount Range(string label, int min, int max) => new(label, working.Count(s => s.Reps >= min && s.Reps <= max));

        return new ExerciseStatsResponse(
            exerciseId,
            exercise.Name,
            exercise.Category,
            period,
            points,
            StrengthCalculator.BestOneRepMax(sets),
            StrengthCalculator.Records(sets).HeaviestSet,
            StrengthCalculator.OneRepMaxChangePercent(sets),
            Math.Round(points.Count / weeks, 1, MidpointRounding.AwayFromZero),
            // The usual training ranges: strength, strength and size, size, endurance.
            [Range("1–5", 1, 5), Range("6–8", 6, 8), Range("9–12", 9, 12), Range("13+", 13, int.MaxValue)]);
    }

    public async Task<IReadOnlyList<ExerciseRecordsResponse>> GetRecordsAsync(string userId, CancellationToken ct)
    {
        var sets = await SetsAsync(userId, since: null, ct);

        return [.. sets
            .GroupBy(s => (s.ExerciseId, s.ExerciseName, s.Category))
            .Select(g =>
            {
                var records = StrengthCalculator.Records(g.Select(s => s.ToLogged()));
                return new ExerciseRecordsResponse(
                    g.Key.ExerciseId, g.Key.ExerciseName, g.Key.Category, records.HeaviestSet, records.BestOneRepMax, records.BestSessionVolume);
            })
            .Where(r => r.HeaviestSet is not null || r.BestSessionVolume is not null)
            .OrderBy(r => r.Name, StringComparer.CurrentCulture)];
    }

    public async Task<IReadOnlyList<MuscleGroupVolume>> GetMuscleGroupVolumeAsync(string userId, ProgressPeriod period, CancellationToken ct)
    {
        var since = Since(period);
        var sets = await SetsAsync(userId, since, ct);
        if (sets.Count == 0)
        {
            return [];
        }

        var from = since ?? sets.Min(s => s.StartedAtUtc);
        var weeks = Math.Max(1m, (decimal)(clock.GetUtcNow() - from).TotalDays / 7m);

        return [.. sets
            .Where(s => StrengthCalculator.CountsForVolume(s.Type))
            .GroupBy(s => s.Category)
            .Select(g => new MuscleGroupVolume(
                g.Key,
                Math.Round(g.Count() / weeks, 1, MidpointRounding.AwayFromZero),
                Math.Round(StrengthCalculator.Volume(g.Select(s => s.ToLogged())) / weeks, 0, MidpointRounding.AwayFromZero)))
            .OrderByDescending(m => m.SetsPerWeek)];
    }

    // ---- Helpers -----------------------------------------------------------------------------------------

    /// <summary>The sets of the user's finished workouts since a moment (null: all), optionally of one exercise.</summary>
    private async Task<List<SetRow>> SetsAsync(string userId, DateTimeOffset? since, CancellationToken ct, int? exerciseId = null)
    {
        var query =
            from set in db.SetEntries.AsNoTracking()
            join entry in db.SessionExercises.AsNoTracking() on set.SessionExerciseId equals entry.Id
            where entry.Session.UserId == userId
                && entry.Session.Status == SessionStatus.Completed
                && entry.Session.StartedAtUtc != null
            select new { set, entry };

        if (since is { } from)
        {
            query = query.Where(x => x.entry.Session.StartedAtUtc >= from);
        }

        if (exerciseId is { } id)
        {
            query = query.Where(x => x.entry.ExerciseId == id);
        }

        return await query
            .Select(x => new SetRow(
                x.entry.WorkoutSessionId,
                x.entry.Session.StartedAtUtc!.Value,
                x.entry.ExerciseId,
                x.entry.Exercise.Name,
                x.entry.Exercise.Category,
                x.set.Type,
                x.set.Reps,
                x.set.WeightKg))
            .ToListAsync(ct);
    }

    private DateTimeOffset? Since(ProgressPeriod period)
    {
        var now = clock.GetUtcNow();
        return period switch
        {
            ProgressPeriod.FourWeeks => now.AddDays(-28),
            ProgressPeriod.ThreeMonths => now.AddDays(-91),
            ProgressPeriod.Year => now.AddDays(-365),
            _ => null,
        };
    }

    /// <summary>Midnight of a local day, as a UTC moment, to filter workouts in the database.</summary>
    private static DateTimeOffset LocalMidnightUtc(DateOnly day, TimeZoneInfo zone)
    {
        var local = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return new DateTimeOffset(local, zone.GetUtcOffset(local)).ToUniversalTime();
    }

    private async Task<TimeZoneInfo> ZoneAsync(string userId, CancellationToken ct) =>
        UserTime.Resolve(await db.UserProfiles.AsNoTracking().Where(p => p.UserId == userId).Select(p => p.TimeZone).SingleOrDefaultAsync(ct));
}
