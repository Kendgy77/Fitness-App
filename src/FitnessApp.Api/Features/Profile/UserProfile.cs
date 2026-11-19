namespace FitnessApp.Api.Features.Profile;

/// <summary>
/// Health and fitness data of one user (thesis class "ProfilUzytkownika", WF03). One row per user, created on the
/// first save. Every field is optional: the user fills in what they want, and features that need a missing value
/// (BMI needs weight and height) say so instead of failing.
/// </summary>
public sealed class UserProfile
{
    /// <summary>Primary key and foreign key to the user at the same time: that is what makes the relation 1:1.</summary>
    public string UserId { get; set; } = null!;

    /// <summary>Stored instead of "age", which would go stale every birthday. Age is calculated when read.</summary>
    public DateOnly? BirthDate { get; set; }

    public Sex? Sex { get; set; }

    public decimal? HeightCm { get; set; }

    /// <summary>Current body weight. The weight history (thesis class "Postep") is a separate feature in week 5.</summary>
    public decimal? WeightKg { get; set; }

    public TrainingGoal? Goal { get; set; }

    /// <summary>
    /// IANA id such as "Europe/Warsaw"; decides which calendar day a workout belongs to and when reminders fire.
    /// The web app fills it in from the browser, so the user never has to pick it (D021).
    /// </summary>
    public string TimeZone { get; set; } = DefaultTimeZone;

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public const string DefaultTimeZone = "Europe/Warsaw";
}

public enum Sex
{
    Female,
    Male,
    Other,
}

/// <summary>The thesis (WF03, WF08) names weight loss, strength and endurance; the rest cover common goals.</summary>
public enum TrainingGoal
{
    LoseWeight,
    BuildMuscle,
    IncreaseStrength,
    ImproveEndurance,
    StayHealthy,
}
