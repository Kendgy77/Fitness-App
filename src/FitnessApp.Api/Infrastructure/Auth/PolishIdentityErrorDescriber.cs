using Microsoft.AspNetCore.Identity;

namespace FitnessApp.Api.Infrastructure.Auth;

/// <summary>Identity's validation messages in Polish, so the UI can show them to the user as they are.</summary>
public sealed class PolishIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DuplicateEmail(string email) =>
        new() { Code = nameof(DuplicateEmail), Description = "Konto z tym adresem e-mail już istnieje." };

    // The user name is the e-mail, so a duplicate user name is reported the same way.
    public override IdentityError DuplicateUserName(string userName) =>
        new() { Code = nameof(DuplicateUserName), Description = "Konto z tym adresem e-mail już istnieje." };

    public override IdentityError InvalidEmail(string? email) =>
        new() { Code = nameof(InvalidEmail), Description = "Podaj poprawny adres e-mail." };

    public override IdentityError InvalidUserName(string? userName) =>
        new() { Code = nameof(InvalidUserName), Description = "Adres e-mail zawiera niedozwolone znaki." };

    public override IdentityError PasswordTooShort(int length) =>
        new() { Code = nameof(PasswordTooShort), Description = $"Hasło musi mieć co najmniej {length} znaków." };

    public override IdentityError PasswordRequiresUpper() =>
        new() { Code = nameof(PasswordRequiresUpper), Description = "Hasło musi zawierać wielką literę." };

    public override IdentityError PasswordRequiresLower() =>
        new() { Code = nameof(PasswordRequiresLower), Description = "Hasło musi zawierać małą literę." };

    public override IdentityError PasswordRequiresDigit() =>
        new() { Code = nameof(PasswordRequiresDigit), Description = "Hasło musi zawierać cyfrę." };

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "Hasło musi zawierać znak specjalny." };

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
        new() { Code = nameof(PasswordRequiresUniqueChars), Description = $"Hasło musi zawierać co najmniej {uniqueChars} różnych znaków." };
}
