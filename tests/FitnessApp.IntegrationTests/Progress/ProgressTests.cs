using System.Net;
using System.Net.Http.Json;
using FitnessApp.Api.Features.Exercises;
using FitnessApp.Api.Features.Measurements;
using FitnessApp.Api.Features.Profile;
using FitnessApp.Api.Features.Progress;
using FitnessApp.Api.Features.Sessions;
using FitnessApp.IntegrationTests.Infrastructure;

namespace FitnessApp.IntegrationTests.Progress;

/// <summary>WF07: progress statistics. Test IDs refer to the thesis RTM.</summary>
public sealed class ProgressTests(ApiFactory factory)
{
    private const int BenchPress = 1;
    private const int BackSquat = 36;
    private const int PushUp = 7;
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    private static SetRequest Set(int reps, decimal? kg, SetType type = SetType.Normal) => new(type, reps, kg, null, null, null);

    private static LoggedExerciseRequest Exercise(int id, params SetRequest[] sets) => new(id, null, sets);

    private async Task LogAsync(HttpClient client, DateTimeOffset startedAt, params LoggedExerciseRequest[] exercises)
    {
        var request = new LogSessionRequest("Workout", SessionType.Strength, startedAt, 60, null, exercises);
        var response = await client.PostAsJsonAsync("/api/sessions", request, TestJson.Options, _ct);
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(_ct));
    }

    private Task<T?> GetAsync<T>(HttpClient client, string url) => client.GetFromJsonAsync<T>(url, TestJson.Options, _ct);

    [Fact]
    public async Task T13_after_logging_a_workout_the_statistics_and_charts_include_it()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var before = await GetAsync<ProgressSummaryResponse>(client, "/api/progress/summary");
        before!.WorkoutsTotal.ShouldBe(0);

        // Back squat 4 × 8 × 80 kg, as in T11.
        await LogAsync(client, DateTimeOffset.UtcNow.AddMinutes(-5), Exercise(BackSquat, Set(8, 80), Set(8, 80), Set(8, 80), Set(8, 80)));

        var summary = await GetAsync<ProgressSummaryResponse>(client, "/api/progress/summary");
        (summary!.WorkoutsTotal, summary.WorkoutsThisWeek, summary.StreakWeeks).ShouldBe((1, 1, 1));
        summary.VolumeThisWeekKg.ShouldBe(2560m);
        summary.LastWorkoutAtUtc.ShouldNotBeNull();

        var weeks = await GetAsync<List<WeeklyVolume>>(client, "/api/progress/volume?weeks=4");
        weeks!.Count.ShouldBe(4);
        (weeks[^1].VolumeKg, weeks[^1].Workouts, weeks[^1].Sets).ShouldBe((2560m, 1, 4));
        weeks.Take(3).ShouldAllBe(w => w.VolumeKg == 0);

        var squat = await GetAsync<ExerciseStatsResponse>(client, $"/api/progress/exercises/{BackSquat}?period=FourWeeks");
        var point = squat!.Workouts.ShouldHaveSingleItem();
        (point.BestOneRepMaxKg, point.TopSetWeightKg, point.TopSetReps, point.VolumeKg).ShouldBe((101.3m, 80m, 8, 2560m));
        squat.RepRanges.Single(r => r.Range == "6–8").Sets.ShouldBe(4);
    }

    [Fact]
    public async Task Records_and_ranking_cover_every_exercise_and_leave_out_warm_ups()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var now = DateTimeOffset.UtcNow;
        await LogAsync(client, now.AddDays(-14),
            Exercise(BenchPress, Set(10, 40, SetType.Warmup), Set(5, 80), Set(5, 80)),
            Exercise(PushUp, Set(20, null)));
        await LogAsync(client, now.AddDays(-7),
            Exercise(BenchPress, Set(5, 85), Set(8, 60, SetType.Drop)));

        var records = await GetAsync<List<ExerciseRecordsResponse>>(client, "/api/progress/records");
        var bench = records!.ShouldHaveSingleItem(); // push-ups have no weight, so no records
        (bench.ExerciseId, bench.HeaviestSet!.Value, bench.HeaviestSet.Reps).ShouldBe((BenchPress, 85m, 5));
        bench.BestOneRepMax!.Value.ShouldBe(99.2m);          // 85 × (1 + 5/30)
        bench.BestSessionVolume!.Value.ShouldBe(905m);       // 425 + 480 (drop set counts) beats 800

        var ranking = (await GetAsync<List<ExerciseProgressRow>>(client, "/api/progress/exercises?period=FourWeeks"))!;
        ranking.Select(r => (r.ExerciseId, r.Workouts)).ShouldBe([(BenchPress, 2), (PushUp, 1)]);
        ranking[0].OneRepMaxChangePercent.ShouldBe(6.3m);    // 93.3 → 99.2
    }

    [Fact]
    public async Task Muscle_groups_count_working_sets_per_week()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        await LogAsync(client, DateTimeOffset.UtcNow.AddDays(-3),
            Exercise(BenchPress, Set(10, 40, SetType.Warmup), Set(5, 80), Set(5, 80), Set(5, 80), Set(5, 80)),
            Exercise(BackSquat, Set(5, 100), Set(5, 100)));

        var groups = await GetAsync<List<MuscleGroupVolume>>(client, "/api/progress/muscle-groups?period=FourWeeks");

        // Four weeks: 4 chest sets → 1 a week, 2 leg sets → 0.5 a week. The warm-up doesn't count.
        groups!.Select(g => (g.Category, g.SetsPerWeek)).ShouldBe([(ExerciseCategory.Chest, 1.0m), (ExerciseCategory.Legs, 0.5m)]);
    }

    [Fact]
    public async Task The_summary_shows_the_latest_measured_weight_and_its_bmi()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var profile = new UpdateProfileRequest(null, null, 180, 90, null, "Europe/Warsaw");
        (await client.PutAsJsonAsync("/api/profile", profile, TestJson.Options, _ct)).EnsureSuccessStatusCode();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var measurement = new MeasurementRequest(today.AddDays(-1), 81, null, null, null, null, null, null, null, null, null, null, null, null, null);
        (await client.PostAsJsonAsync("/api/measurements", measurement, TestJson.Options, _ct)).EnsureSuccessStatusCode();

        var summary = await GetAsync<ProgressSummaryResponse>(client, "/api/progress/summary");

        (summary!.WeightKg, summary.WeightMeasuredOn).ShouldBe((81m, today.AddDays(-1)));
        summary.Bmi.ShouldBe(new BmiResult(25.0m, BmiCategory.Overweight));
        summary.WorkoutsTotal.ShouldBe(0);
    }

    [Fact]
    public async Task Only_finished_workouts_count_and_other_users_are_left_out()
    {
        var owner = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var other = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        await LogAsync(owner, DateTimeOffset.UtcNow.AddDays(-1), Exercise(BenchPress, Set(5, 100)));

        // A workout in progress, with a set, isn't history yet.
        var start = await other.PostAsJsonAsync("/api/sessions/start", new StartSessionRequest(1, null, null), TestJson.Options, _ct);
        var live = (await start.Content.ReadFromJsonAsync<SessionResponse>(TestJson.Options, _ct))!;
        (await other.PostAsJsonAsync($"/api/sessions/{live.Id}/exercises/{live.Exercises[0].Id}/sets", Set(10, 20), TestJson.Options, _ct))
            .EnsureSuccessStatusCode();

        var summary = await GetAsync<ProgressSummaryResponse>(other, "/api/progress/summary");
        summary!.WorkoutsTotal.ShouldBe(0);
        (await GetAsync<List<ExerciseRecordsResponse>>(other, "/api/progress/records"))!.ShouldBeEmpty();
        var bench = await GetAsync<ExerciseStatsResponse>(other, $"/api/progress/exercises/{BenchPress}?period=All");
        bench!.Workouts.ShouldBeEmpty();
    }

    [Fact]
    public async Task Another_users_private_exercise_has_no_stats_page()
    {
        var owner = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var other = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var created = await owner.PostAsJsonAsync("/api/exercises",
            new ExerciseRequest("Secret curl", null, null, ExerciseCategory.Arms, Difficulty.Beginner, Equipment.Dumbbell), TestJson.Options, _ct);
        var secret = (await created.Content.ReadFromJsonAsync<ExerciseResponse>(TestJson.Options, _ct))!;

        var response = await other.GetAsync($"/api/progress/exercises/{secret.Id}", _ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Weeks_out_of_range_return_400()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);

        var response = await client.GetAsync("/api/progress/volume?weeks=0", _ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
