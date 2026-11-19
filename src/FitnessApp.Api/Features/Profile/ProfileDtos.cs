using System.ComponentModel.DataAnnotations;

namespace FitnessApp.Api.Features.Profile;

/// <summary>
/// Saves the whole profile (PUT replaces it). Every field may be null: "not filled in". Value ranges are checked
/// here; rules that need today's date (birth date) are checked in the service.
/// </summary>
public sealed record UpdateProfileRequest(
    DateOnly? BirthDate,
    Sex? Sex,
    [Range(100.0, 250.0, ErrorMessage = "Height must be between 100 and 250 cm.")]
    decimal? HeightCm,
    [Range(20.0, 400.0, ErrorMessage = "Body weight must be between 20 and 400 kg.")]
    decimal? WeightKg,
    TrainingGoal? Goal,
    [StringLength(64, ErrorMessage = "Unknown time zone.")]
    string? TimeZone);

/// <summary>
/// The profile as stored, plus derived values: <c>Age</c> from the birth date and <c>Bmi</c> from weight and height.
/// <c>Bmi</c> is null when weight or height is missing, and <c>MissingForBmi</c> then names the fields the user
/// still has to fill in (thesis T17). <c>UpdatedAtUtc</c> is null until the profile is saved for the first time.
/// </summary>
public sealed record ProfileResponse(
    DateOnly? BirthDate,
    int? Age,
    Sex? Sex,
    decimal? HeightCm,
    decimal? WeightKg,
    TrainingGoal? Goal,
    string TimeZone,
    BmiResult? Bmi,
    IReadOnlyList<string> MissingForBmi,
    DateTimeOffset? UpdatedAtUtc);
