using FitnessApp.Api.Features.Sessions;

namespace FitnessApp.Api.Features.Progress;

/// <summary>One logged set, as the progress statistics see it.</summary>
public sealed record LoggedSet(int SessionId, DateTimeOffset StartedAtUtc, SetType Type, int Reps, decimal? WeightKg);

/// <summary>A record: its value, and the set or workout it came from.</summary>
public sealed record RecordValue(decimal Value, decimal? WeightKg, int? Reps, int SessionId, DateTimeOffset AchievedAtUtc);

public sealed record ExerciseRecords(RecordValue? HeaviestSet, RecordValue? BestOneRepMax, RecordValue? BestSessionVolume);

/// <summary>
/// Strength statistics (WF07): estimated one-rep max, training volume and personal records. Pure functions with
/// no database, so every rule is unit-tested and easy to explain:
/// <list type="bullet">
/// <item>Warm-up sets are logged but count for nothing here.</item>
/// <item>Drop sets count for volume (the work was done) but not for records (they follow a heavier set).</item>
/// <item>Bodyweight sets have no weight, so they add no volume and no 1RM.</item>
/// </list>
/// </summary>
public static class StrengthCalculator
{
    /// <summary>Above 12 reps the Epley formula overestimates a lot, so no estimate is given.</summary>
    public const int MaxRepsForOneRepMax = 12;

    /// <summary>
    /// Epley: 1RM = weight × (1 + reps / 30), rounded to 0.1 kg. One rep is the 1RM itself. Null without a weight,
    /// without reps or above <see cref="MaxRepsForOneRepMax"/> reps. Example: 80 kg × 8 → 101.3 kg.
    /// </summary>
    public static decimal? EstimateOneRepMax(decimal? weightKg, int reps)
    {
        if (weightKg is not > 0 || reps <= 0 || reps > MaxRepsForOneRepMax)
        {
            return null;
        }

        return reps == 1 ? weightKg.Value : Math.Round(weightKg.Value * (1 + (reps / 30m)), 1, MidpointRounding.AwayFromZero);
    }

    /// <summary>Normal and to-failure sets show what the user can lift; warm-ups and drop sets don't.</summary>
    public static bool CountsForRecords(SetType type) => type is SetType.Normal or SetType.Failure;

    /// <summary>Everything but warm-ups is real work.</summary>
    public static bool CountsForVolume(SetType type) => type != SetType.Warmup;

    /// <summary>Reps × weight for a working set; 0 for warm-ups and sets without weight.</summary>
    public static decimal SetVolume(SetType type, int reps, decimal? weightKg) =>
        CountsForVolume(type) && weightKg is > 0 && reps > 0 ? reps * weightKg.Value : 0;

    public static decimal Volume(IEnumerable<LoggedSet> sets) => sets.Sum(s => SetVolume(s.Type, s.Reps, s.WeightKg));

    /// <summary>The best estimated 1RM among the sets that count for records, or null.</summary>
    public static decimal? BestOneRepMax(IEnumerable<LoggedSet> sets) =>
        sets.Where(s => CountsForRecords(s.Type)).Select(s => EstimateOneRepMax(s.WeightKg, s.Reps)).Max();

    /// <summary>
    /// Personal records of one exercise: the heaviest set (more reps break a tie), the best estimated 1RM and
    /// the most volume in one workout. The earliest set wins a tie, because that's when the record was set.
    /// </summary>
    public static ExerciseRecords Records(IEnumerable<LoggedSet> sets)
    {
        var list = sets.OrderBy(s => s.StartedAtUtc).ToList();
        var counting = list.Where(s => CountsForRecords(s.Type) && s.WeightKg is > 0 && s.Reps > 0).ToList();

        var heaviest = counting
            .OrderByDescending(s => s.WeightKg).ThenByDescending(s => s.Reps).ThenBy(s => s.StartedAtUtc)
            .Select(s => new RecordValue(s.WeightKg!.Value, s.WeightKg, s.Reps, s.SessionId, s.StartedAtUtc))
            .FirstOrDefault();

        var bestOneRepMax = counting
            .Select(s => (Set: s, Estimate: EstimateOneRepMax(s.WeightKg, s.Reps)))
            .Where(x => x.Estimate is not null)
            .OrderByDescending(x => x.Estimate).ThenBy(x => x.Set.StartedAtUtc)
            .Select(x => new RecordValue(x.Estimate!.Value, x.Set.WeightKg, x.Set.Reps, x.Set.SessionId, x.Set.StartedAtUtc))
            .FirstOrDefault();

        var bestVolume = list
            .GroupBy(s => (s.SessionId, s.StartedAtUtc))
            .Select(g => new RecordValue(Volume(g), null, null, g.Key.SessionId, g.Key.StartedAtUtc))
            .Where(r => r.Value > 0)
            .OrderByDescending(r => r.Value).ThenBy(r => r.AchievedAtUtc)
            .FirstOrDefault();

        return new ExerciseRecords(heaviest, bestOneRepMax, bestVolume);
    }

    /// <summary>
    /// How much the best estimated 1RM changed from the first to the last workout in a period, in percent with one
    /// decimal; null with fewer than two workouts that have an estimate.
    /// </summary>
    public static decimal? OneRepMaxChangePercent(IEnumerable<LoggedSet> sets)
    {
        var perSession = sets
            .GroupBy(s => (s.SessionId, s.StartedAtUtc))
            .OrderBy(g => g.Key.StartedAtUtc)
            .Select(g => BestOneRepMax(g))
            .Where(v => v is not null)
            .ToList();

        if (perSession.Count < 2 || perSession[0] is not { } first || perSession[^1] is not { } last)
        {
            return null;
        }

        return Math.Round((last - first) / first * 100, 1, MidpointRounding.AwayFromZero);
    }
}
