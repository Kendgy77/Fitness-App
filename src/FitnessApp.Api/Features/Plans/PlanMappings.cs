using System.Linq.Expressions;

namespace FitnessApp.Api.Features.Plans;

/// <summary>LINQ projections: EF Core turns them into one SQL query each, selecting only these columns.</summary>
public static class PlanMappings
{
    public static readonly Expression<Func<TrainingPlan, PlanSummaryResponse>> ToSummary = p => new PlanSummaryResponse(
        p.Id, p.Name, p.Description, p.Status, p.UserId == null, p.Exercises.Count, p.UpdatedAtUtc);

    public static readonly Expression<Func<TrainingPlan, PlanResponse>> ToResponse = p => new PlanResponse(
        p.Id,
        p.Name,
        p.Description,
        p.Status,
        p.UserId == null,
        p.CreatedAtUtc,
        p.UpdatedAtUtc,
        p.Exercises
            .OrderBy(e => e.Order)
            .Select(e => new PlanExerciseResponse(
                e.Order,
                e.ExerciseId,
                e.Exercise.Name,
                e.Exercise.Category,
                e.Exercise.Equipment,
                e.Exercise.IsArchived,
                e.TargetSets,
                e.TargetReps,
                e.TargetWeightKg,
                e.Notes))
            .ToList());
}
