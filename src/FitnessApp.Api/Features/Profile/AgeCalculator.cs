namespace FitnessApp.Api.Features.Profile;

/// <summary>Age in full years on a given day. The day is passed in, so the result never depends on the real clock.</summary>
public static class AgeCalculator
{
    public static int AgeOn(DateOnly birthDate, DateOnly today)
    {
        var age = today.Year - birthDate.Year;

        // Birthday not reached yet this year. AddYears maps 29 Feb to 28 Feb, so someone born
        // on 29 Feb turns a year older on 28 Feb in non-leap years.
        if (today < birthDate.AddYears(age))
        {
            age--;
        }

        return age;
    }
}
