using FitnessApp.Api.Common.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace FitnessApp.Api.Common;

/// <summary>Base class for every API controller: JSON API conventions plus the current user's id.</summary>
[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// The authenticated user's id. The global fallback policy guarantees the user is authenticated,
    /// so a missing claim is a server misconfiguration, not a client error.
    /// </summary>
    protected string CurrentUserId =>
        User.GetUserId() ?? throw new InvalidOperationException("Authenticated user has no id claim.");
}
