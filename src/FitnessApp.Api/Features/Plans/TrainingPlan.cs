using FitnessApp.Api.Features.Exercises;

namespace FitnessApp.Api.Features.Plans;

/// <summary>
/// A training plan (thesis class "PlanTreningowy", WF05): a named, ordered list of exercises with target sets and
/// reps. A user's plans are private. Templates (<see cref="UserId"/> is null) ship with the app, are visible to
/// everyone and can only be copied.
/// </summary>
public sealed class TrainingPlan
{
    public int Id { get; set; }

    /// <summary>Owner; null for a template.</summary>
    public string? UserId { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = "";

    /// <summary>Changes only through <see cref="PlanStateMachine"/>.</summary>
    public PlanStatus Status { get; set; } = PlanStatus.Draft;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public List<PlanExercise> Exercises { get; set; } = [];
}

/// <summary>
/// One exercise in a plan (the thesis table "plan_cwiczenie"). The same exercise may appear twice, e.g. a light
/// warm-up set block and a heavy one, so the key is its own id and the position is <see cref="Order"/>.
/// </summary>
public sealed class PlanExercise
{
    public int Id { get; set; }

    public int TrainingPlanId { get; set; }

    public int ExerciseId { get; set; }

    public Exercise Exercise { get; set; } = null!;

    /// <summary>1, 2, 3, … Rows in a database have no natural order, so the order is stored.</summary>
    public int Order { get; set; }

    public int TargetSets { get; set; }

    public int TargetReps { get; set; }

    /// <summary>Optional starting weight; null for bodyweight exercises or "decide on the day".</summary>
    public decimal? TargetWeightKg { get; set; }

    public string Notes { get; set; } = "";
}

/// <summary>
/// Draft: being built, not offered when starting a workout. Active: in use. Archived: kept for history, read-only.
/// </summary>
public enum PlanStatus
{
    Draft,
    Active,
    Archived,
}
