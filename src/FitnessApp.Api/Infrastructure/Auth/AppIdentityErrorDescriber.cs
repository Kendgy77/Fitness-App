using Microsoft.AspNetCore.Identity;

namespace FitnessApp.Api.Infrastructure.Auth;

/// <summary>Identity's validation messages, worded for the end user so the UI can show them as they are. Registering uses the e-mail as the user name, so both duplicate checks give the same message.</summary>
public sealed class AppIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DuplicateEmail(string email) =>
        new() { Code = nameof(DuplicateEmail), Description = "An account with this email already exists." };

    // The user name is the e-mail, so a duplicate user name is reported the same way.
    public override IdentityError DuplicateUserName(string userName) =>
        new() { Code = nameof(DuplicateUserName), Description = "An account with this email already exists." };

    public override IdentityError InvalidEmail(string? email) =>
        new() { Code = nameof(InvalidEmail), Description = "Enter a valid email address." };

    public override IdentityError InvalidUserName(string? userName) =>
        new() { Code = nameof(InvalidUserName), Description = "Adres e-mail zawiera niedozwolone znaki." };

    public override IdentityError PasswordTooShort(int length) =>
        new() { Code = nameof(PasswordTooShort), Description = $"The password must be at least {length} characters long." };

    public override IdentityError PasswordRequiresUpper() =>
        new() { Code = nameof(PasswordRequiresUpper), Description = "The password must contain an upper-case letter." };

    public override IdentityError PasswordRequiresLower() =>
        new() { Code = nameof(PasswordRequiresLower), Description = "The password must contain a lower-case letter." };

    public override IdentityError PasswordRequiresDigit() =>
        new() { Code = nameof(PasswordRequiresDigit), Description = "The password must contain a digit." };

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "The password must contain a special character." };

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
        new() { Code = nameof(PasswordRequiresUniqueChars), Description = $"The password must contain at least {uniqueChars} different characters." };
}
