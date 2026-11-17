using Microsoft.AspNetCore.Identity;

namespace FitnessApp.Api.Features.Auth;

/// <summary>
/// A registered account (thesis class "Uzytkownik"). Identity provides e-mail, password hash, lockout and
/// security stamp; an administrator is the same entity with the Admin role (the ERD's "rola" column).
/// </summary>
public sealed class AppUser : IdentityUser
{
    public DateTimeOffset CreatedAtUtc { get; set; }
}
