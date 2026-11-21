using System.ComponentModel.DataAnnotations;
using FitnessApp.Api.Common;
using FitnessApp.Api.Features.Exercises;

namespace FitnessApp.Api.Features.Sessions;

// ---- Requests ----------------------------------------------------------------------------------------------

/// <summary>One set. Strength: reps (and weight). Cardio: time and/or distance.</summary>
public sealed record SetRequest(
    SetType? Type,
    [Range(0, 200, ErrorMessage = "Reps must be between 0 and 200.")]
    int Reps,
    [Range(0.0, 1000.0, ErrorMessage = "Weight must be between 0 and 1000 kg.")]
    decimal? WeightKg,
    [Range(1, 10, ErrorMessage = "RPE must be between 1 and 10.")]
    int? Rpe,
    [Range(1, 86_400, ErrorMessage = "Time must be between 1 second and 24 hours.")]
    int? DurationSeconds,
    [Range(1, 1_000_000, ErrorMessage = "Distance must be between 1 m and 1000 km.")]
    int? DistanceMeters) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Reps == 0 && DurationSeconds is null && DistanceMeters is null)
        {
            yield return new ValidationResult("Enter the reps, or the time or distance for cardio.", [nameof(Reps)]);
        }
    }
}

public sealed record LoggedExerciseRequest(
    int ExerciseId,
    [StringLength(200, ErrorMessage = "Notes can be at most 200 characters long.")]
    string? Notes,
    [Required]
    [MaxLength(50, ErrorMessage = "An exercise can have at most 50 sets.")]
    IReadOnlyList<SetRequest> Sets) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // T12: every exercise in a finished workout has at least one set.
        if (Sets is { Count: 0 })
        {
            yield return new ValidationResult("Add at least one set.", [nameof(Sets)]);
        }
        else if (Sets is [{ Type: SetType.Drop }, ..])
        {
            yield return new ValidationResult("A drop set must follow another set.", [nameof(Sets)]);
        }
    }
}

/// <summary>A workout entered afterwards, already finished (the thesis form, T11). Also used to edit one.</summary>
public sealed record LogSessionRequest(
    [StringLength(100, ErrorMessage = "The name can be at most 100 characters long.")]
    string? Name,
    [Required(ErrorMessage = "Choose the workout type.")]
    SessionType? Type,
    [Required(ErrorMessage = "Enter the date and time.")]
    DateTimeOffset? StartedAt,
    [Required(ErrorMessage = "Enter how long the workout took.")]
    [Range(1, 1440, ErrorMessage = "The duration must be between 1 and 1440 minutes.")]
    int? DurationMinutes,
    [StringLength(1000, ErrorMessage = "Notes can be at most 1000 characters long.")]
    string? Notes,
    [Required]
    [MaxLength(30, ErrorMessage = "A workout can have at most 30 exercises.")]
    IReadOnlyList<LoggedExerciseRequest> Exercises) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // T12: a workout without exercises is not saved.
        if (Exercises is { Count: 0 })
        {
            yield return new ValidationResult("Add at least one exercise with at least one set.", [nameof(Exercises)]);
        }
    }
}

/// <summary>Start a live workout now: empty, or from one of the user's active plans or a template.</summary>
public sealed record StartSessionRequest(
    int? PlanId,
    [StringLength(100, ErrorMessage = "The name can be at most 100 characters long.")]
    string? Name,
    SessionType? Type);

public sealed record AddSessionExerciseRequest(int ExerciseId);

public sealed record FinishSessionRequest(
    [Range(1, 1440, ErrorMessage = "The duration must be between 1 and 1440 minutes.")]
    int? DurationMinutes,
    [StringLength(1000, ErrorMessage = "Notes can be at most 1000 characters long.")]
    string? Notes);

/// <summary>
/// <c>/api/sessions?from=…&amp;to=…&amp;exerciseId=1</c>. <c>from</c>/<c>to</c> are exact instants, so the app
/// decides where "a day" starts and ends in the user's own time zone.
/// </summary>
public sealed record SessionQuery : PageQuery
{
    public DateTimeOffset? From { get; init; }

    public DateTimeOffset? To { get; init; }

    /// <summary>Only workouts that include this exercise ("every session with bench press").</summary>
    public int? ExerciseId { get; init; }

    public SessionStatus? Status { get; init; }
}

// ---- Responses ---------------------------------------------------------------------------------------------

/// <summary>A row in the history list.</summary>
public sealed record SessionSummaryResponse(
    int Id,
    string Name,
    SessionType Type,
    SessionStatus Status,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? ScheduledAtUtc,
    int? DurationMinutes,
    int ExerciseCount,
    int SetCount,
    IReadOnlyList<string> ExerciseNames);

public sealed record SessionResponse(
    int Id,
    string Name,
    SessionType Type,
    SessionStatus Status,
    int? PlanId,
    DateTimeOffset? ScheduledAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? FinishedAtUtc,
    DateTimeOffset? PausedAtUtc,
    int PausedSeconds,
    int? DurationMinutes,
    string Notes,
    IReadOnlyList<SessionExerciseResponse> Exercises);

public sealed record SessionExerciseResponse(
    int Id,
    int Order,
    int ExerciseId,
    string ExerciseName,
    ExerciseCategory Category,
    Equipment Equipment,
    string Notes,
    int? TargetSets,
    int? TargetRepsMin,
    int? TargetRepsMax,
    decimal? TargetWeightKg,
    IReadOnlyList<SetResponse> Sets);

public sealed record SetResponse(
    int Id,
    int SetNumber,
    SetType Type,
    int Reps,
    decimal? WeightKg,
    int? Rpe,
    int? DurationSeconds,
    int? DistanceMeters,
    DateTimeOffset CompletedAtUtc);

/// <summary>What the "New workout" screen offers: plans to start from, most recently used first.</summary>
public sealed record StartOptionsResponse(IReadOnlyList<StartPlanOption> Plans);

public sealed record StartPlanOption(int Id, string Name, bool IsTemplate, int ExerciseCount, DateTimeOffset? LastUsedAtUtc);

/// <summary>For the exercise picker: the user's most-used exercises, then the most recent ones.</summary>
public sealed record ExerciseSuggestionsResponse(
    IReadOnlyList<ExerciseResponse> MostUsed,
    IReadOnlyList<ExerciseResponse> Recent);

/// <summary>One workout's sets of a single exercise, for the exercise's history.</summary>
public sealed record ExerciseHistoryEntry(
    int SessionId,
    string SessionName,
    DateTimeOffset StartedAtUtc,
    IReadOnlyList<SetResponse> Sets);
