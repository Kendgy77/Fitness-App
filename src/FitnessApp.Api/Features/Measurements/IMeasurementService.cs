using FitnessApp.Api.Common;

namespace FitnessApp.Api.Features.Measurements;

/// <summary>Body measurements (WF07). Another user's entry behaves as if it didn't exist (null → 404).</summary>
public interface IMeasurementService
{
    /// <summary>Newest day first, optionally within a date range.</summary>
    Task<PagedResponse<MeasurementResponse>> ListAsync(string userId, MeasurementQuery query, CancellationToken ct);

    Task<MeasurementResponse?> GetAsync(string userId, int id, CancellationToken ct);

    /// <summary>One entry per day: a second one for the same day → 409. A newer weight updates the profile.</summary>
    Task<MeasurementResponse> CreateAsync(string userId, MeasurementRequest request, CancellationToken ct);

    Task<MeasurementResponse?> UpdateAsync(string userId, int id, MeasurementRequest request, CancellationToken ct);

    Task<bool> DeleteAsync(string userId, int id, CancellationToken ct);
}
