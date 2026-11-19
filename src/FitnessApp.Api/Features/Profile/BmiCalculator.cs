namespace FitnessApp.Api.Features.Profile;

/// <summary>
/// Body Mass Index (WF10): weight in kg divided by height in metres squared, with the WHO adult categories.
/// Pure logic with no dependencies, so it is unit-tested on its own (thesis class ProfilUzytkownika "obliczBMI").
/// </summary>
public static class BmiCalculator
{
    /// <summary>BMI rounded to one decimal place, e.g. 78 kg and 183 cm give 23.3 (thesis T16).</summary>
    public static decimal Calculate(decimal weightKg, decimal heightCm)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(weightKg);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(heightCm);

        var heightM = heightCm / 100m;
        return Math.Round(weightKg / (heightM * heightM), 1, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// WHO categories. The rounded value is categorised, so the number and the label the user sees always agree
    /// (24.96 is shown as 25.0, so it must also be labelled "overweight").
    /// </summary>
    public static BmiCategory Categorize(decimal bmi) => bmi switch
    {
        < 18.5m => BmiCategory.Underweight,
        < 25m => BmiCategory.Normal,
        < 30m => BmiCategory.Overweight,
        _ => BmiCategory.Obese,
    };

    /// <summary>The BMI when both values are known, otherwise null (thesis T17: BMI is not calculated without height).</summary>
    public static BmiResult? TryCalculate(decimal? weightKg, decimal? heightCm)
    {
        if (weightKg is not > 0 || heightCm is not > 0)
        {
            return null;
        }

        var value = Calculate(weightKg.Value, heightCm.Value);
        return new BmiResult(value, Categorize(value));
    }
}

public sealed record BmiResult(decimal Value, BmiCategory Category);

public enum BmiCategory
{
    Underweight,
    Normal,
    Overweight,
    Obese,
}
