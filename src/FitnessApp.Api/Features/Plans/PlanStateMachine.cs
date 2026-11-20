namespace FitnessApp.Api.Features.Plans;

/// <summary>
/// The rules for a plan's status, in one place, with no database or HTTP, so they are easy to read and to test:
/// <code>
///   Draft ──activate──▶ Active ──archive──▶ Archived
///     │                                      │
///     └──────────────archive────────────────▶│
///     ◀──────────────restore─────────────────┘
/// </code>
/// Active plans are edited in place (workouts copy a plan's exercises, so editing never changes history).
/// Archived plans are read-only until restored, which brings them back as a draft.
/// </summary>
public static class PlanStateMachine
{
    private static readonly HashSet<(PlanStatus From, PlanStatus To)> Allowed =
    [
        (PlanStatus.Draft, PlanStatus.Active),
        (PlanStatus.Draft, PlanStatus.Archived),
        (PlanStatus.Active, PlanStatus.Archived),
        (PlanStatus.Archived, PlanStatus.Draft),
    ];

    /// <summary>Why a change is refused, or null when it is allowed.</summary>
    public static PlanTransitionError? Check(PlanStatus from, PlanStatus to, int exerciseCount)
    {
        if (from == to)
        {
            return PlanTransitionError.AlreadyInStatus;
        }

        if (!Allowed.Contains((from, to)))
        {
            return PlanTransitionError.NotAllowed;
        }

        // An empty plan can't be used for a workout, so it can't be activated.
        if (to == PlanStatus.Active && exerciseCount == 0)
        {
            return PlanTransitionError.NoExercises;
        }

        return null;
    }

    /// <summary>Only drafts and active plans can be changed; an archived plan is history.</summary>
    public static bool CanEdit(PlanStatus status) => status != PlanStatus.Archived;

    /// <summary>
    /// Only drafts can be deleted. A plan that was ever active may be referenced by past workouts, so it is
    /// archived instead.
    /// </summary>
    public static bool CanDelete(PlanStatus status) => status == PlanStatus.Draft;
}

public enum PlanTransitionError
{
    AlreadyInStatus,
    NotAllowed,
    NoExercises,
}
