namespace FitnessApp.Api.Features.Profile;

public interface IProfileService
{
    /// <summary>The user's profile; an empty one if they have never saved it (a GET never creates rows).</summary>
    Task<ProfileResponse> GetAsync(string userId, CancellationToken ct);

    /// <summary>Creates the profile on the first save and replaces it on later saves.</summary>
    Task<ProfileResponse> UpdateAsync(string userId, UpdateProfileRequest request, CancellationToken ct);
}
