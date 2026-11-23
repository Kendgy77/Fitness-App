using FitnessApp.Api.Features.Progress;
using FitnessApp.Api.Features.Sessions;
using static FitnessApp.Api.Features.Sessions.SetType;

namespace FitnessApp.UnitTests.Progress;

public sealed class StrengthCalculatorTests
{
    private static readonly DateTimeOffset Day1 = new(2026, 11, 2, 18, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Day2 = Day1.AddDays(3);

    private static LoggedSet Set(int session, DateTimeOffset at, SetType type, int reps, decimal? kg) => new(session, at, type, reps, kg);

    [Theory]
    [InlineData(80, 8, 101.3)]   // 80 × (1 + 8/30) = 101.33…
    [InlineData(100, 5, 116.7)]
    [InlineData(60, 12, 84.0)]   // 12 reps is the last one estimated
    [InlineData(140, 1, 140.0)]  // a single is the 1RM itself
    public void Epley_estimates_the_one_rep_max(double kg, int reps, double expected)
    {
        StrengthCalculator.EstimateOneRepMax((decimal)kg, reps).ShouldBe((decimal)expected);
    }

    [Theory]
    [InlineData(60.0, 13)]  // too many reps to be reliable
    [InlineData(60.0, 0)]
    [InlineData(null, 10)]  // bodyweight
    [InlineData(0.0, 10)]
    public void No_estimate_without_weight_or_with_too_many_reps(double? kg, int reps)
    {
        StrengthCalculator.EstimateOneRepMax((decimal?)kg, reps).ShouldBeNull();
    }

    [Fact]
    public void Volume_counts_working_and_drop_sets_but_not_warm_ups_or_bodyweight()
    {
        LoggedSet[] sets =
        [
            Set(1, Day1, Warmup, 10, 40),   // 0
            Set(1, Day1, Normal, 8, 80),    // 640
            Set(1, Day1, Failure, 6, 80),   // 480
            Set(1, Day1, Drop, 10, 60),     // 600
            Set(1, Day1, Normal, 12, null), // bodyweight: 0
        ];

        StrengthCalculator.Volume(sets).ShouldBe(1720m);
    }

    [Fact]
    public void Volume_of_nothing_is_zero()
    {
        StrengthCalculator.Volume([]).ShouldBe(0m);
        StrengthCalculator.BestOneRepMax([]).ShouldBeNull();
    }

    [Fact]
    public void Records_ignore_warm_ups_and_drop_sets()
    {
        LoggedSet[] sets =
        [
            Set(1, Day1, Warmup, 3, 120), // heavier, but a warm-up
            Set(1, Day1, Normal, 5, 100),
            Set(1, Day1, Drop, 12, 110),  // only possible after a heavier set; not a record
        ];

        var records = StrengthCalculator.Records(sets);

        records.HeaviestSet!.Value.ShouldBe(100m);
        records.BestOneRepMax!.Value.ShouldBe(116.7m);
    }

    [Fact]
    public void Records_point_at_the_workout_they_came_from()
    {
        LoggedSet[] sets =
        [
            Set(1, Day1, Normal, 8, 80),  // 1RM 101.3, volume 640
            Set(1, Day1, Normal, 8, 80),  // workout 1 volume 1280
            Set(2, Day2, Normal, 3, 95),  // heaviest; 1RM 104.5; volume 285
        ];

        var records = StrengthCalculator.Records(sets);

        (records.HeaviestSet!.Value, records.HeaviestSet.Reps, records.HeaviestSet.SessionId).ShouldBe((95m, 3, 2));
        (records.BestOneRepMax!.Value, records.BestOneRepMax.SessionId).ShouldBe((104.5m, 2));
        (records.BestSessionVolume!.Value, records.BestSessionVolume.SessionId).ShouldBe((1280m, 1));
    }

    [Fact]
    public void A_tie_goes_to_the_first_time_and_more_reps_beat_fewer_at_the_same_weight()
    {
        LoggedSet[] sets =
        [
            Set(1, Day1, Normal, 5, 100),
            Set(2, Day2, Normal, 5, 100),
            Set(2, Day2, Failure, 6, 100),
        ];

        var heaviest = StrengthCalculator.Records(sets).HeaviestSet!;

        (heaviest.Reps, heaviest.SessionId).ShouldBe((6, 2));
        StrengthCalculator.Records(sets.Take(2)).HeaviestSet!.SessionId.ShouldBe(1);
    }

    [Fact]
    public void Bodyweight_exercises_have_no_strength_records()
    {
        var records = StrengthCalculator.Records([Set(1, Day1, Normal, 15, null)]);

        records.ShouldBe(new ExerciseRecords(null, null, null));
    }

    [Fact]
    public void One_rep_max_change_compares_the_first_and_last_workout()
    {
        LoggedSet[] sets =
        [
            Set(1, Day1, Normal, 5, 100),  // 116.7
            Set(2, Day2, Normal, 5, 105),  // 122.5
        ];

        StrengthCalculator.OneRepMaxChangePercent(sets).ShouldBe(5.0m); // (122.5 − 116.7) / 116.7 = 4.97 %
        StrengthCalculator.OneRepMaxChangePercent(sets.Take(1)).ShouldBeNull();
    }
}
