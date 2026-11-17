using System.ComponentModel.DataAnnotations;

namespace FitnessApp.Api.Infrastructure.Auth;

/// <summary>
/// JWT settings, validated at startup: a missing or short key stops the app immediately instead of failing
/// on the first login. The key comes from user-secrets locally and an environment variable in production.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required(ErrorMessage = "Jwt:Key is not set. Run: dotnet user-secrets set \"Jwt:Key\" \"<32+ random characters>\" --project src/FitnessApp.Api")]
    [MinLength(32, ErrorMessage = "Jwt:Key must be at least 32 characters (256 bits for HMAC-SHA256).")]
    public string Key { get; init; } = "";

    [Required]
    public string Issuer { get; init; } = "";

    [Required]
    public string Audience { get; init; } = "";

    [Range(5, 24 * 60)]
    public int ExpiryMinutes { get; init; } = 60;
}
