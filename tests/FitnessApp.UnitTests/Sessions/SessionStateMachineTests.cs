using FitnessApp.Api.Features.Sessions;
using static FitnessApp.Api.Features.Sessions.SessionAction;
using static FitnessApp.Api.Features.Sessions.SessionStatus;

namespace FitnessApp.UnitTests.Sessions;

public sealed class SessionStateMachineTests
{
    [Theory]
    [InlineData(Planned, Start, InProgress)]
    [InlineData(InProgress, Pause, Paused)]
    [InlineData(Paused, Resume, InProgress)]
    [InlineData(InProgress, Finish, Completed)]
    [InlineData(Paused, Finish, Completed)] // finishing while paused is fine; the pause is left out of the time
    public void Allowed_changes_lead_to_the_next_status(SessionStatus from, SessionAction action, SessionStatus to)
    {
        SessionStateMachine.Next(from, action).ShouldBe(to);
    }

    [Theory]
    [InlineData(Planned, Pause)]
    [InlineData(Planned, Finish)]     // a workout that never started has nothing to finish
    [InlineData(InProgress, Start)]
    [InlineData(InProgress, Resume)]
    [InlineData(Paused, Pause)]
    [InlineData(Completed, Start)]
    [InlineData(Completed, Pause)]
    [InlineData(Completed, Resume)]
    [InlineData(Completed, Finish)]   // finished stays finished
    public void Other_changes_are_not_allowed(SessionStatus from, SessionAction action)
    {
        SessionStateMachine.Next(from, action).ShouldBeNull();
    }

    [Theory]
    [InlineData(Planned, false)]
    [InlineData(InProgress, true)]
    [InlineData(Paused, true)]
    [InlineData(Completed, false)] // corrected with the full form instead
    public void Sets_are_logged_one_by_one_only_during_the_workout(SessionStatus status, bool allowed)
    {
        SessionStateMachine.CanLogSets(status).ShouldBe(allowed);
    }

    [Theory]
    [InlineData(Planned, true)]
    [InlineData(InProgress, true)]
    [InlineData(Paused, true)]
    [InlineData(Completed, false)]
    public void Exercises_can_change_until_the_workout_is_finished(SessionStatus status, bool allowed)
    {
        SessionStateMachine.CanChangeExercises(status).ShouldBe(allowed);
    }

    [Fact]
    public void Duration_leaves_out_the_pauses()
    {
        var start = new DateTimeOffset(2026, 11, 21, 18, 0, 0, TimeSpan.Zero);

        // 70 minutes on the clock, 8 of them paused.
        SessionStateMachine.DurationMinutes(start, start.AddMinutes(70), pausedSeconds: 8 * 60).ShouldBe(62);
    }

    [Theory]
    [InlineData(89, 1)]   // 1.48 minutes rounds down
    [InlineData(90, 2)]   // 1.5 minutes rounds up
    [InlineData(29, 1)]   // a workout is never 0 minutes
    public void Duration_is_rounded_to_whole_minutes_and_at_least_one(int seconds, int minutes)
    {
        var start = new DateTimeOffset(2026, 11, 21, 18, 0, 0, TimeSpan.Zero);

        SessionStateMachine.DurationMinutes(start, start.AddSeconds(seconds), pausedSeconds: 0).ShouldBe(minutes);
    }
}
