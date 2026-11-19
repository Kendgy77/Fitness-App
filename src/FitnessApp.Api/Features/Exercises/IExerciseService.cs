using FitnessApp.Api.Common;

namespace FitnessApp.Api.Features.Exercises;

/// <summary>
/// The exercise library. Methods for users take the caller's id and only ever touch exercises that user can
/// see (global + own). "Global" methods are for the administrator and only touch the shared catalog.
/// Methods return null when the exercise doesn't exist or isn't visible: the controller turns that into 404.
/// </summary>
public interface IExerciseService
{
    Task<PagedResponse<ExerciseResponse>> SearchAsync(string userId, ExerciseQuery query, CancellationToken ct);

    /// <summary>Also returns archived exercises, so old workouts can still show them.</summary>
    Task<ExerciseResponse?> GetAsync(string userId, int id, CancellationToken ct);

    Task<ExerciseResponse> CreateCustomAsync(string userId, ExerciseRequest request, CancellationToken ct);

    /// <summary>Throws <c>ForbiddenException</c> for a global exercise: users can see it but not change it.</summary>
    Task<ExerciseResponse?> UpdateCustomAsync(string userId, int id, ExerciseRequest request, CancellationToken ct);

    Task<bool> ArchiveCustomAsync(string userId, int id, CancellationToken ct);

    Task<ExerciseResponse> CreateGlobalAsync(ExerciseRequest request, CancellationToken ct);

    Task<ExerciseResponse?> UpdateGlobalAsync(int id, ExerciseRequest request, CancellationToken ct);

    Task<bool> ArchiveGlobalAsync(int id, CancellationToken ct);
}
