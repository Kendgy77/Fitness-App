using System.ComponentModel.DataAnnotations;
using FitnessApp.Api.Common;

namespace FitnessApp.Api.Features.Exercises;

/// <summary>Filters for the library: <c>/api/exercises?search=wyciskanie&amp;category=Chest&amp;page=1</c>.</summary>
public sealed record ExerciseQuery : PageQuery
{
    /// <summary>Part of the name or of the muscles; case and Polish accents are ignored ("lawka" finds "ławka").</summary>
    [StringLength(100, ErrorMessage = "Wyszukiwana fraza może mieć najwyżej 100 znaków.")]
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
    [Required(ErrorMessage = "Podaj nazwę ćwiczenia.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Nazwa musi mieć od 2 do 100 znaków.")]
    string Name,
    [StringLength(2000, ErrorMessage = "Opis może mieć najwyżej 2000 znaków.")]
    string? Description,
    [StringLength(200, ErrorMessage = "Lista mięśni może mieć najwyżej 200 znaków.")]
    string? Muscles,
    [Required(ErrorMessage = "Wybierz kategorię.")]
    ExerciseCategory? Category,
    [Required(ErrorMessage = "Wybierz poziom trudności.")]
    Difficulty? Difficulty,
    [Required(ErrorMessage = "Wybierz sprzęt.")]
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
