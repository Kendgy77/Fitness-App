using FitnessApp.Api.Common;
using FitnessApp.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FitnessApp.Api.Features.Auth;

[Route("api/auth")]
public sealed class AuthController(IAuthService auth) : ApiControllerBase
{
    /// <summary>Creates an account and logs the user in (WF01).</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingSetup.AuthPolicy)]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken ct)
    {
        var outcome = await auth.RegisterAsync(request, ct);
        if (outcome.Response is null)
        {
            return ValidationProblem(new ValidationProblemDetails(outcome.Errors)
            {
                Title = "Could not create the account.",
            });
        }

        return CreatedAtAction(nameof(Me), outcome.Response);
    }

    /// <summary>Logs in with e-mail and password (WF02). Three wrong passwords lock the account for 15 minutes.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingSetup.AuthPolicy)]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status423Locked)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var outcome = await auth.LoginAsync(request, ct);
        switch (outcome.Status)
        {
            case LoginStatus.Success:
                return Ok(outcome.Response);

            case LoginStatus.LockedOut:
                var seconds = (int)Math.Ceiling(outcome.RetryAfter!.Value.TotalSeconds);
                Response.Headers.RetryAfter = seconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
                var locked = ProblemDetailsFactory.CreateProblemDetails(HttpContext,
                    statusCode: StatusCodes.Status423Locked,
                    title: "Account locked",
                    detail: "Too many failed login attempts. Try again later.");
                locked.Extensions["retryAfter"] = seconds;
                return StatusCode(StatusCodes.Status423Locked, locked);

            default:
                return Problem(statusCode: StatusCodes.Status401Unauthorized,
                    title: "Invalid email or password.");
        }
    }

    /// <summary>
    /// Logout. Tokens are stateless, so the client logs out by discarding its token; this endpoint exists so
    /// the API has an explicit logout step (and a place for server-side revocation later).
    /// </summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout() => NoContent();

    /// <summary>The logged-in user.</summary>
    [HttpGet("me")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserResponse>> Me(CancellationToken ct) =>
        await auth.GetCurrentUserAsync(CurrentUserId, ct) is { } user ? Ok(user) : NotFound();
}
