using FitnessApp.Api.Features.Plans;

namespace FitnessApp.Api.Infrastructure.Data.Seed;

/// <summary>
/// Ready-made plans every user can copy (seeded by migration, like the exercise catalog). Exercise ids refer to
/// <see cref="ExerciseCatalog"/>. Ids below 1000 are reserved for seeded rows.
/// </summary>
public static class PlanTemplates
{
    public const int FirstRuntimeId = 1000;

    private static readonly DateTimeOffset SeededAt = new(2026, 11, 20, 0, 0, 0, TimeSpan.Zero);

    public static IReadOnlyList<TrainingPlan> Plans { get; } =
    [
        Plan(1, "Full Body – Beginner",
            "Three times a week, with a rest day in between. Every main muscle group in one session, with simple "
            + "exercises to learn the technique. Add weight when all sets feel easy."),
        Plan(2, "Upper Body – Intermediate",
            "Chest, back, shoulders and arms. Pair it with \"Lower Body – Intermediate\" for a four-day "
            + "upper/lower split. Rest 2–3 minutes between heavy sets."),
        Plan(3, "Lower Body – Intermediate",
            "Legs, glutes and core. Pair it with \"Upper Body – Intermediate\". Warm up with a few light sets "
            + "of squats before the working sets."),
    ];

    public static IReadOnlyList<PlanExercise> Exercises { get; } =
    [
        // Full Body – Beginner: one wide range everywhere, so the rule "add weight at the top" is easy to learn.
        .. Rows(1,
            (38, 3, 10, 12), // Goblet Squat
            (7, 3, 8, 12),   // Push-Up
            (14, 3, 10, 12), // One-Arm Dumbbell Row
            (21, 3, 10, 12), // Seated Dumbbell Shoulder Press
            (43, 3, 10, 12), // Lying Leg Curl
            (56, 3, 10, 10)), // Dead Bug (per side)

        // Upper Body – Intermediate: heavy compound lifts first in low ranges, accessories higher.
        .. Rows(2,
            (1, 4, 6, 8),    // Barbell Bench Press
            (13, 4, 6, 8),   // Bent-Over Barbell Row
            (20, 3, 6, 8),   // Overhead Press
            (15, 3, 8, 10),  // Lat Pulldown
            (27, 3, 8, 12),  // Barbell Curl
            (32, 3, 10, 12)), // Triceps Pushdown

        // Lower Body – Intermediate
        .. Rows(3,
            (36, 4, 5, 6),   // Barbell Back Squat
            (42, 3, 6, 8),   // Romanian Deadlift
            (41, 3, 8, 10),  // Bulgarian Split Squat (per leg)
            (39, 3, 10, 12), // Leg Press
            (46, 4, 12, 15), // Standing Calf Raise
            (52, 3, 8, 12)), // Hanging Leg Raise
    ];

    private static TrainingPlan Plan(int id, string name, string description) => new()
    {
        Id = id,
        UserId = null,
        Name = name,
        Description = description,
        Status = PlanStatus.Active,
        CreatedAtUtc = SeededAt,
        UpdatedAtUtc = SeededAt,
    };

    private static IEnumerable<PlanExercise> Rows(int planId, params (int ExerciseId, int Sets, int RepsMin, int RepsMax)[] rows) =>
        rows.Select((row, index) => new PlanExercise
        {
            // 101, 102, … for plan 1; 201, … for plan 2: fixed and readable, below the runtime range.
            Id = (planId * 100) + index + 1,
            TrainingPlanId = planId,
            ExerciseId = row.ExerciseId,
            Order = index + 1,
            TargetSets = row.Sets,
            TargetRepsMin = row.RepsMin,
            TargetRepsMax = row.RepsMax,
            TargetWeightKg = null,
            Notes = "",
        });
}
