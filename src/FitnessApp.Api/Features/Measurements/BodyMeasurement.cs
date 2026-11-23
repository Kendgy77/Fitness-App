namespace FitnessApp.Api.Features.Measurements;

/// <summary>
/// Body measurements on one day (thesis class "Postep", WF07): weight, and optionally body fat, muscle mass (from
/// a smart scale or an InBody test) and tape measurements. Every value is optional, but an entry has at least one.
/// One entry per user and day: measuring again the same day corrects that entry.
/// </summary>
public sealed class BodyMeasurement
{
    public int Id { get; set; }

    public string UserId { get; set; } = null!;

    /// <summary>The user's own calendar day (their time zone), not a moment: "measured on 21 Nov".</summary>
    public DateOnly MeasuredOn { get; set; }

    public decimal? WeightKg { get; set; }

    public decimal? BodyFatPercent { get; set; }

    public decimal? MuscleMassKg { get; set; }

    // Circumferences in centimetres. Arms and legs are measured on one side; biceps and thigh have both sides,
    // because a difference between left and right is worth seeing.
    public decimal? NeckCm { get; set; }

    public decimal? ChestCm { get; set; }

    public decimal? WaistCm { get; set; }

    public decimal? HipsCm { get; set; }

    public decimal? BicepsLeftCm { get; set; }

    public decimal? BicepsRightCm { get; set; }

    public decimal? ForearmCm { get; set; }

    public decimal? ThighLeftCm { get; set; }

    public decimal? ThighRightCm { get; set; }

    public decimal? CalfCm { get; set; }

    public string Notes { get; set; } = "";

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}
