using FitnessApp.Api.Common;
using Microsoft.AspNetCore.Mvc;

namespace FitnessApp.Api.Features.Exercises;

/// <summary>The exercise library (WF04) as a user sees it: the global catalog plus their own custom exercises.</summary>
[Route("api/exercises")]
public sealed class ExercisesController(IExerciseService exercises) : ApiControllerBase
{
    /// <summary>Search and filter the library, one page at a time (thesis T07, T08).</summary>
    [HttpGet]
    [ProducesResponseType<PagedResponse<ExerciseResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<ExerciseResponse>>> Search([FromQuery] ExerciseQuery query, CancellationToken ct) =>
        Ok(await exercises.SearchAsync(CurrentUserId, query, ct));

    [HttpGet("{id:int}")]
    [ProducesResponseType<ExerciseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExerciseResponse>> Get(int id, CancellationToken ct) =>
        await exercises.GetAsync(CurrentUserId, id, ct) is { } exercise ? Ok(exercise) : NotFound();

    /// <summary>Adds a custom exercise that only this user sees.</summary>
    [HttpPost]
    [ProducesResponseType<ExerciseResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExerciseResponse>> Create(ExerciseRequest request, CancellationToken ct)
    {
        var created = await exercises.CreateCustomAsync(CurrentUserId, request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    /// <summary>Edits one of the user's custom exercises. Global exercises answer 403.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<ExerciseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExerciseResponse>> Update(int id, ExerciseRequest request, CancellationToken ct) =>
        await exercises.UpdateCustomAsync(CurrentUserId, id, request, ct) is { } updated ? Ok(updated) : NotFound();

    /// <summary>Archives (hides) one of the user's custom exercises. Nothing is deleted, so history stays intact.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Archive(int id, CancellationToken ct) =>
        await exercises.ArchiveCustomAsync(CurrentUserId, id, ct) ? NoContent() : NotFound();
}
