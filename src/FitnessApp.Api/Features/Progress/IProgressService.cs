namespace FitnessApp.Api.Features.Progress;

/// <summary>
/// Progress statistics (WF07, thesis T13). Nothing is stored: every number is calculated per request from the
/// finished workouts, so it can never go out of date when a workout is edited or deleted. One projection query
/// loads only the columns needed (date, exercise, set type, reps, weight); <see cref="StrengthCalculator"/> does
/// the maths in memory. One person's history is a few thousand sets, so this stays fast.
/// </summary>
public interface IProgressService
{
    Task<ProgressSummaryResponse> GetSummaryAsync(string userId, CancellationToken ct);

    Task<IReadOnlyList<WeeklyVolume>> GetWeeklyVolumeAsync(string userId, int weeks, CancellationToken ct);

    Task<IReadOnlyList<ExerciseProgressRow>> GetExerciseRankingAsync(string userId, ProgressPeriod period, CancellationToken ct);

    /// <summary>Null when the exercise isn't visible to the user (→ 404).</summary>
    Task<ExerciseStatsResponse?> GetExerciseStatsAsync(string userId, int exerciseId, ProgressPeriod period, CancellationToken ct);

    Task<IReadOnlyList<ExerciseRecordsResponse>> GetRecordsAsync(string userId, CancellationToken ct);

    Task<IReadOnlyList<MuscleGroupVolume>> GetMuscleGroupVolumeAsync(string userId, ProgressPeriod period, CancellationToken ct);
}
