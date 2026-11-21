using FitnessApp.Api.Common;
using Microsoft.AspNetCore.Mvc;

namespace FitnessApp.Api.Features.Sessions;

/// <summary>
/// Workouts (WF06): logged live at the gym, or entered afterwards. The set endpoints return the whole workout,
/// so the live screen always shows exactly what was saved.
/// </summary>
[Route("api/sessions")]
public sealed class SessionsController(ISessionService sessions) : ApiControllerBase
{
    /// <summary>Workout history, newest first. Filters: <c>from</c>, <c>to</c>, <c>exerciseId</c>, <c>status</c>.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResponse<SessionSummaryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<SessionSummaryResponse>>> List([FromQuery] SessionQuery query, CancellationToken ct) =>
        Ok(await sessions.ListAsync(CurrentUserId, query, ct));

    [HttpGet("{id:int}")]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SessionResponse>> Get(int id, CancellationToken ct) =>
        await sessions.GetAsync(CurrentUserId, id, ct) is { } session ? Ok(session) : NotFound();

    /// <summary>The workout in progress or paused; 204 when there is none.</summary>
    [HttpGet("active")]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<SessionResponse>> Active(CancellationToken ct) =>
        await sessions.GetActiveAsync(CurrentUserId, ct) is { } session ? Ok(session) : NoContent();

    /// <summary>Plans to start a workout from: the user's active plans and the templates, most recently used first.</summary>
    [HttpGet("start-options")]
    [ProducesResponseType<StartOptionsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<StartOptionsResponse>> StartOptions(CancellationToken ct) =>
        Ok(await sessions.GetStartOptionsAsync(CurrentUserId, ct));

    /// <summary>For the exercise picker: the 10 most used exercises (last 90 days), then the 10 most recent.</summary>
    [HttpGet("/api/exercises/suggestions")]
    [ProducesResponseType<ExerciseSuggestionsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ExerciseSuggestionsResponse>> ExerciseSuggestions(CancellationToken ct) =>
        Ok(await sessions.GetExerciseSuggestionsAsync(CurrentUserId, ct));

    /// <summary>Every finished workout with this exercise and its sets, newest first.</summary>
    [HttpGet("/api/exercises/{exerciseId:int}/history")]
    [ProducesResponseType<PagedResponse<ExerciseHistoryEntry>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResponse<ExerciseHistoryEntry>>> ExerciseHistory(
        int exerciseId, [FromQuery] PageQuery query, CancellationToken ct) =>
        await sessions.GetExerciseHistoryAsync(CurrentUserId, exerciseId, query, ct) is { } history ? Ok(history) : NotFound();

    /// <summary>Saves a workout entered afterwards (thesis T11). Without exercises or sets → 400 (T12).</summary>
    [HttpPost]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SessionResponse>> Log(LogSessionRequest request, CancellationToken ct)
    {
        var created = await sessions.LogAsync(CurrentUserId, request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    /// <summary>Corrects a finished workout: details, exercises and sets are replaced.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SessionResponse>> Update(int id, LogSessionRequest request, CancellationToken ct) =>
        await sessions.UpdateAsync(CurrentUserId, id, request, ct) is { } session ? Ok(session) : NotFound();

    /// <summary>Starts a live workout now, empty or from a plan. Another active workout → 409.</summary>
    [HttpPost("start")]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SessionResponse>> Start(StartSessionRequest request, CancellationToken ct)
    {
        var created = await sessions.StartAsync(CurrentUserId, request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    /// <summary>Starts a planned workout.</summary>
    [HttpPost("{id:int}/start")]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<ActionResult<SessionResponse>> StartPlanned(int id, CancellationToken ct) => Apply(id, SessionAction.Start, null, ct);

    [HttpPost("{id:int}/pause")]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<ActionResult<SessionResponse>> Pause(int id, CancellationToken ct) => Apply(id, SessionAction.Pause, null, ct);

    [HttpPost("{id:int}/resume")]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<ActionResult<SessionResponse>> Resume(int id, CancellationToken ct) => Apply(id, SessionAction.Resume, null, ct);

    /// <summary>
    /// Finishes the workout. Exercises without sets are dropped; with no sets at all → 400 (T12). The duration is
    /// calculated without the pauses unless one is sent.
    /// </summary>
    [HttpPost("{id:int}/finish")]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<ActionResult<SessionResponse>> Finish(int id, FinishSessionRequest? request, CancellationToken ct) =>
        Apply(id, SessionAction.Finish, request, ct);

    [HttpPost("{id:int}/exercises")]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SessionResponse>> AddExercise(int id, AddSessionExerciseRequest request, CancellationToken ct) =>
        await sessions.AddExerciseAsync(CurrentUserId, id, request.ExerciseId, ct) is { } session ? Ok(session) : NotFound();

    [HttpDelete("{id:int}/exercises/{entryId:int}")]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SessionResponse>> RemoveExercise(int id, int entryId, CancellationToken ct) =>
        await sessions.RemoveExerciseAsync(CurrentUserId, id, entryId, ct) is { } session ? Ok(session) : NotFound();

    /// <summary>Logs one set (the fast path at the gym).</summary>
    [HttpPost("{id:int}/exercises/{entryId:int}/sets")]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SessionResponse>> AddSet(int id, int entryId, SetRequest request, CancellationToken ct) =>
        await sessions.AddSetAsync(CurrentUserId, id, entryId, request, ct) is { } session ? Ok(session) : NotFound();

    [HttpPut("{id:int}/exercises/{entryId:int}/sets/{setId:int}")]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SessionResponse>> UpdateSet(int id, int entryId, int setId, SetRequest request, CancellationToken ct) =>
        await sessions.UpdateSetAsync(CurrentUserId, id, entryId, setId, request, ct) is { } session ? Ok(session) : NotFound();

    [HttpDelete("{id:int}/exercises/{entryId:int}/sets/{setId:int}")]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SessionResponse>> DeleteSet(int id, int entryId, int setId, CancellationToken ct) =>
        await sessions.DeleteSetAsync(CurrentUserId, id, entryId, setId, ct) is { } session ? Ok(session) : NotFound();

    /// <summary>Deletes a workout in any status (discarding one is the user's choice).</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) =>
        await sessions.DeleteAsync(CurrentUserId, id, ct) ? NoContent() : NotFound();

    private async Task<ActionResult<SessionResponse>> Apply(int id, SessionAction action, FinishSessionRequest? finish, CancellationToken ct) =>
        await sessions.ApplyAsync(CurrentUserId, id, action, finish, ct) is { } session ? Ok(session) : NotFound();
}
