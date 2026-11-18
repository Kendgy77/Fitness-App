using FitnessApp.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Identity;

namespace FitnessApp.Api.Features.Auth;

/// <summary>
/// Registration and login on top of ASP.NET Core Identity. Identity does the security-critical parts
/// (password hashing, lockout counting); this class decides what the API answers.
/// </summary>
public sealed class AuthService(
    UserManager<AppUser> users,
    SignInManager<AppUser> signIn,
    IJwtTokenService tokens,
    TimeProvider clock) : IAuthService
{
    public async Task<RegisterOutcome> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim();
        var user = new AppUser
        {
            UserName = email,
            Email = email,
            CreatedAtUtc = clock.GetUtcNow(),
        };

        var created = await users.CreateAsync(user, request.Password);
        if (!created.Succeeded)
        {
            return RegisterOutcome.Failed(ToFieldErrors(created.Errors));
        }

        await users.AddToRoleAsync(user, Roles.User);

        // Thesis T01: after registering, the user is logged in straight away.
        return RegisterOutcome.Success(await IssueTokenAsync(user));
    }

    public async Task<LoginOutcome> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(request.Email.Trim());
        if (user is null)
        {
            // Same answer as a wrong password, so nobody can probe which e-mails are registered.
            return new LoginOutcome(LoginStatus.InvalidCredentials);
        }

        // lockoutOnFailure: true makes Identity count failures and lock the account after
        // MaxFailedAccessAttempts (3). While locked, even the correct password is refused.
        var result = await signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            var lockoutEnd = await users.GetLockoutEndDateAsync(user) ?? clock.GetUtcNow();
            var retryAfter = lockoutEnd - clock.GetUtcNow();
            return new LoginOutcome(LoginStatus.LockedOut, RetryAfter: retryAfter > TimeSpan.Zero ? retryAfter : TimeSpan.Zero);
        }

        if (!result.Succeeded)
        {
            return new LoginOutcome(LoginStatus.InvalidCredentials);
        }

        return new LoginOutcome(LoginStatus.Success, await IssueTokenAsync(user));
    }

    public async Task<UserResponse?> GetCurrentUserAsync(string userId, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(userId);
        if (user is null)
        {
            return null;
        }

        var roles = await users.GetRolesAsync(user);
        return new UserResponse(user.Id, user.Email!, [.. roles]);
    }

    private async Task<AuthResponse> IssueTokenAsync(AppUser user)
    {
        var roles = (await users.GetRolesAsync(user)).ToList();
        var token = tokens.CreateToken(user.Id, user.Email!, roles);
        return new AuthResponse(token.Value, token.ExpiresAt, new UserResponse(user.Id, user.Email!, roles));
    }

    /// <summary>Groups Identity's errors by the form field they belong to, so the UI can show them inline.</summary>
    private static Dictionary<string, string[]> ToFieldErrors(IEnumerable<IdentityError> errors) =>
        errors
            .GroupBy(e => e.Code.StartsWith("Password", StringComparison.Ordinal) ? "password" : "email")
            .ToDictionary(g => g.Key, g => g.Select(e => e.Description).Distinct().ToArray());
}
