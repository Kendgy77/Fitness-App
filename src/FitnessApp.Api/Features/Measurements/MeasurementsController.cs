using FitnessApp.Api.Common;
using Microsoft.AspNetCore.Mvc;

namespace FitnessApp.Api.Features.Measurements;

/// <summary>Body measurements over time (WF07): weight, body fat, muscle mass and tape measurements.</summary>
[Route("api/measurements")]
public sealed class MeasurementsController(IMeasurementService measurements) : ApiControllerBase
{
    /// <summary>Newest day first; each entry with the BMI for its weight. Filters: <c>from</c>, <c>to</c> (dates).</summary>
    [HttpGet]
    [ProducesResponseType<PagedResponse<MeasurementResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<MeasurementResponse>>> List([FromQuery] MeasurementQuery query, CancellationToken ct) =>
        Ok(await measurements.ListAsync(CurrentUserId, query, ct));

    [HttpGet("{id:int}")]
    [ProducesResponseType<MeasurementResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MeasurementResponse>> Get(int id, CancellationToken ct) =>
        await measurements.GetAsync(CurrentUserId, id, ct) is { } m ? Ok(m) : NotFound();

    /// <summary>Saves a day's measurements. A newer weight also becomes the profile's current weight.</summary>
    [HttpPost]
    [ProducesResponseType<MeasurementResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MeasurementResponse>> Create(MeasurementRequest request, CancellationToken ct)
    {
        var created = await measurements.CreateAsync(CurrentUserId, request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<MeasurementResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MeasurementResponse>> Update(int id, MeasurementRequest request, CancellationToken ct) =>
        await measurements.UpdateAsync(CurrentUserId, id, request, ct) is { } m ? Ok(m) : NotFound();

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) =>
        await measurements.DeleteAsync(CurrentUserId, id, ct) ? NoContent() : NotFound();
}
