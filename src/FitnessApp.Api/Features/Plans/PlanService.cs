using FitnessApp.Api.Common;
using FitnessApp.Api.Common.Errors;
using FitnessApp.Api.Features.Exercises;
using FitnessApp.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Api.Features.Plans;

public sealed class PlanService(AppDbContext db, TimeProvider clock) : IPlanService
{
    private const string TemplateIsReadOnly = "Templates can't be changed. Copy the template to make your own plan.";
    private const string ArchivedIsReadOnly = "An archived plan can't be changed. Restore it first.";

    public async Task<PagedResponse<PlanSummaryResponse>> ListAsync(string userId, PlanQuery query, CancellationToken ct)
    {
        var plans = db.TrainingPlans.AsNoTracking().Where(p => p.UserId == userId);
        if (query.Status is { } status)
        {
            plans = plans.Where(p => p.Status == status);
        }

        // Most recently changed first: the plan you are working on is at the top.
        var total = await plans.CountAsync(ct);
        var items = await plans
            .OrderByDescending(p => p.UpdatedAtUtc).ThenByDescending(p => p.Id)
            .Skip(query.Skip).Take(query.PageSize)
            .Select(PlanMappings.ToSummary)
            .ToListAsync(ct);

        return new PagedResponse<PlanSummaryResponse>(items, query.Page, query.PageSize, total);
    }

    public async Task<PagedResponse<PlanSummaryResponse>> ListTemplatesAsync(PageQuery query, CancellationToken ct)
    {
        var templates = db.TrainingPlans.AsNoTracking().Where(p => p.UserId == null);

        var total = await templates.CountAsync(ct);
        var items = await templates
            .OrderBy(p => p.Id)
            .Skip(query.Skip).Take(query.PageSize)
            .Select(PlanMappings.ToSummary)
            .ToListAsync(ct);

        return new PagedResponse<PlanSummaryResponse>(items, query.Page, query.PageSize, total);
    }

    public Task<PlanResponse?> GetAsync(string userId, int id, CancellationToken ct) =>
        VisibleTo(userId)
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(PlanMappings.ToResponse)
            .SingleOrDefaultAsync(ct);

    public async Task<PlanResponse> CreateAsync(string userId, PlanRequest request, CancellationToken ct)
    {
        await EnsureExercisesAreUsableAsync(userId, request.Exercises, ct);

        var now = clock.GetUtcNow();
        var plan = new TrainingPlan
        {
            UserId = userId,
            Status = PlanStatus.Draft,
            CreatedAtUtc = now,
        };
        Apply(plan, request, now);
        db.TrainingPlans.Add(plan);

        await db.SaveChangesAsync(ct);
        return (await GetAsync(userId, plan.Id, ct))!;
    }

    public async Task<PlanResponse?> UpdateAsync(string userId, int id, PlanRequest request, CancellationToken ct)
    {
        var plan = await FindEditableAsync(userId, id, ct);
        if (plan is null)
        {
            return null;
        }

        await EnsureExercisesAreUsableAsync(userId, request.Exercises, ct);

        // The list is replaced as a whole: the editor sends the plan as it should look, in order.
        db.PlanExercises.RemoveRange(plan.Exercises);
        plan.Exercises.Clear();
        Apply(plan, request, clock.GetUtcNow());

        await db.SaveChangesAsync(ct);
        return await GetAsync(userId, id, ct);
    }

    public async Task<PlanResponse?> ChangeStatusAsync(string userId, int id, PlanStatus status, CancellationToken ct)
    {
        var plan = await VisibleTo(userId).Include(p => p.Exercises).SingleOrDefaultAsync(p => p.Id == id, ct);
        if (plan is null)
        {
            return null;
        }

        if (plan.UserId is null)
        {
            throw new ForbiddenException(TemplateIsReadOnly);
        }

        switch (PlanStateMachine.Check(plan.Status, status, plan.Exercises.Count))
        {
            case PlanTransitionError.AlreadyInStatus:
                // Asking for the status it already has is harmless: the request is safe to repeat.
                return await GetAsync(userId, id, ct);
            case PlanTransitionError.NotAllowed:
                throw new ConflictException($"A plan can't go from {plan.Status} to {status}.");
            case PlanTransitionError.NoExercises:
                throw new DomainValidationException("Add at least one exercise before activating the plan.", "status");
        }

        plan.Status = status;
        plan.UpdatedAtUtc = clock.GetUtcNow();
        await db.SaveChangesAsync(ct);
        return await GetAsync(userId, id, ct);
    }

    public async Task<PlanResponse?> CopyAsync(string userId, int id, CancellationToken ct)
    {
        var source = await VisibleTo(userId)
            .AsNoTracking()
            .Include(p => p.Exercises)
            .SingleOrDefaultAsync(p => p.Id == id, ct);
        if (source is null)
        {
            return null;
        }

        var now = clock.GetUtcNow();
        var copy = new TrainingPlan
        {
            UserId = userId,
            // A template keeps its name; copying one of your own plans says so, so the two can be told apart.
            Name = source.UserId is null ? source.Name : TrimTo($"{source.Name} (copy)", 100),
            Description = source.Description,
            Status = PlanStatus.Draft,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Exercises = [.. source.Exercises
                .OrderBy(e => e.Order)
                .Select(e => new PlanExercise
                {
                    ExerciseId = e.ExerciseId,
                    Order = e.Order,
                    TargetSets = e.TargetSets,
                    TargetRepsMin = e.TargetRepsMin,
                    TargetRepsMax = e.TargetRepsMax,
                    TargetWeightKg = e.TargetWeightKg,
                    Notes = e.Notes,
                })],
        };
        db.TrainingPlans.Add(copy);

        await db.SaveChangesAsync(ct);
        return await GetAsync(userId, copy.Id, ct);
    }

    public async Task<bool> DeleteAsync(string userId, int id, CancellationToken ct)
    {
        var plan = await VisibleTo(userId).SingleOrDefaultAsync(p => p.Id == id, ct);
        if (plan is null)
        {
            return false;
        }

        if (plan.UserId is null)
        {
            throw new ForbiddenException(TemplateIsReadOnly);
        }

        if (!PlanStateMachine.CanDelete(plan.Status))
        {
            throw new ConflictException("Only draft plans can be deleted. Archive this plan instead.");
        }

        // Its exercises go with it (cascade); the exercises in the library are untouched.
        db.TrainingPlans.Remove(plan);
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>The user's own plans plus the templates. Anything else doesn't exist for this user.</summary>
    private IQueryable<TrainingPlan> VisibleTo(string userId) =>
        db.TrainingPlans.Where(p => p.UserId == null || p.UserId == userId);

    private async Task<TrainingPlan?> FindEditableAsync(string userId, int id, CancellationToken ct)
    {
        var plan = await VisibleTo(userId).Include(p => p.Exercises).SingleOrDefaultAsync(p => p.Id == id, ct);
        if (plan is null)
        {
            return null;
        }

        if (plan.UserId is null)
        {
            throw new ForbiddenException(TemplateIsReadOnly);
        }

        if (!PlanStateMachine.CanEdit(plan.Status))
        {
            throw new ConflictException(ArchivedIsReadOnly);
        }

        return plan;
    }

    /// <summary>
    /// Every exercise must be one the user can see (global or their own) and not archived: a plan can't point at
    /// another user's private exercise, even by guessing its id.
    /// </summary>
    private async Task EnsureExercisesAreUsableAsync(
        string userId, IReadOnlyList<PlanExerciseRequest> exercises, CancellationToken ct)
    {
        var ids = exercises.Select(e => e.ExerciseId).Distinct().ToList();
        if (ids.Count == 0)
        {
            return;
        }

        var usable = await db.Exercises.AsNoTracking().UsableBy(userId).CountAsync(e => ids.Contains(e.Id), ct);
        if (usable != ids.Count)
        {
            throw new DomainValidationException("One or more exercises don't exist or were removed from the library.", "exercises");
        }
    }

    private static void Apply(TrainingPlan plan, PlanRequest request, DateTimeOffset now)
    {
        plan.Name = request.Name.Trim();
        plan.Description = request.Description?.Trim() ?? "";
        plan.UpdatedAtUtc = now;
        plan.Exercises.AddRange(request.Exercises.Select((e, index) => new PlanExercise
        {
            ExerciseId = e.ExerciseId,
            // The position in the request is the order in the plan.
            Order = index + 1,
            TargetSets = e.TargetSets,
            TargetRepsMin = e.TargetRepsMin,
            TargetRepsMax = e.TargetRepsMax,
            TargetWeightKg = e.TargetWeightKg is { } kg ? Math.Round(kg, 2, MidpointRounding.AwayFromZero) : null,
            Notes = e.Notes?.Trim() ?? "",
        }));
    }

    private static string TrimTo(string text, int maxLength) => text.Length <= maxLength ? text : text[..maxLength];
}
