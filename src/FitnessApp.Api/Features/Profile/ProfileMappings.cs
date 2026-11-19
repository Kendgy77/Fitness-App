namespace FitnessApp.Api.Features.Profile;

public static class ProfileMappings
{
    /// <summary>What a user who has never saved their profile sees: everything empty, default time zone.</summary>
    public static ProfileResponse Empty { get; } = new(
        BirthDate: null,
        Age: null,
        Sex: null,
        HeightCm: null,
        WeightKg: null,
        Goal: null,
        TimeZone: UserProfile.DefaultTimeZone,
        Bmi: null,
        MissingForBmi: MissingForBmi(null, null),
        UpdatedAtUtc: null);

    /// <summary>Age and BMI are derived here, from stored data, every time the profile is read.</summary>
    public static ProfileResponse ToResponse(this UserProfile profile, DateOnly today) => new(
        profile.BirthDate,
        profile.BirthDate is { } birthDate ? AgeCalculator.AgeOn(birthDate, today) : null,
        profile.Sex,
        profile.HeightCm,
        profile.WeightKg,
        profile.Goal,
        profile.TimeZone,
        BmiCalculator.TryCalculate(profile.WeightKg, profile.HeightCm),
        MissingForBmi(profile.WeightKg, profile.HeightCm),
        profile.UpdatedAtUtc);

    // Names match the JSON field names, so the UI can highlight the right input.
    private static List<string> MissingForBmi(decimal? weightKg, decimal? heightCm)
    {
        var missing = new List<string>(2);
        if (weightKg is null)
        {
            missing.Add("weightKg");
        }

        if (heightCm is null)
        {
            missing.Add("heightCm");
        }

        return missing;
    }
}
