namespace FitnessApp.Api.Features.Sessions;

/// <summary>
/// The rules for a workout's status, with no database or HTTP, so they are easy to read and to test:
/// <code>
///   Planned ──start──▶ InProgress ──pause──▶ Paused
///                          ▲   │               │
///                          │   └─────finish────┼──▶ Completed
///                          └──────resume───────┘      ▲
///                                     Paused ──finish─┘
/// </code>
/// A past workout entered afterwards is created directly as Completed. A completed workout can still be edited
/// (to fix a typo) and stays Completed. Any workout can be deleted (discarding a workout is the user's choice).
/// </summary>
public static class SessionStateMachine
{
    /// <summary>The status after <paramref name="action"/>, or null when it isn't allowed from <paramref name="from"/>.</summary>
    public static SessionStatus? Next(SessionStatus from, SessionAction action) => (from, action) switch
    {
        (SessionStatus.Planned, SessionAction.Start) => SessionStatus.InProgress,
        (SessionStatus.InProgress, SessionAction.Pause) => SessionStatus.Paused,
        (SessionStatus.Paused, SessionAction.Resume) => SessionStatus.InProgress,
        (SessionStatus.InProgress or SessionStatus.Paused, SessionAction.Finish) => SessionStatus.Completed,
        _ => null,
    };

    /// <summary>Started and not yet finished. Only one workout can be active at a time.</summary>
    public static bool IsActive(SessionStatus status) => status is SessionStatus.InProgress or SessionStatus.Paused;

    /// <summary>
    /// Sets are logged one by one only during the workout. A finished workout is corrected with the full form,
    /// which is checked as a whole (T12), so it can never be left without sets.
    /// </summary>
    public static bool CanLogSets(SessionStatus status) => IsActive(status);

    /// <summary>Exercises can be added or removed one by one until the workout is finished.</summary>
    public static bool CanChangeExercises(SessionStatus status) => status != SessionStatus.Completed;

    /// <summary>
    /// Minutes from start to finish without the pauses, rounded, and at least 1. Used when a live workout is
    /// finished without a duration typed in.
    /// </summary>
    public static int DurationMinutes(DateTimeOffset startedAt, DateTimeOffset finishedAt, int pausedSeconds)
    {
        var seconds = (finishedAt - startedAt).TotalSeconds - pausedSeconds;
        return Math.Max(1, (int)Math.Round(seconds / 60, MidpointRounding.AwayFromZero));
    }
}

public enum SessionAction
{
    Start,
    Pause,
    Resume,
    Finish,
}
