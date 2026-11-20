using System.ComponentModel.DataAnnotations;
using FitnessApp.Api.Common;
using FitnessApp.Api.Features.Exercises;

namespace FitnessApp.Api.Features.Plans;

/// <summary>Create or replace a plan. The exercises are sent in the order they should appear.</summary>
public sealed record PlanRequest(
    [Required(ErrorMessage = "Enter the plan name.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "The name must be 2 to 100 characters long.")]
    string Name,
    [StringLength(1000, ErrorMessage = "The description can be at most 1000 characters long.")]
    string? Description,
    [Required]
    [MaxLength(30, ErrorMessage = "A plan can have at most 30 exercises.")]
    IReadOnlyList<PlanExerciseRequest> Exercises);

public sealed record PlanExerciseRequest(
    int ExerciseId,
    [Range(1, 20, ErrorMessage = "Sets must be between 1 and 20.")]
    int TargetSets,
    [Range(1, 100, ErrorMessage = "Reps must be between 1 and 100.")]
    int TargetReps,
    [Range(0.0, 1000.0, ErrorMessage = "Weight must be between 0 and 1000 kg.")]
    decimal? TargetWeightKg,
    [StringLength(200, ErrorMessage = "Notes can be at most 200 characters long.")]
    string? Notes);

public sealed record ChangePlanStatusRequest([Required(ErrorMessage = "Choose a status.")] PlanStatus? Status);

/// <summary><c>/api/plans?status=Active&amp;page=1</c>.</summary>
public sealed record PlanQuery : PageQuery
{
    public PlanStatus? Status { get; init; }
}

/// <summary>A row in the plan list: no exercises, just how many.</summary>
public sealed record PlanSummaryResponse(
    int Id,
    string Name,
    string Description,
    PlanStatus Status,
    bool IsTemplate,
    int ExerciseCount,
    DateTimeOffset UpdatedAtUtc);

public sealed record PlanResponse(
    int Id,
    string Name,
    string Description,
    PlanStatus Status,
    bool IsTemplate,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<PlanExerciseResponse> Exercises);

/// <summary>The exercise's name and category come along, so the plan can be shown without extra requests.</summary>
public sealed record PlanExerciseResponse(
    int Order,
    int ExerciseId,
    string ExerciseName,
    ExerciseCategory Category,
    Equipment Equipment,
    bool ExerciseIsArchived,
    int TargetSets,
    int TargetReps,
    decimal? TargetWeightKg,
    string Notes);
