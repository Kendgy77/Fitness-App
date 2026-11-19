using FitnessApp.Api.Features.Profile;

namespace FitnessApp.UnitTests.Profile;

/// <summary>WF10. Test IDs refer to the thesis RTM.</summary>
public sealed class BmiCalculatorTests
{
    [Fact]
    public void T16_bmi_for_78_kg_and_183_cm_is_23_3()
    {
        BmiCalculator.Calculate(78m, 183m).ShouldBe(23.3m);
    }

    [Theory]
    [InlineData(70, 175, 22.9)]
    [InlineData(50, 160, 19.5)]
    [InlineData(120, 180, 37.0)]
    [InlineData(64.8, 180, 20.0)] // 20.0 exactly
    public void Bmi_is_weight_divided_by_height_in_metres_squared_rounded_to_one_decimal(
        decimal weightKg, decimal heightCm, decimal expected)
    {
        BmiCalculator.Calculate(weightKg, heightCm).ShouldBe(expected);
    }

    [Theory]
    [InlineData(18.4, BmiCategory.Underweight)]
    [InlineData(18.5, BmiCategory.Normal)]
    [InlineData(24.9, BmiCategory.Normal)]
    [InlineData(25.0, BmiCategory.Overweight)]
    [InlineData(29.9, BmiCategory.Overweight)]
    [InlineData(30.0, BmiCategory.Obese)]
    public void Categories_follow_the_who_boundaries(decimal bmi, BmiCategory expected)
    {
        BmiCalculator.Categorize(bmi).ShouldBe(expected);
    }

    [Fact]
    public void T17_no_bmi_without_height()
    {
        BmiCalculator.TryCalculate(weightKg: 78m, heightCm: null).ShouldBeNull();
    }

    [Fact]
    public void No_bmi_without_weight()
    {
        BmiCalculator.TryCalculate(weightKg: null, heightCm: 183m).ShouldBeNull();
    }

    [Fact]
    public void TryCalculate_returns_value_and_category_together()
    {
        BmiCalculator.TryCalculate(78m, 183m).ShouldBe(new BmiResult(23.3m, BmiCategory.Normal));
    }

    [Fact]
    public void Category_is_taken_from_the_rounded_value_so_label_and_number_agree()
    {
        // 81 kg / 1.80 m² = 24.999…, shown as 25.0, so it must be labelled overweight too.
        BmiCalculator.TryCalculate(81m, 180m).ShouldBe(new BmiResult(25.0m, BmiCategory.Overweight));
    }

    [Theory]
    [InlineData(0, 180)]
    [InlineData(-5, 180)]
    [InlineData(80, 0)]
    public void Calculate_rejects_non_positive_inputs(decimal weightKg, decimal heightCm)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => BmiCalculator.Calculate(weightKg, heightCm));
    }
}
