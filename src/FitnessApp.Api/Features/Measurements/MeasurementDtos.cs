using System.ComponentModel.DataAnnotations;
using FitnessApp.Api.Common;
using FitnessApp.Api.Features.Profile;

namespace FitnessApp.Api.Features.Measurements;

/// <summary>
/// One day's measurements. Every value is optional with its own sensible range; at least one must be filled in.
/// </summary>
public sealed record MeasurementRequest(
    [Required(ErrorMessage = "Enter the date.")]
    DateOnly? MeasuredOn,
    [Range(20.0, 400.0, ErrorMessage = "Body weight must be between 20 and 400 kg.")]
    decimal? WeightKg,
    [Range(2.0, 70.0, ErrorMessage = "Body fat must be between 2 and 70%.")]
    decimal? BodyFatPercent,
    [Range(5.0, 200.0, ErrorMessage = "Muscle mass must be between 5 and 200 kg.")]
    decimal? MuscleMassKg,
    [Range(20.0, 80.0, ErrorMessage = "Neck must be between 20 and 80 cm.")]
    decimal? NeckCm,
    [Range(50.0, 200.0, ErrorMessage = "Chest must be between 50 and 200 cm.")]
    decimal? ChestCm,
    [Range(40.0, 200.0, ErrorMessage = "Waist must be between 40 and 200 cm.")]
    decimal? WaistCm,
    [Range(50.0, 200.0, ErrorMessage = "Hips must be between 50 and 200 cm.")]
    decimal? HipsCm,
    [Range(15.0, 80.0, ErrorMessage = "Biceps must be between 15 and 80 cm.")]
    decimal? BicepsLeftCm,
    [Range(15.0, 80.0, ErrorMessage = "Biceps must be between 15 and 80 cm.")]
    decimal? BicepsRightCm,
    [Range(15.0, 60.0, ErrorMessage = "Forearm must be between 15 and 60 cm.")]
    decimal? ForearmCm,
    [Range(25.0, 120.0, ErrorMessage = "Thigh must be between 25 and 120 cm.")]
    decimal? ThighLeftCm,
    [Range(25.0, 120.0, ErrorMessage = "Thigh must be between 25 and 120 cm.")]
    decimal? ThighRightCm,
    [Range(20.0, 80.0, ErrorMessage = "Calf must be between 20 and 80 cm.")]
    decimal? CalfCm,
    [StringLength(200, ErrorMessage = "Notes can be at most 200 characters long.")]
    string? Notes) : IValidatableObject
{
    public bool HasAnyValue =>
        new[]
        {
            WeightKg, BodyFatPercent, MuscleMassKg, NeckCm, ChestCm, WaistCm, HipsCm, BicepsLeftCm, BicepsRightCm,
            ForearmCm, ThighLeftCm, ThighRightCm, CalfCm,
        }.Any(v => v is not null);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!HasAnyValue)
        {
            yield return new ValidationResult("Enter at least one measurement.", [nameof(WeightKg)]);
        }
    }
}

/// <summary><c>/api/measurements?from=2026-01-01&amp;to=2026-12-31</c>: whole days, both ends included.</summary>
public sealed record MeasurementQuery : PageQuery
{
    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }
}

/// <summary>A measurement with the BMI for its weight, from the profile's height (null without either).</summary>
public sealed record MeasurementResponse(
    int Id,
    DateOnly MeasuredOn,
    decimal? WeightKg,
    BmiResult? Bmi,
    decimal? BodyFatPercent,
    decimal? MuscleMassKg,
    decimal? NeckCm,
    decimal? ChestCm,
    decimal? WaistCm,
    decimal? HipsCm,
    decimal? BicepsLeftCm,
    decimal? BicepsRightCm,
    decimal? ForearmCm,
    decimal? ThighLeftCm,
    decimal? ThighRightCm,
    decimal? CalfCm,
    string Notes);

/// <summary>One value's newest reading, e.g. ("waistCm", 84.5, 2026-11-20).</summary>
public sealed record CurrentMeasurement(string Field, decimal Value, DateOnly MeasuredOn);
