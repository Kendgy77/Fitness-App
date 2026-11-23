using System.ComponentModel.DataAnnotations;
using FitnessApp.Api.Features.Exercises;
using FitnessApp.Api.Features.Profile;

namespace FitnessApp.Api.Features.Progress;

/// <summary>How far back the statistics look.</summary>
public enum ProgressPeriod
{
    FourWeeks,
    ThreeMonths,
    Year,
    All,
}

public sealed record PeriodQuery
{
    public ProgressPeriod Period { get; init; } = ProgressPeriod.ThreeMonths;
}

public sealed record VolumeQuery
{
    [Range(1, 52, ErrorMessage = "Weeks must be between 1 and 52.")]
    public int Weeks { get; init; } = 12;
}

/// <summary>The dashboard's numbers (thesis Fig. 6.2). Weeks and months are the user's own (time zone).</summary>
public sealed record ProgressSummaryResponse(
    int WorkoutsThisWeek,
    int WorkoutsThisMonth,
    int WorkoutsTotal,
    int StreakWeeks,
    decimal VolumeThisWeekKg,
    decimal VolumeThisMonthKg,
    decimal? WeightKg,
    DateOnly? WeightMeasuredOn,
    BmiResult? Bmi,
    DateTimeOffset? LastWorkoutAtUtc);

public sealed record WeeklyVolume(DateOnly WeekStart, decimal VolumeKg, int Workouts, int Sets);

/// <summary>A row of the exercise ranking table; the app sorts it by any column.</summary>
public sealed record ExerciseProgressRow(
    int ExerciseId,
    string Name,
    ExerciseCategory Category,
    int Workouts,
    decimal VolumeKg,
    decimal? BestOneRepMaxKg,
    decimal? OneRepMaxChangePercent,
    DateTimeOffset LastDoneAtUtc);

public sealed record ExerciseSessionPoint(
    int SessionId,
    DateTimeOffset StartedAtUtc,
    decimal? BestOneRepMaxKg,
    decimal? TopSetWeightKg,
    int? TopSetReps,
    decimal VolumeKg,
    int WorkingSets);

public sealed record RepRangeCount(string Range, int Sets);

/// <summary>The detailed page of one exercise for a period.</summary>
public sealed record ExerciseStatsResponse(
    int ExerciseId,
    string Name,
    ExerciseCategory Category,
    ProgressPeriod Period,
    IReadOnlyList<ExerciseSessionPoint> Workouts,
    decimal? BestOneRepMaxKg,
    RecordValue? HeaviestSet,
    decimal? OneRepMaxChangePercent,
    decimal WorkoutsPerWeek,
    IReadOnlyList<RepRangeCount> RepRanges);

public sealed record ExerciseRecordsResponse(
    int ExerciseId,
    string Name,
    ExerciseCategory Category,
    RecordValue? HeaviestSet,
    RecordValue? BestOneRepMax,
    RecordValue? BestSessionVolume);

/// <summary>Working sets and volume per week for one muscle group, averaged over the period.</summary>
public sealed record MuscleGroupVolume(ExerciseCategory Category, decimal SetsPerWeek, decimal VolumePerWeekKg);
