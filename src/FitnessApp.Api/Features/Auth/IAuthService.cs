namespace FitnessApp.Api.Features.Auth;

public interface IAuthService
{
    Task<RegisterOutcome> RegisterAsync(RegisterRequest request, CancellationToken ct);

    Task<LoginOutcome> LoginAsync(LoginRequest request, CancellationToken ct);

    Task<UserResponse?> GetCurrentUserAsync(string userId, CancellationToken ct);
}
