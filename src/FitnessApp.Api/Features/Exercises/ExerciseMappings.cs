using System.Linq.Expressions;

namespace FitnessApp.Api.Features.Exercises;

public static class ExerciseMappings
{
    /// <summary>
    /// Used inside LINQ queries: EF Core turns it into a SELECT of exactly these columns, so whole entities are
    /// never loaded just to be mapped. Exercises a caller can see are global or their own, so "has an owner"
    /// means "custom".
    /// </summary>
    public static readonly Expression<Func<Exercise, ExerciseResponse>> ToResponseExpression = e => new ExerciseResponse(
        e.Id, e.Name, e.Description, e.Muscles, e.Category, e.Difficulty, e.Equipment, e.UserId != null, e.IsArchived);

    private static readonly Func<Exercise, ExerciseResponse> ToResponseCompiled = ToResponseExpression.Compile();

    /// <summary>The same mapping for an entity already in memory (after a create or an update).</summary>
    public static ExerciseResponse ToResponse(this Exercise exercise) => ToResponseCompiled(exercise);
}
