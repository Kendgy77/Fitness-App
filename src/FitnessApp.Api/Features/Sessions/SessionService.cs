using FitnessApp.Api.Common;
using FitnessApp.Api.Common.Errors;
using FitnessApp.Api.Features.Exercises;
using FitnessApp.Api.Features.Plans;
using FitnessApp.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Api.Features.Sessions;

public sealed class SessionService(AppDbContext db, TimeProvider clock) : ISessionService
{
    private const int MaxExercises = 30;
    private const int MaxSetsPerExercise = 50;

    /// <summary>Phones and servers disagree on the time by a little; a workout "now" must not count as the future.</summary>
    private static readonly TimeSpan ClockTolerance = TimeSpan.FromMinutes(5);

    /// <summary>"Most used" looks at the last 90 days, so it follows the current routine, not last year's.</summary>
    private static readonly TimeSpan SuggestionWindow = TimeSpan.FromDays(90);

    // ---- Reads -------------------------------------------------------------------------------------------

    public async Task<PagedResponse<SessionSummaryResponse>> ListAsync(string userId, SessionQuery query, CancellationToken ct)
    {
        var sessions = db.WorkoutSessions.AsNoTracking().Where(s => s.UserId == userId);

        // A workout's date: when it started, or when it is planned for.
        if (query.From is { } from)
        {
            sessions = sessions.Where(s => (s.StartedAtUtc ?? s.ScheduledAtUtc) >= from);
        }

        if (query.To is { } to)
        {
            sessions = sessions.Where(s => (s.StartedAtUtc ?? s.ScheduledAtUtc) < to);
        }

        if (query.ExerciseId is { } exerciseId)
        {
            sessions = sessions.Where(s => s.Exercises.Any(e => e.ExerciseId == exerciseId));
        }

        if (query.Status is { } status)
        {
            sessions = sessions.Where(s => s.Status == status);
        }

        var total = await sessions.CountAsync(ct);
        var items = await sessions
            .OrderByDescending(s => s.StartedAtUtc ?? s.ScheduledAtUtc ?? s.CreatedAtUtc).ThenByDescending(s => s.Id)
            .Skip(query.Skip).Take(query.PageSize)
            .Select(SessionMappings.ToSummary)
            .ToListAsync(ct);

        return new PagedResponse<SessionSummaryResponse>(items, query.Page, query.PageSize, total);
    }

    public Task<SessionResponse?> GetAsync(string userId, int id, CancellationToken ct) =>
        db.WorkoutSessions.AsNoTracking()
            .Where(s => s.Id == id && s.UserId == userId)
            .Select(SessionMappings.ToResponse)
            .SingleOrDefaultAsync(ct);

    public Task<SessionResponse?> GetActiveAsync(string userId, CancellationToken ct) =>
        db.WorkoutSessions.AsNoTracking()
            .Where(s => s.UserId == userId && (s.Status == SessionStatus.InProgress || s.Status == SessionStatus.Paused))
            .OrderByDescending(s => s.StartedAtUtc)
            .Select(SessionMappings.ToResponse)
            .FirstOrDefaultAsync(ct);

    public async Task<StartOptionsResponse> GetStartOptionsAsync(string userId, CancellationToken ct)
    {
        // The user's active plans and the templates (drafts aren't ready, archived plans are history).
        var plans = await db.TrainingPlans.AsNoTracking()
            .Where(p => (p.UserId == userId && p.Status == PlanStatus.Active) || p.UserId == null)
            .Select(p => new { p.Id, p.Name, IsTemplate = p.UserId == null, ExerciseCount = p.Exercises.Count })
            .ToListAsync(ct);

        var lastUsed = await db.WorkoutSessions.AsNoTracking()
            .Where(s => s.UserId == userId && s.PlanId != null)
            .GroupBy(s => s.PlanId!.Value)
            .Select(g => new { PlanId = g.Key, Last = g.Max(s => s.StartedAtUtc) })
            .ToDictionaryAsync(x => x.PlanId, x => x.Last, ct);

        // Most recently used first, then your own plans before the templates, then by name.
        var options = plans
            .Select(p => new StartPlanOption(p.Id, p.Name, p.IsTemplate, p.ExerciseCount, lastUsed.GetValueOrDefault(p.Id)))
            .OrderByDescending(p => p.LastUsedAtUtc.HasValue)
            .ThenByDescending(p => p.LastUsedAtUtc)
            .ThenBy(p => p.IsTemplate)
            .ThenBy(p => p.Name, StringComparer.CurrentCulture)
            .ToList();

        return new StartOptionsResponse(options);
    }

    public async Task<ExerciseSuggestionsResponse> GetExerciseSuggestionsAsync(string userId, CancellationToken ct)
    {
        var since = clock.GetUtcNow() - SuggestionWindow;
        var used = db.SessionExercises.AsNoTracking()
            .Where(e => e.Session.UserId == userId && e.Session.StartedAtUtc != null)
            .Select(e => new { e.ExerciseId, e.WorkoutSessionId, StartedAtUtc = e.Session.StartedAtUtc!.Value });

        // One grouped query each; nothing is counted ahead of time, so there is no counter to keep in sync.
        var mostUsedIds = await used
            .Where(x => x.StartedAtUtc >= since)
            .GroupBy(x => x.ExerciseId)
            .Select(g => new { ExerciseId = g.Key, Workouts = g.Select(x => x.WorkoutSessionId).Distinct().Count() })
            .OrderByDescending(x => x.Workouts).ThenBy(x => x.ExerciseId)
            .Take(10)
            .Select(x => x.ExerciseId)
            .ToListAsync(ct);

        var recentIds = await used
            .Where(x => !mostUsedIds.Contains(x.ExerciseId))
            .GroupBy(x => x.ExerciseId)
            .Select(g => new { ExerciseId = g.Key, Last = g.Max(x => x.StartedAtUtc) })
            .OrderByDescending(x => x.Last).ThenBy(x => x.ExerciseId)
            .Take(10)
            .Select(x => x.ExerciseId)
            .ToListAsync(ct);

        var ids = mostUsedIds.Concat(recentIds).ToList();
        var exercises = await db.Exercises.AsNoTracking()
            .UsableBy(userId)
            .Where(e => ids.Contains(e.Id))
            .Select(ExerciseMappings.ToResponseExpression)
            .ToDictionaryAsync(e => e.Id, ct);

        // Keep each list's order; an exercise archived since then is simply left out.
        List<ExerciseResponse> InOrder(List<int> list) =>
            [.. list.Where(exercises.ContainsKey).Select(id => exercises[id])];

        return new ExerciseSuggestionsResponse(InOrder(mostUsedIds), InOrder(recentIds));
    }

    public async Task<PagedResponse<ExerciseHistoryEntry>?> GetExerciseHistoryAsync(
        string userId, int exerciseId, PageQuery query, CancellationToken ct)
    {
        // Archived exercises still have a history, so only visibility matters here.
        var visible = await db.Exercises.AsNoTracking()
            .AnyAsync(e => e.Id == exerciseId && (e.UserId == null || e.UserId == userId), ct);
        if (!visible)
        {
            return null;
        }

        var entries = db.SessionExercises.AsNoTracking()
            .Where(e => e.ExerciseId == exerciseId
                && e.Session.UserId == userId
                && e.Session.Status == SessionStatus.Completed
                && e.Sets.Count > 0);

        var total = await entries.CountAsync(ct);
        var items = await entries
            .OrderByDescending(e => e.Session.StartedAtUtc).ThenByDescending(e => e.Id)
            .Skip(query.Skip).Take(query.PageSize)
            .Select(e => new ExerciseHistoryEntry(
                e.WorkoutSessionId,
                e.Session.Name,
                e.Session.StartedAtUtc!.Value,
                e.Sets
                    .OrderBy(x => x.SetNumber)
                    .Select(x => new SetResponse(
                        x.Id, x.SetNumber, x.Type, x.Reps, x.WeightKg, x.Rpe, x.DurationSeconds, x.DistanceMeters, x.CompletedAtUtc))
                    .ToList()))
            .ToListAsync(ct);

        return new PagedResponse<ExerciseHistoryEntry>(items, query.Page, query.PageSize, total);
    }

    // ---- A workout entered afterwards --------------------------------------------------------------------

    public async Task<SessionResponse> LogAsync(string userId, LogSessionRequest request, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        await EnsureExercisesAreUsableAsync(userId, request.Exercises.Select(e => e.ExerciseId), ct);

        var session = new WorkoutSession { UserId = userId, Status = SessionStatus.Completed, CreatedAtUtc = now };
        ApplyLog(session, request, now);
        db.WorkoutSessions.Add(session);

        await db.SaveChangesAsync(ct);
        return (await GetAsync(userId, session.Id, ct))!;
    }

    public async Task<SessionResponse?> UpdateAsync(string userId, int id, LogSessionRequest request, CancellationToken ct)
    {
        var session = await db.WorkoutSessions
            .Include(s => s.Exercises)
            .SingleOrDefaultAsync(s => s.Id == id && s.UserId == userId, ct);
        if (session is null)
        {
            return null;
        }

        if (session.Status != SessionStatus.Completed)
        {
            throw new ConflictException("Only a finished workout can be edited with the full form.");
        }

        var now = clock.GetUtcNow();
        await EnsureExercisesAreUsableAsync(userId, request.Exercises.Select(e => e.ExerciseId), ct);

        // Like plans: the form sends the workout as it should look, so the exercises and sets are replaced.
        db.SessionExercises.RemoveRange(session.Exercises);
        session.Exercises.Clear();
        ApplyLog(session, request, now);

        await db.SaveChangesAsync(ct);
        return await GetAsync(userId, id, ct);
    }

    private static void ApplyLog(WorkoutSession session, LogSessionRequest request, DateTimeOffset now)
    {
        var startedAt = request.StartedAt!.Value.ToUniversalTime();
        if (startedAt > now + ClockTolerance)
        {
            throw new DomainValidationException("A finished workout can't be in the future.", "startedAt");
        }

        session.Type = request.Type!.Value;
        session.Name = NameOrDefault(request.Name, session.Type);
        session.StartedAtUtc = startedAt;
        session.DurationMinutes = request.DurationMinutes!.Value;
        session.FinishedAtUtc = startedAt.AddMinutes(request.DurationMinutes.Value);
        session.Notes = request.Notes?.Trim() ?? "";
        session.UpdatedAtUtc = now;
        session.Exercises.AddRange(request.Exercises.Select((e, index) => new SessionExercise
        {
            ExerciseId = e.ExerciseId,
            Order = index + 1,
            Notes = e.Notes?.Trim() ?? "",
            Sets = [.. e.Sets.Select((set, i) => NewSet(set, i + 1, startedAt))],
        }));
    }

    // ---- A live workout ----------------------------------------------------------------------------------

    public async Task<SessionResponse> StartAsync(string userId, StartSessionRequest request, CancellationToken ct)
    {
        await EnsureNoActiveWorkoutAsync(userId, ct);

        var now = clock.GetUtcNow();
        var type = request.Type ?? SessionType.Strength;
        var session = new WorkoutSession
        {
            UserId = userId,
            Type = type,
            Status = SessionStatus.InProgress,
            StartedAtUtc = now,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        if (request.PlanId is { } planId)
        {
            var plan = await db.TrainingPlans.AsNoTracking()
                .Include(p => p.Exercises)
                .SingleOrDefaultAsync(p => p.Id == planId && (p.UserId == null || p.UserId == userId), ct)
                ?? throw new DomainValidationException("This plan doesn't exist.", "planId");

            if (plan.UserId is not null && plan.Status != PlanStatus.Active)
            {
                throw new DomainValidationException("Activate the plan before starting a workout from it.", "planId");
            }

            // A copy, not a link: editing the plan later never changes this workout.
            session.PlanId = plan.Id;
            session.Name = NameOrDefault(request.Name ?? plan.Name, type);
            session.Exercises = [.. plan.Exercises.OrderBy(e => e.Order).Select(e => new SessionExercise
            {
                ExerciseId = e.ExerciseId,
                Order = e.Order,
                Notes = e.Notes,
                TargetSets = e.TargetSets,
                TargetRepsMin = e.TargetRepsMin,
                TargetRepsMax = e.TargetRepsMax,
                TargetWeightKg = e.TargetWeightKg,
            })];
        }
        else
        {
            session.Name = NameOrDefault(request.Name, type);
        }

        db.WorkoutSessions.Add(session);
        await db.SaveChangesAsync(ct);
        return (await GetAsync(userId, session.Id, ct))!;
    }

    public async Task<SessionResponse?> ApplyAsync(
        string userId, int id, SessionAction action, FinishSessionRequest? finish, CancellationToken ct)
    {
        var session = await db.WorkoutSessions
            .Include(s => s.Exercises).ThenInclude(e => e.Sets)
            .SingleOrDefaultAsync(s => s.Id == id && s.UserId == userId, ct);
        if (session is null)
        {
            return null;
        }

        var next = SessionStateMachine.Next(session.Status, action)
            ?? throw new ConflictException(NotAllowedMessage(action, session.Status));

        var now = clock.GetUtcNow();
        switch (action)
        {
            case SessionAction.Start:
                await EnsureNoActiveWorkoutAsync(userId, ct);
                session.StartedAtUtc = now;
                break;
            case SessionAction.Pause:
                session.PausedAtUtc = now;
                break;
            case SessionAction.Resume:
                EndPause(session, now);
                break;
            case SessionAction.Finish:
                Finish(session, finish, now);
                break;
        }

        session.Status = next;
        session.UpdatedAtUtc = now;
        await db.SaveChangesAsync(ct);
        return await GetAsync(userId, id, ct);
    }

    private void Finish(WorkoutSession session, FinishSessionRequest? finish, DateTimeOffset now)
    {
        // Exercises that were added but skipped are dropped, instead of blocking the finish.
        var skipped = session.Exercises.Where(e => e.Sets.Count == 0).ToList();
        db.SessionExercises.RemoveRange(skipped);
        session.Exercises.RemoveAll(skipped.Contains);

        // T12: a finished workout has at least one exercise with at least one set.
        if (session.Exercises.Count == 0)
        {
            throw new DomainValidationException("Log at least one set before finishing the workout.", "exercises");
        }

        Renumber(session.Exercises);
        EndPause(session, now);
        session.FinishedAtUtc = now;
        session.DurationMinutes = finish?.DurationMinutes
            ?? SessionStateMachine.DurationMinutes(session.StartedAtUtc!.Value, now, session.PausedSeconds);
        if (finish?.Notes is { } notes)
        {
            session.Notes = notes.Trim();
        }
    }

    private static void EndPause(WorkoutSession session, DateTimeOffset now)
    {
        if (session.PausedAtUtc is { } pausedAt)
        {
            session.PausedSeconds += (int)Math.Max(0, (now - pausedAt).TotalSeconds);
            session.PausedAtUtc = null;
        }
    }

    public async Task<SessionResponse?> AddExerciseAsync(string userId, int id, int exerciseId, CancellationToken ct)
    {
        var session = await db.WorkoutSessions
            .Include(s => s.Exercises)
            .SingleOrDefaultAsync(s => s.Id == id && s.UserId == userId, ct);
        if (session is null)
        {
            return null;
        }

        EnsureExercisesCanChange(session);
        if (session.Exercises.Count >= MaxExercises)
        {
            throw new DomainValidationException($"A workout can have at most {MaxExercises} exercises.", "exerciseId");
        }

        await EnsureExercisesAreUsableAsync(userId, [exerciseId], ct, field: "exerciseId");

        session.Exercises.Add(new SessionExercise
        {
            ExerciseId = exerciseId,
            Order = session.Exercises.Count == 0 ? 1 : session.Exercises.Max(e => e.Order) + 1,
        });
        session.UpdatedAtUtc = clock.GetUtcNow();

        await db.SaveChangesAsync(ct);
        return await GetAsync(userId, id, ct);
    }

    public async Task<SessionResponse?> RemoveExerciseAsync(string userId, int id, int sessionExerciseId, CancellationToken ct)
    {
        var session = await db.WorkoutSessions
            .Include(s => s.Exercises)
            .SingleOrDefaultAsync(s => s.Id == id && s.UserId == userId, ct);
        var entry = session?.Exercises.SingleOrDefault(e => e.Id == sessionExerciseId);
        if (session is null || entry is null)
        {
            return null;
        }

        EnsureExercisesCanChange(session);

        // Its sets go with it (cascade).
        db.SessionExercises.Remove(entry);
        session.Exercises.Remove(entry);
        Renumber(session.Exercises);
        session.UpdatedAtUtc = clock.GetUtcNow();

        await db.SaveChangesAsync(ct);
        return await GetAsync(userId, id, ct);
    }

    public async Task<SessionResponse?> AddSetAsync(
        string userId, int id, int sessionExerciseId, SetRequest request, CancellationToken ct)
    {
        var entry = await FindEntryForSetsAsync(userId, id, sessionExerciseId, ct);
        if (entry is null)
        {
            return null;
        }

        if (entry.Sets.Count >= MaxSetsPerExercise)
        {
            throw new DomainValidationException($"An exercise can have at most {MaxSetsPerExercise} sets.", "sets");
        }

        if (request.Type == SetType.Drop && entry.Sets.Count == 0)
        {
            throw new DomainValidationException("A drop set must follow another set.", "type");
        }

        var now = clock.GetUtcNow();
        entry.Sets.Add(NewSet(request, entry.Sets.Count + 1, now));
        entry.Session.UpdatedAtUtc = now;

        await db.SaveChangesAsync(ct);
        return await GetAsync(userId, id, ct);
    }

    public async Task<SessionResponse?> UpdateSetAsync(
        string userId, int id, int sessionExerciseId, int setId, SetRequest request, CancellationToken ct)
    {
        var entry = await FindEntryForSetsAsync(userId, id, sessionExerciseId, ct);
        var set = entry?.Sets.SingleOrDefault(x => x.Id == setId);
        if (entry is null || set is null)
        {
            return null;
        }

        if (request.Type == SetType.Drop && set.SetNumber == 1)
        {
            throw new DomainValidationException("A drop set must follow another set.", "type");
        }

        set.Type = request.Type ?? SetType.Normal;
        set.Reps = request.Reps;
        set.WeightKg = RoundKg(request.WeightKg);
        set.Rpe = request.Rpe;
        set.DurationSeconds = request.DurationSeconds;
        set.DistanceMeters = request.DistanceMeters;
        entry.Session.UpdatedAtUtc = clock.GetUtcNow();

        await db.SaveChangesAsync(ct);
        return await GetAsync(userId, id, ct);
    }

    public async Task<SessionResponse?> DeleteSetAsync(string userId, int id, int sessionExerciseId, int setId, CancellationToken ct)
    {
        var entry = await FindEntryForSetsAsync(userId, id, sessionExerciseId, ct);
        var set = entry?.Sets.SingleOrDefault(x => x.Id == setId);
        if (entry is null || set is null)
        {
            return null;
        }

        db.SetEntries.Remove(set);
        entry.Sets.Remove(set);

        // Close the gap: sets stay numbered 1, 2, 3, … and the first set can't be a drop set.
        var number = 1;
        foreach (var remaining in entry.Sets.OrderBy(x => x.SetNumber))
        {
            remaining.SetNumber = number++;
        }

        if (entry.Sets.SingleOrDefault(x => x.SetNumber == 1) is { Type: SetType.Drop } first)
        {
            first.Type = SetType.Normal;
        }

        entry.Session.UpdatedAtUtc = clock.GetUtcNow();
        await db.SaveChangesAsync(ct);
        return await GetAsync(userId, id, ct);
    }

    public async Task<bool> DeleteAsync(string userId, int id, CancellationToken ct)
    {
        // Exercises and sets go with it (cascade). Any status: discarding a workout is the user's choice.
        var deleted = await db.WorkoutSessions.Where(s => s.Id == id && s.UserId == userId).ExecuteDeleteAsync(ct);
        return deleted > 0;
    }

    // ---- Helpers -----------------------------------------------------------------------------------------

    /// <summary>
    /// The exercise row with its sets, found through the workout, so another user's sets can't be reached by
    /// guessing ids: ownership is checked on <c>Session.UserId</c>.
    /// </summary>
    private async Task<SessionExercise?> FindEntryForSetsAsync(string userId, int id, int sessionExerciseId, CancellationToken ct)
    {
        var entry = await db.SessionExercises
            .Include(e => e.Session)
            .Include(e => e.Sets)
            .SingleOrDefaultAsync(e => e.Id == sessionExerciseId && e.WorkoutSessionId == id && e.Session.UserId == userId, ct);

        if (entry is not null && !SessionStateMachine.CanLogSets(entry.Session.Status))
        {
            throw new ConflictException(entry.Session.Status == SessionStatus.Planned
                ? "Start the workout before logging sets."
                : "This workout is finished. Edit it with the full form.");
        }

        return entry;
    }

    private async Task EnsureNoActiveWorkoutAsync(string userId, CancellationToken ct)
    {
        var active = await db.WorkoutSessions.AnyAsync(
            s => s.UserId == userId && (s.Status == SessionStatus.InProgress || s.Status == SessionStatus.Paused), ct);
        if (active)
        {
            throw new ConflictException("You already have a workout in progress. Finish or discard it first.");
        }
    }

    private static void EnsureExercisesCanChange(WorkoutSession session)
    {
        if (!SessionStateMachine.CanChangeExercises(session.Status))
        {
            throw new ConflictException("This workout is finished. Edit it with the full form.");
        }
    }

    private async Task EnsureExercisesAreUsableAsync(
        string userId, IEnumerable<int> exerciseIds, CancellationToken ct, string field = "exercises")
    {
        var ids = exerciseIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return;
        }

        var usable = await db.Exercises.AsNoTracking().UsableBy(userId).CountAsync(e => ids.Contains(e.Id), ct);
        if (usable != ids.Count)
        {
            throw new DomainValidationException("One or more exercises don't exist or were removed from the library.", field);
        }
    }

    private static SetEntry NewSet(SetRequest request, int number, DateTimeOffset completedAt) => new()
    {
        SetNumber = number,
        Type = request.Type ?? SetType.Normal,
        Reps = request.Reps,
        WeightKg = RoundKg(request.WeightKg),
        Rpe = request.Rpe,
        DurationSeconds = request.DurationSeconds,
        DistanceMeters = request.DistanceMeters,
        CompletedAtUtc = completedAt,
    };

    private static decimal? RoundKg(decimal? kg) => kg is { } value ? Math.Round(value, 2, MidpointRounding.AwayFromZero) : null;

    private static void Renumber(List<SessionExercise> exercises)
    {
        var order = 1;
        foreach (var exercise in exercises.OrderBy(e => e.Order))
        {
            exercise.Order = order++;
        }
    }

    private static string NameOrDefault(string? name, SessionType type) =>
        string.IsNullOrWhiteSpace(name)
            ? type switch
            {
                SessionType.Strength => "Strength workout",
                SessionType.Cardio => "Cardio workout",
                SessionType.Mixed => "Mixed workout",
                _ => "Workout",
            }
            : name.Trim();

    private static string NotAllowedMessage(SessionAction action, SessionStatus status) => (action, status) switch
    {
        (_, SessionStatus.Completed) => "This workout is already finished.",
        (SessionAction.Start, _) => "This workout has already started.",
        (SessionAction.Pause, SessionStatus.Paused) => "This workout is already paused.",
        (SessionAction.Resume, SessionStatus.InProgress) => "This workout isn't paused.",
        _ => "Start the workout first.",
    };
}
