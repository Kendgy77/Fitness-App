using FitnessApp.Api.Features.Profile;

namespace FitnessApp.UnitTests.Profile;

public sealed class AgeCalculatorTests
{
    private static readonly DateOnly BirthDate = new(2000, 6, 15);

    [Fact]
    public void Age_increases_on_the_birthday()
    {
        AgeCalculator.AgeOn(BirthDate, new DateOnly(2026, 6, 15)).ShouldBe(26);
    }

    [Fact]
    public void Age_does_not_increase_the_day_before_the_birthday()
    {
        AgeCalculator.AgeOn(BirthDate, new DateOnly(2026, 6, 14)).ShouldBe(25);
    }

    [Fact]
    public void Someone_born_on_29_february_gets_older_on_28_february_in_a_non_leap_year()
    {
        var leapDay = new DateOnly(2004, 2, 29);

        AgeCalculator.AgeOn(leapDay, new DateOnly(2025, 2, 27)).ShouldBe(20);
        AgeCalculator.AgeOn(leapDay, new DateOnly(2025, 2, 28)).ShouldBe(21);
        AgeCalculator.AgeOn(leapDay, new DateOnly(2028, 2, 29)).ShouldBe(24);
    }
}
