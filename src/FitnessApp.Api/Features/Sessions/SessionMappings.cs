using System.Linq.Expressions;

namespace FitnessApp.Api.Features.Sessions;

/// <summary>LINQ projections: EF Core turns each into one SQL query that selects only these columns.</summary>
public static class SessionMappings
{
    public static readonly Expression<Func<WorkoutSession, SessionSummaryResponse>> ToSummary = s => new SessionSummaryResponse(
        s.Id,
        s.Name,
        s.Type,
        s.Status,
        s.StartedAtUtc,
        s.ScheduledAtUtc,
        s.DurationMinutes,
        s.Exercises.Count,
        s.Exercises.SelectMany(e => e.Sets).Count(),
        s.Exercises.OrderBy(e => e.Order).Select(e => e.Exercise.Name).Take(4).ToList());

    public static readonly Expression<Func<WorkoutSession, SessionResponse>> ToResponse = s => new SessionResponse(
        s.Id,
        s.Name,
        s.Type,
        s.Status,
        s.PlanId,
        s.ScheduledAtUtc,
        s.StartedAtUtc,
        s.FinishedAtUtc,
        s.PausedAtUtc,
        s.PausedSeconds,
        s.DurationMinutes,
        s.Notes,
        s.Exercises
            .OrderBy(e => e.Order)
            .Select(e => new SessionExerciseResponse(
                e.Id,
                e.Order,
                e.ExerciseId,
                e.Exercise.Name,
                e.Exercise.Category,
                e.Exercise.Equipment,
                e.Notes,
                e.TargetSets,
                e.TargetRepsMin,
                e.TargetRepsMax,
                e.TargetWeightKg,
                e.Sets
                    .OrderBy(x => x.SetNumber)
                    .Select(x => new SetResponse(
                        x.Id, x.SetNumber, x.Type, x.Reps, x.WeightKg, x.Rpe, x.DurationSeconds, x.DistanceMeters, x.CompletedAtUtc))
                    .ToList()))
            .ToList());
}
