using FitnessApp.Api.Common;

namespace FitnessApp.Api.Features.Sessions;

/// <summary>
/// Workouts (WF06). Every method takes the caller's id; another user's workout behaves as if it didn't exist
/// (null → 404), including its exercises and sets.
/// </summary>
public interface ISessionService
{
    /// <summary>History, newest first, with optional date range, exercise and status filters.</summary>
    Task<PagedResponse<SessionSummaryResponse>> ListAsync(string userId, SessionQuery query, CancellationToken ct);

    Task<SessionResponse?> GetAsync(string userId, int id, CancellationToken ct);

    /// <summary>The workout in progress or paused, if any, so the live screen can pick up where it left off.</summary>
    Task<SessionResponse?> GetActiveAsync(string userId, CancellationToken ct);

    Task<StartOptionsResponse> GetStartOptionsAsync(string userId, CancellationToken ct);

    Task<ExerciseSuggestionsResponse> GetExerciseSuggestionsAsync(string userId, CancellationToken ct);

    /// <summary>Every finished workout with this exercise, newest first; null if the exercise isn't visible.</summary>
    Task<PagedResponse<ExerciseHistoryEntry>?> GetExerciseHistoryAsync(string userId, int exerciseId, PageQuery query, CancellationToken ct);

    /// <summary>Saves a workout entered afterwards as Completed (T11; without exercises → 400, T12).</summary>
    Task<SessionResponse> LogAsync(string userId, LogSessionRequest request, CancellationToken ct);

    /// <summary>Replaces a finished workout's details, exercises and sets.</summary>
    Task<SessionResponse?> UpdateAsync(string userId, int id, LogSessionRequest request, CancellationToken ct);

    /// <summary>Starts a live workout now, empty or from a plan. Only one workout can be active (else 409).</summary>
    Task<SessionResponse> StartAsync(string userId, StartSessionRequest request, CancellationToken ct);

    /// <summary>Start, pause, resume or finish. Disallowed changes answer 409.</summary>
    Task<SessionResponse?> ApplyAsync(string userId, int id, SessionAction action, FinishSessionRequest? finish, CancellationToken ct);

    Task<SessionResponse?> AddExerciseAsync(string userId, int id, int exerciseId, CancellationToken ct);

    Task<SessionResponse?> RemoveExerciseAsync(string userId, int id, int sessionExerciseId, CancellationToken ct);

    /// <summary>The fast path at the gym: appends one set and returns the whole workout.</summary>
    Task<SessionResponse?> AddSetAsync(string userId, int id, int sessionExerciseId, SetRequest request, CancellationToken ct);

    Task<SessionResponse?> UpdateSetAsync(string userId, int id, int sessionExerciseId, int setId, SetRequest request, CancellationToken ct);

    Task<SessionResponse?> DeleteSetAsync(string userId, int id, int sessionExerciseId, int setId, CancellationToken ct);

    Task<bool> DeleteAsync(string userId, int id, CancellationToken ct);
}
