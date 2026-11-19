using FitnessApp.Api.Common;
using FitnessApp.Api.Common.Errors;
using FitnessApp.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FitnessApp.Api.Features.Exercises;

public sealed class ExerciseService(AppDbContext db, TimeProvider clock) : IExerciseService
{
    private const string DuplicateName = "Ćwiczenie o tej nazwie już istnieje.";
    private const string GlobalIsReadOnly = "Ćwiczenia z biblioteki może zmieniać tylko administrator.";
    private const string ArchivedIsReadOnly = "Zarchiwizowanego ćwiczenia nie można edytować.";

    public async Task<PagedResponse<ExerciseResponse>> SearchAsync(string userId, ExerciseQuery query, CancellationToken ct)
    {
        var exercises = query.OnlyCustom
            ? db.Exercises.Where(e => e.UserId == userId)
            : VisibleTo(userId);
        exercises = exercises.AsNoTracking().Where(e => !e.IsArchived);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // unaccent() strips Polish accents on both sides, so "lawka" finds "ławka"; ILIKE ignores case.
            var pattern = $"%{EscapeLikePattern(query.Search.Trim())}%";
            exercises = exercises.Where(e =>
                EF.Functions.ILike(EF.Functions.Unaccent(e.Name), EF.Functions.Unaccent(pattern)) ||
                EF.Functions.ILike(EF.Functions.Unaccent(e.Muscles), EF.Functions.Unaccent(pattern)));
        }

        if (query.Category is { } category)
        {
            exercises = exercises.Where(e => e.Category == category);
        }

        if (query.Difficulty is { } difficulty)
        {
            exercises = exercises.Where(e => e.Difficulty == difficulty);
        }

        if (query.Equipment is { } equipment)
        {
            exercises = exercises.Where(e => e.Equipment == equipment);
        }

        var total = await exercises.CountAsync(ct);
        var items = await exercises
            // Id breaks ties, so the order is stable and paging never skips or repeats a row.
            .OrderBy(e => e.Name).ThenBy(e => e.Id)
            .Skip(query.Skip).Take(query.PageSize)
            .Select(ExerciseMappings.ToResponseExpression)
            .ToListAsync(ct);

        return new PagedResponse<ExerciseResponse>(items, query.Page, query.PageSize, total);
    }

    public Task<ExerciseResponse?> GetAsync(string userId, int id, CancellationToken ct) =>
        VisibleTo(userId)
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(ExerciseMappings.ToResponseExpression)
            .SingleOrDefaultAsync(ct);

    public Task<ExerciseResponse> CreateCustomAsync(string userId, ExerciseRequest request, CancellationToken ct) =>
        CreateAsync(userId, request, ct);

    public Task<ExerciseResponse> CreateGlobalAsync(ExerciseRequest request, CancellationToken ct) =>
        CreateAsync(ownerId: null, request, ct);

    public async Task<ExerciseResponse?> UpdateCustomAsync(string userId, int id, ExerciseRequest request, CancellationToken ct)
    {
        var exercise = await VisibleTo(userId).SingleOrDefaultAsync(e => e.Id == id, ct);
        if (exercise is null)
        {
            return null;
        }

        if (exercise.UserId is null)
        {
            throw new ForbiddenException(GlobalIsReadOnly);
        }

        return await UpdateAsync(exercise, request, ct);
    }

    public async Task<ExerciseResponse?> UpdateGlobalAsync(int id, ExerciseRequest request, CancellationToken ct)
    {
        // The administrator manages the shared catalog only; users' custom exercises are private.
        var exercise = await db.Exercises.SingleOrDefaultAsync(e => e.Id == id && e.UserId == null, ct);
        return exercise is null ? null : await UpdateAsync(exercise, request, ct);
    }

    public async Task<bool> ArchiveCustomAsync(string userId, int id, CancellationToken ct)
    {
        var exercise = await VisibleTo(userId).SingleOrDefaultAsync(e => e.Id == id, ct);
        if (exercise is null)
        {
            return false;
        }

        if (exercise.UserId is null)
        {
            throw new ForbiddenException(GlobalIsReadOnly);
        }

        await ArchiveAsync(exercise, ct);
        return true;
    }

    public async Task<bool> ArchiveGlobalAsync(int id, CancellationToken ct)
    {
        var exercise = await db.Exercises.SingleOrDefaultAsync(e => e.Id == id && e.UserId == null, ct);
        if (exercise is null)
        {
            return false;
        }

        await ArchiveAsync(exercise, ct);
        return true;
    }

    /// <summary>Ownership lives in the query: anything outside this set behaves as if it didn't exist.</summary>
    private IQueryable<Exercise> VisibleTo(string userId) =>
        db.Exercises.Where(e => e.UserId == null || e.UserId == userId);

    private async Task<ExerciseResponse> CreateAsync(string? ownerId, ExerciseRequest request, CancellationToken ct)
    {
        var name = request.Name.Trim();
        await EnsureNameIsFreeAsync(ownerId, name, exceptId: null, ct);

        var exercise = new Exercise
        {
            UserId = ownerId,
            CreatedAtUtc = clock.GetUtcNow(),
        };
        Apply(exercise, request, name);
        db.Exercises.Add(exercise);

        await SaveAsync(ct);
        return exercise.ToResponse();
    }

    private async Task<ExerciseResponse> UpdateAsync(Exercise exercise, ExerciseRequest request, CancellationToken ct)
    {
        if (exercise.IsArchived)
        {
            throw new ConflictException(ArchivedIsReadOnly);
        }

        var name = request.Name.Trim();
        await EnsureNameIsFreeAsync(exercise.UserId, name, exercise.Id, ct);
        Apply(exercise, request, name);

        await SaveAsync(ct);
        return exercise.ToResponse();
    }

    private async Task ArchiveAsync(Exercise exercise, CancellationToken ct)
    {
        // Archiving twice is fine: the result is the same, so the request is safe to repeat.
        exercise.IsArchived = true;
        await db.SaveChangesAsync(ct);
    }

    private static void Apply(Exercise exercise, ExerciseRequest request, string name)
    {
        exercise.Name = name;
        exercise.Description = request.Description?.Trim() ?? "";
        exercise.Muscles = request.Muscles?.Trim() ?? "";
        // [Required] on the request guarantees these have values by the time the service runs.
        exercise.Category = request.Category!.Value;
        exercise.Difficulty = request.Difficulty!.Value;
        exercise.Equipment = request.Equipment!.Value;
    }

    /// <summary>
    /// A name must be unique among the exercises its owner sees (case-insensitive): a user's custom exercise may not
    /// repeat a global one, and two globals may not share a name. Archived exercises free their name.
    /// </summary>
    private async Task EnsureNameIsFreeAsync(string? ownerId, string name, int? exceptId, CancellationToken ct)
    {
        // ILIKE without wildcards is a case-insensitive "equals" done by PostgreSQL.
        var exactName = EscapeLikePattern(name);
        var taken = await db.Exercises.AnyAsync(e =>
            !e.IsArchived &&
            e.Id != exceptId &&
            (e.UserId == null || e.UserId == ownerId) &&
            EF.Functions.ILike(e.Name, exactName), ct);

        if (taken)
        {
            throw new ConflictException(DuplicateName);
        }
    }

    /// <summary>
    /// The check above can't stop two identical requests arriving at the same moment; the unique index then
    /// rejects the second insert, and it gets the same 409 instead of a 500.
    /// </summary>
    private async Task SaveAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConflictException(DuplicateName);
        }
    }

    /// <summary>% and _ are wildcards in LIKE; escape them so a search for "50%" means the text "50%".</summary>
    private static string EscapeLikePattern(string text) =>
        text.Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("%", @"\%", StringComparison.Ordinal)
            .Replace("_", @"\_", StringComparison.Ordinal);
}
