namespace FitnessApp.Api.Features.Auth;

/// <summary>Result of a registration: either a logged-in user, or validation errors per field.</summary>
public sealed record RegisterOutcome(AuthResponse? Response, IDictionary<string, string[]> Errors)
{
    public static RegisterOutcome Success(AuthResponse response) => new(response, new Dictionary<string, string[]>());
    public static RegisterOutcome Failed(IDictionary<string, string[]> errors) => new(null, errors);
}

public enum LoginStatus
{
    Success,
    InvalidCredentials,
    LockedOut,
}

/// <summary>Result of a login attempt. RetryAfter is set only when the account is locked.</summary>
public sealed record LoginOutcome(LoginStatus Status, AuthResponse? Response = null, TimeSpan? RetryAfter = null);
