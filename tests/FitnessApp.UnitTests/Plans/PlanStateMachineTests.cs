using FitnessApp.Api.Features.Plans;
using static FitnessApp.Api.Features.Plans.PlanStatus;

namespace FitnessApp.UnitTests.Plans;

public sealed class PlanStateMachineTests
{
    [Theory]
    [InlineData(Draft, Active)]
    [InlineData(Draft, Archived)]
    [InlineData(Active, Archived)]
    [InlineData(Archived, Draft)]
    public void Allowed_changes_pass(PlanStatus from, PlanStatus to)
    {
        PlanStateMachine.Check(from, to, exerciseCount: 3).ShouldBeNull();
    }

    [Theory]
    [InlineData(Active, Draft)]     // an active plan is edited in place, not sent back to draft
    [InlineData(Archived, Active)]  // restore to draft first, then activate
    public void Other_changes_are_not_allowed(PlanStatus from, PlanStatus to)
    {
        PlanStateMachine.Check(from, to, exerciseCount: 3).ShouldBe(PlanTransitionError.NotAllowed);
    }

    [Theory]
    [InlineData(Draft)]
    [InlineData(Active)]
    [InlineData(Archived)]
    public void Staying_in_the_same_status_is_reported_as_such(PlanStatus status)
    {
        PlanStateMachine.Check(status, status, exerciseCount: 3).ShouldBe(PlanTransitionError.AlreadyInStatus);
    }

    [Fact]
    public void An_empty_plan_can_not_be_activated()
    {
        PlanStateMachine.Check(Draft, Active, exerciseCount: 0).ShouldBe(PlanTransitionError.NoExercises);
    }

    [Fact]
    public void An_empty_plan_can_still_be_archived()
    {
        PlanStateMachine.Check(Draft, Archived, exerciseCount: 0).ShouldBeNull();
    }

    [Theory]
    [InlineData(Draft, true)]
    [InlineData(Active, true)]
    [InlineData(Archived, false)]
    public void Only_archived_plans_are_read_only(PlanStatus status, bool canEdit)
    {
        PlanStateMachine.CanEdit(status).ShouldBe(canEdit);
    }

    [Theory]
    [InlineData(Draft, true)]
    [InlineData(Active, false)]
    [InlineData(Archived, false)]
    public void Only_drafts_can_be_deleted(PlanStatus status, bool canDelete)
    {
        PlanStateMachine.CanDelete(status).ShouldBe(canDelete);
    }
}
