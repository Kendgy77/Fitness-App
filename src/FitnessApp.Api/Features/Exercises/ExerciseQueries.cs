namespace FitnessApp.Api.Features.Exercises;

/// <summary>Query rules about exercises that other features (plans, workouts) share.</summary>
public static class ExerciseQueries
{
    /// <summary>
    /// Exercises a user may add to a plan or a workout: from the shared catalog or their own, and not archived.
    /// Another user's private exercise never qualifies, even if its id is guessed.
    /// </summary>
    public static IQueryable<Exercise> UsableBy(this IQueryable<Exercise> exercises, string userId) =>
        exercises.Where(e => !e.IsArchived && (e.UserId == null || e.UserId == userId));
}
