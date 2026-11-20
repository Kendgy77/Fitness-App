using FitnessApp.Api.Common;

namespace FitnessApp.Api.Features.Plans;

/// <summary>
/// Training plans (WF05). Every method takes the caller's id; a plan of another user behaves as if it didn't exist
/// (null → 404). Templates are visible to everyone but can only be read and copied (changing one → 403).
/// </summary>
public interface IPlanService
{
    Task<PagedResponse<PlanSummaryResponse>> ListAsync(string userId, PlanQuery query, CancellationToken ct);

    Task<PagedResponse<PlanSummaryResponse>> ListTemplatesAsync(PageQuery query, CancellationToken ct);

    Task<PlanResponse?> GetAsync(string userId, int id, CancellationToken ct);

    /// <summary>A new plan always starts as a draft.</summary>
    Task<PlanResponse> CreateAsync(string userId, PlanRequest request, CancellationToken ct);

    /// <summary>Replaces name, description and the whole exercise list.</summary>
    Task<PlanResponse?> UpdateAsync(string userId, int id, PlanRequest request, CancellationToken ct);

    Task<PlanResponse?> ChangeStatusAsync(string userId, int id, PlanStatus status, CancellationToken ct);

    /// <summary>Copies one of the user's plans or a template into a new draft owned by the user.</summary>
    Task<PlanResponse?> CopyAsync(string userId, int id, CancellationToken ct);

    /// <summary>Only drafts can be deleted; others are archived instead (409).</summary>
    Task<bool> DeleteAsync(string userId, int id, CancellationToken ct);
}
