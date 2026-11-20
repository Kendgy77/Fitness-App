using FitnessApp.Api.Common;
using Microsoft.AspNetCore.Mvc;

namespace FitnessApp.Api.Features.Plans;

/// <summary>Training plans (WF05): the user's own plans plus read-only templates they can copy.</summary>
[Route("api/plans")]
public sealed class PlansController(IPlanService plans) : ApiControllerBase
{
    /// <summary>The user's plans, most recently changed first. Filter with <c>?status=Active</c>.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResponse<PlanSummaryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<PlanSummaryResponse>>> List([FromQuery] PlanQuery query, CancellationToken ct) =>
        Ok(await plans.ListAsync(CurrentUserId, query, ct));

    /// <summary>Ready-made plans that ship with the app. Copy one to start from it.</summary>
    [HttpGet("templates")]
    [ProducesResponseType<PagedResponse<PlanSummaryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<PlanSummaryResponse>>> Templates([FromQuery] PageQuery query, CancellationToken ct) =>
        Ok(await plans.ListTemplatesAsync(query, ct));

    [HttpGet("{id:int}")]
    [ProducesResponseType<PlanResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlanResponse>> Get(int id, CancellationToken ct) =>
        await plans.GetAsync(CurrentUserId, id, ct) is { } plan ? Ok(plan) : NotFound();

    /// <summary>Creates a draft plan with the given exercises (thesis T09; without a name → 400, T10).</summary>
    [HttpPost]
    [ProducesResponseType<PlanResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PlanResponse>> Create(PlanRequest request, CancellationToken ct)
    {
        var created = await plans.CreateAsync(CurrentUserId, request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    /// <summary>Saves the whole plan: name, description and the exercises in order.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<PlanResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PlanResponse>> Update(int id, PlanRequest request, CancellationToken ct) =>
        await plans.UpdateAsync(CurrentUserId, id, request, ct) is { } plan ? Ok(plan) : NotFound();

    /// <summary>Activate, archive or restore a plan. Disallowed changes answer 409.</summary>
    [HttpPost("{id:int}/status")]
    [ProducesResponseType<PlanResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PlanResponse>> ChangeStatus(int id, ChangePlanStatusRequest request, CancellationToken ct) =>
        await plans.ChangeStatusAsync(CurrentUserId, id, request.Status!.Value, ct) is { } plan ? Ok(plan) : NotFound();

    /// <summary>Copies a template or one of the user's plans into a new draft.</summary>
    [HttpPost("{id:int}/copy")]
    [ProducesResponseType<PlanResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlanResponse>> Copy(int id, CancellationToken ct) =>
        await plans.CopyAsync(CurrentUserId, id, ct) is { } copy
            ? CreatedAtAction(nameof(Get), new { id = copy.Id }, copy)
            : NotFound();

    /// <summary>Deletes a draft. Plans that were active are archived instead (409).</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) =>
        await plans.DeleteAsync(CurrentUserId, id, ct) ? NoContent() : NotFound();
}
