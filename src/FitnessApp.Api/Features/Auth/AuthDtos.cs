using System.ComponentModel.DataAnnotations;

namespace FitnessApp.Api.Features.Auth;

public sealed record RegisterRequest(
    [Required(ErrorMessage = "Podaj adres e-mail.")]
    [EmailAddress(ErrorMessage = "Podaj poprawny adres e-mail.")]
    [StringLength(256, ErrorMessage = "Adres e-mail może mieć najwyżej 256 znaków.")]
    string Email,
    [Required(ErrorMessage = "Podaj hasło.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Hasło musi mieć od 8 do 100 znaków.")]
    string Password);

public sealed record LoginRequest(
    [Required(ErrorMessage = "Podaj adres e-mail.")]
    [StringLength(256)]
    string Email,
    [Required(ErrorMessage = "Podaj hasło.")]
    [StringLength(100)]
    string Password);

public sealed record UserResponse(string Id, string Email, IReadOnlyList<string> Roles);

/// <summary>Returned by register and login: the client stores the token and sends it as "Authorization: Bearer …".</summary>
public sealed record AuthResponse(string AccessToken, DateTimeOffset ExpiresAt, UserResponse User);
