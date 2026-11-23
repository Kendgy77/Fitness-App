using FitnessApp.Api.Features.Profile;

namespace FitnessApp.Api.Common;

/// <summary>
/// Calendar days and weeks in the user's own time zone. A workout at 00:30 in Warsaw is on Tuesday there, even
/// though it is still Monday in UTC, and "this week" starts on the user's Monday.
/// </summary>
public static class UserTime
{
    /// <summary>The user's time zone, or the default one when the profile has none (or an unknown id).</summary>
    public static TimeZoneInfo Resolve(string? timeZoneId) =>
        timeZoneId is not null && TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var zone)
            ? zone
            : TimeZoneInfo.FindSystemTimeZoneById(UserProfile.DefaultTimeZone);

    public static DateOnly LocalDate(DateTimeOffset moment, TimeZoneInfo zone) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(moment, zone).DateTime);

    /// <summary>The Monday of the week the day is in (weeks start on Monday, as in Poland).</summary>
    public static DateOnly WeekStart(DateOnly day) => day.AddDays(-(((int)day.DayOfWeek + 6) % 7));
}
