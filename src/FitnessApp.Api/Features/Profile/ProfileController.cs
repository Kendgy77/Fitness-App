using FitnessApp.Api.Common;
using Microsoft.AspNetCore.Mvc;

namespace FitnessApp.Api.Features.Profile;

/// <summary>The logged-in user's own profile (WF03) with BMI (WF10). There is no id in the URL: it is always "mine".</summary>
[Route("api/profile")]
public sealed class ProfileController(IProfileService profiles) : ApiControllerBase
{
    /// <summary>The profile with age and BMI calculated. Empty fields are null.</summary>
    [HttpGet]
    [ProducesResponseType<ProfileResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProfileResponse>> Get(CancellationToken ct) =>
        Ok(await profiles.GetAsync(CurrentUserId, ct));

    /// <summary>Saves the whole profile. Send null for a field to clear it.</summary>
    [HttpPut]
    [ProducesResponseType<ProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProfileResponse>> Update(UpdateProfileRequest request, CancellationToken ct) =>
        Ok(await profiles.UpdateAsync(CurrentUserId, request, ct));
}
