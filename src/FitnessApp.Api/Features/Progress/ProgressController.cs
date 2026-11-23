using FitnessApp.Api.Common;
using Microsoft.AspNetCore.Mvc;

namespace FitnessApp.Api.Features.Progress;

/// <summary>Progress statistics (WF07). Read-only and calculated per request from the finished workouts.</summary>
[Route("api/progress")]
public sealed class ProgressController(IProgressService progress) : ApiControllerBase
{
    /// <summary>The dashboard's numbers: workouts this week and month, streak, volume, current weight and BMI.</summary>
    [HttpGet("summary")]
    [ProducesResponseType<ProgressSummaryResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProgressSummaryResponse>> Summary(CancellationToken ct) =>
        Ok(await progress.GetSummaryAsync(CurrentUserId, ct));

    /// <summary>Volume, workouts and working sets per week (Monday to Sunday, user's time zone), oldest first.</summary>
    [HttpGet("volume")]
    [ProducesResponseType<IReadOnlyList<WeeklyVolume>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<WeeklyVolume>>> Volume([FromQuery] VolumeQuery query, CancellationToken ct) =>
        Ok(await progress.GetWeeklyVolumeAsync(CurrentUserId, query.Weeks, ct));

    /// <summary>Every exercise done in the period with workouts, volume, best estimated 1RM and its change.</summary>
    [HttpGet("exercises")]
    [ProducesResponseType<IReadOnlyList<ExerciseProgressRow>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ExerciseProgressRow>>> Exercises([FromQuery] PeriodQuery query, CancellationToken ct) =>
        Ok(await progress.GetExerciseRankingAsync(CurrentUserId, query.Period, ct));

    /// <summary>One exercise in detail: 1RM and top set per workout, rep ranges, workouts per week.</summary>
    [HttpGet("exercises/{exerciseId:int}")]
    [ProducesResponseType<ExerciseStatsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExerciseStatsResponse>> Exercise(int exerciseId, [FromQuery] PeriodQuery query, CancellationToken ct) =>
        await progress.GetExerciseStatsAsync(CurrentUserId, exerciseId, query.Period, ct) is { } stats ? Ok(stats) : NotFound();

    /// <summary>Personal records per exercise: heaviest set, best estimated 1RM, most volume in one workout.</summary>
    [HttpGet("records")]
    [ProducesResponseType<IReadOnlyList<ExerciseRecordsResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ExerciseRecordsResponse>>> Records(CancellationToken ct) =>
        Ok(await progress.GetRecordsAsync(CurrentUserId, ct));

    /// <summary>Working sets and volume per week for each muscle group, averaged over the period.</summary>
    [HttpGet("muscle-groups")]
    [ProducesResponseType<IReadOnlyList<MuscleGroupVolume>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MuscleGroupVolume>>> MuscleGroups([FromQuery] PeriodQuery query, CancellationToken ct) =>
        Ok(await progress.GetMuscleGroupVolumeAsync(CurrentUserId, query.Period, ct));
}
