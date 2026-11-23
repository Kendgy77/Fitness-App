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

    /// <summary>Each value's newest reading, whichever day it was taken (what the profile shows).</summary>
    Task<IReadOnlyList<CurrentMeasurement>> CurrentAsync(string userId, CancellationToken ct);

    /// <summary>
    /// Saves the profile's measurements: values that differ from the last known ones go into that day's entry
    /// (created, replaced, or removed when nothing differs any more). Returns the new current values.
    /// </summary>
    Task<IReadOnlyList<CurrentMeasurement>> SaveCurrentAsync(string userId, MeasurementRequest request, CancellationToken ct);
}
