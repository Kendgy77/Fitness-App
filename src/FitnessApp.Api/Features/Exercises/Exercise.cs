namespace FitnessApp.Api.Features.Exercises;

/// <summary>
/// One exercise in the library (thesis class "Cwiczenie", WF04). Global exercises (<see cref="UserId"/> is null)
/// are managed by the administrator and visible to everyone; a user's custom exercises are visible only to them.
/// </summary>
public sealed class Exercise
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    /// <summary>How to perform it: the technique (WF04 "opis techniki wykonania").</summary>
    public string Description { get; set; } = "";

    /// <summary>Muscles involved, as readable text (WF04 "muscles involved").</summary>
    public string Muscles { get; set; } = "";

    /// <summary>Main body part; what the library is filtered by (thesis T08).</summary>
    public ExerciseCategory Category { get; set; }

    public Difficulty Difficulty { get; set; }

    public Equipment Equipment { get; set; }

    /// <summary>Owner of a custom exercise; null for the global catalog.</summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Exercises are never deleted, only archived: plans and workout history will point at them, and history
    /// must not change. An archived exercise disappears from the library but still shows in old workouts.
    /// </summary>
    public bool IsArchived { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}

public enum ExerciseCategory
{
    Chest,
    Back,
    Shoulders,
    Arms,
    Legs,
    Core,
    Cardio,
}

public enum Difficulty
{
    Beginner,
    Intermediate,
    Advanced,
}

public enum Equipment
{
    Barbell,
    Dumbbell,
    Kettlebell,
    Machine,
    Cable,
    Bodyweight,
    Other,
}
