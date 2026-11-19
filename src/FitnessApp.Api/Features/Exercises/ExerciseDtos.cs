using System.ComponentModel.DataAnnotations;
using FitnessApp.Api.Common;

namespace FitnessApp.Api.Features.Exercises;

/// <summary>Filters for the library: <c>/api/exercises?search=wyciskanie&amp;category=Chest&amp;page=1</c>.</summary>
public sealed record ExerciseQuery : PageQuery
{
    /// <summary>Part of the name or of the muscles; case and accents are ignored ("wioslowanie" finds "Wiosłowanie").</summary>
    [StringLength(100, ErrorMessage = "The search text can be at most 100 characters long.")]
    public string? Search { get; init; }

    public ExerciseCategory? Category { get; init; }

    public Difficulty? Difficulty { get; init; }

    public Equipment? Equipment { get; init; }

    /// <summary>True: only the user's own custom exercises.</summary>
    public bool OnlyCustom { get; init; }
}

/// <summary>
/// Create or edit an exercise. The enums are nullable only so that a missing value is a 400 ("choose a category")
/// instead of silently becoming the first enum member.
/// </summary>
public sealed record ExerciseRequest(
    [Required(ErrorMessage = "Enter the exercise name.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "The name must be 2 to 100 characters long.")]
    string Name,
    [StringLength(2000, ErrorMessage = "The description can be at most 2000 characters long.")]
    string? Description,
    [StringLength(200, ErrorMessage = "The muscles list can be at most 200 characters long.")]
    string? Muscles,
    [Required(ErrorMessage = "Choose a category.")]
    ExerciseCategory? Category,
    [Required(ErrorMessage = "Choose a difficulty level.")]
    Difficulty? Difficulty,
    [Required(ErrorMessage = "Choose the equipment.")]
    Equipment? Equipment);

/// <summary><c>IsCustom</c>: the user's own exercise, which they can edit and archive.</summary>
public sealed record ExerciseResponse(
    int Id,
    string Name,
    string Description,
    string Muscles,
    ExerciseCategory Category,
    Difficulty Difficulty,
    Equipment Equipment,
    bool IsCustom,
    bool IsArchived);
