using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace FitnessApp.Api.Common.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>The user id from the token's "sub" claim, or null for an anonymous user.</summary>
    public static string? GetUserId(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
}
