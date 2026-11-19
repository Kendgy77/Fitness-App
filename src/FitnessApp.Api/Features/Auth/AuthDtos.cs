using System.ComponentModel.DataAnnotations;

namespace FitnessApp.Api.Features.Auth;

public sealed record RegisterRequest(
    [Required(ErrorMessage = "Enter your email address.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(256, ErrorMessage = "The email address can be at most 256 characters long.")]
    string Email,
    [Required(ErrorMessage = "Enter your password.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "The password must be 8 to 100 characters long.")]
    string Password);

public sealed record LoginRequest(
    [Required(ErrorMessage = "Enter your email address.")]
    [StringLength(256)]
    string Email,
    [Required(ErrorMessage = "Enter your password.")]
    [StringLength(100)]
    string Password);

public sealed record UserResponse(string Id, string Email, IReadOnlyList<string> Roles);

/// <summary>Returned by register and login: the client stores the token and sends it as "Authorization: Bearer …".</summary>
public sealed record AuthResponse(string AccessToken, DateTimeOffset ExpiresAt, UserResponse User);
