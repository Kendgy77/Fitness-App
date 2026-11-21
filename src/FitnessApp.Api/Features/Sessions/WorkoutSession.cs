using FitnessApp.Api.Features.Exercises;
using FitnessApp.Api.Features.Plans;

namespace FitnessApp.Api.Features.Sessions;

/// <summary>
/// One workout (thesis class "SesjaTreningowa", WF06). It is logged live at the gym (start, add sets, finish) or
/// entered afterwards as a completed workout (the thesis form, T11). Started from a plan, it gets a copy of the
/// plan's exercises, so editing the plan later never changes a past workout.
/// </summary>
public sealed class WorkoutSession
{
    public int Id { get; set; }

    public string UserId { get; set; } = null!;

    /// <summary>The plan it was started from, for "most recently used" and history. Null for an empty workout.</summary>
    public int? PlanId { get; set; }

    public TrainingPlan? Plan { get; set; }

    public string Name { get; set; } = null!;

    public SessionType Type { get; set; }

    /// <summary>Changes only through <see cref="SessionStateMachine"/>.</summary>
    public SessionStatus Status { get; set; }

    /// <summary>For planned workouts (reminders, week 6).</summary>
    public DateTimeOffset? ScheduledAtUtc { get; set; }

    public DateTimeOffset? StartedAtUtc { get; set; }

    public DateTimeOffset? FinishedAtUtc { get; set; }

    /// <summary>Set while the workout is paused; the pause is added to <see cref="PausedSeconds"/> on resume.</summary>
    public DateTimeOffset? PausedAtUtc { get; set; }

    /// <summary>Total time spent paused, left out of the duration.</summary>
    public int PausedSeconds { get; set; }

    /// <summary>Entered for a past workout; calculated when a live workout is finished.</summary>
    public int? DurationMinutes { get; set; }

    public string Notes { get; set; } = "";

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public List<SessionExercise> Exercises { get; set; } = [];
}

/// <summary>
/// One exercise in a workout (with the sets done). This extra level between the workout and its sets keeps the
/// exercise order and a note per exercise. Targets are copied from the plan and shown as hints.
/// </summary>
public sealed class SessionExercise
{
    public int Id { get; set; }

    public int WorkoutSessionId { get; set; }

    public WorkoutSession Session { get; set; } = null!;

    public int ExerciseId { get; set; }

    public Exercise Exercise { get; set; } = null!;

    public int Order { get; set; }

    public string Notes { get; set; } = "";

    public int? TargetSets { get; set; }

    public int? TargetRepsMin { get; set; }

    public int? TargetRepsMax { get; set; }

    public decimal? TargetWeightKg { get; set; }

    public List<SetEntry> Sets { get; set; } = [];
}

/// <summary>
/// One set (thesis class "SeriaCwiczenia"). Strength sets use reps and weight; cardio uses time and distance.
/// A drop set belongs to the set right before it, so its position is the link and needs no extra column.
/// </summary>
public sealed class SetEntry
{
    public int Id { get; set; }

    public int SessionExerciseId { get; set; }

    /// <summary>1, 2, 3, … within the exercise.</summary>
    public int SetNumber { get; set; }

    public SetType Type { get; set; }

    public int Reps { get; set; }

    /// <summary>Null for bodyweight exercises.</summary>
    public decimal? WeightKg { get; set; }

    /// <summary>Rate of perceived exertion, 1 (very easy) to 10 (nothing left). Optional.</summary>
    public int? Rpe { get; set; }

    public int? DurationSeconds { get; set; }

    public int? DistanceMeters { get; set; }

    /// <summary>When the set was logged: orders the history and starts the rest timer.</summary>
    public DateTimeOffset CompletedAtUtc { get; set; }
}

public enum SessionType
{
    Strength,
    Cardio,
    Mixed,
    Other,
}

/// <summary>Planned → InProgress ⇄ Paused → Completed (see <see cref="SessionStateMachine"/>).</summary>
public enum SessionStatus
{
    Planned,
    InProgress,
    Paused,
    Completed,
}

/// <summary>
/// Warm-up sets are logged but left out of volume and records. A drop set is the same exercise straight after
/// a set, with less weight. Failure means the last rep could not be completed.
/// </summary>
public enum SetType
{
    Normal,
    Warmup,
    Drop,
    Failure,
}
