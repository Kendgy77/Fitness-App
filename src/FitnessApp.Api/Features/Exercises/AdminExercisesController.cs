using FitnessApp.Api.Common;
using FitnessApp.Api.Features.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitnessApp.Api.Features.Exercises;

/// <summary>
/// The administrator manages the global catalog that every user sees (thesis: Administrator manages the exercise
/// library). Reading the catalog uses the normal <c>/api/exercises</c> endpoints.
/// </summary>
[Route("api/admin/exercises")]
[Authorize(Roles = Roles.Admin)]
public sealed class AdminExercisesController(IExerciseService exercises) : ApiControllerBase
{
    [HttpPost]
    [ProducesResponseType<ExerciseResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExerciseResponse>> Create(ExerciseRequest request, CancellationToken ct)
    {
        var created = await exercises.CreateGlobalAsync(request, ct);
        return CreatedAtAction(nameof(ExercisesController.Get), "Exercises", new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<ExerciseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExerciseResponse>> Update(int id, ExerciseRequest request, CancellationToken ct) =>
        await exercises.UpdateGlobalAsync(id, request, ct) is { } updated ? Ok(updated) : NotFound();

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Archive(int id, CancellationToken ct) =>
        await exercises.ArchiveGlobalAsync(id, ct) ? NoContent() : NotFound();
}
