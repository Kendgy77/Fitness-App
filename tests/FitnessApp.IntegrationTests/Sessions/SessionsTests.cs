using System.Net;
using System.Net.Http.Json;
using FitnessApp.Api.Common;
using FitnessApp.Api.Features.Exercises;
using FitnessApp.Api.Features.Plans;
using FitnessApp.Api.Features.Sessions;
using FitnessApp.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace FitnessApp.IntegrationTests.Sessions;

/// <summary>WF06: workouts, logged live or entered afterwards. Test IDs refer to the thesis RTM.</summary>
public sealed class SessionsTests(ApiFactory factory)
{
    private const string Url = "/api/sessions";
    private const int FullBodyTemplateId = 1; // 6 exercises, the first is Goblet Squat 3 × 10–12
    // From the seeded exercise catalog.
    private const int BenchPress = 1;
    private const int BackSquat = 36;
    private const int JumpRope = 60;
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    private static SetRequest Set(int reps, decimal? kg = null, SetType? type = null) => new(type, reps, kg, null, null, null);

    private static LogSessionRequest PastWorkout(params LoggedExerciseRequest[] exercises) => new(
        "Leg day",
        SessionType.Strength,
        new DateTimeOffset(2026, 6, 24, 18, 0, 0, TimeSpan.FromHours(2)),
        62,
        "Felt strong.",
        exercises.Length > 0 ? exercises : [new LoggedExerciseRequest(BackSquat, null, [Set(8, 80), Set(8, 80), Set(8, 80), Set(8, 80)])]);

    private async Task<SessionResponse> ReadAsync(HttpResponseMessage response, HttpStatusCode expected = HttpStatusCode.OK)
    {
        response.StatusCode.ShouldBe(expected, await response.Content.ReadAsStringAsync(_ct));
        return (await response.Content.ReadFromJsonAsync<SessionResponse>(TestJson.Options, _ct))!;
    }

    private async Task<ValidationProblemDetails> ReadProblemAsync(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        return (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(_ct))!;
    }

    private Task<HttpResponseMessage> PostAsync(HttpClient client, string url, object? body = null) =>
        client.PostAsJsonAsync(url, body ?? new { }, TestJson.Options, _ct);

    private async Task<SessionResponse> StartAsync(HttpClient client, int? planId = null) =>
        await ReadAsync(await PostAsync(client, $"{Url}/start", new StartSessionRequest(planId, null, null)), HttpStatusCode.Created);

    private async Task<SessionResponse> AddSetAsync(HttpClient client, SessionResponse session, int entryIndex, SetRequest set) =>
        await ReadAsync(await PostAsync(client, $"{Url}/{session.Id}/exercises/{session.Exercises[entryIndex].Id}/sets", set));

    // ---- A workout entered afterwards (T11, T12) ----------------------------------------------------------

    [Fact]
    public async Task T11_a_past_workout_is_saved_and_shows_in_the_history()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);

        var saved = await ReadAsync(await PostAsync(client, Url, PastWorkout()), HttpStatusCode.Created);

        saved.Status.ShouldBe(SessionStatus.Completed);
        saved.StartedAtUtc.ShouldBe(new DateTimeOffset(2026, 6, 24, 16, 0, 0, TimeSpan.Zero));
        saved.DurationMinutes.ShouldBe(62);
        saved.FinishedAtUtc.ShouldBe(saved.StartedAtUtc!.Value.AddMinutes(62));
        var squat = saved.Exercises.ShouldHaveSingleItem();
        squat.ExerciseName.ShouldBe("Barbell Back Squat");
        squat.Sets.Select(s => (s.SetNumber, s.Reps, s.WeightKg)).ShouldBe([(1, 8, 80m), (2, 8, 80m), (3, 8, 80m), (4, 8, 80m)]);

        var history = await client.GetFromJsonAsync<PagedResponse<SessionSummaryResponse>>(Url, TestJson.Options, _ct);
        var row = history!.Items.ShouldHaveSingleItem();
        (row.Id, row.Name, row.ExerciseCount, row.SetCount).ShouldBe((saved.Id, "Leg day", 1, 4));
        row.ExerciseNames.ShouldBe(["Barbell Back Squat"]);
    }

    [Fact]
    public async Task T12_a_workout_without_exercises_returns_400_and_is_not_saved()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);

        var problem = await ReadProblemAsync(await PostAsync(client, Url, PastWorkout() with { Exercises = [] }));

        problem.Errors["Exercises"].ShouldBe(["Add at least one exercise with at least one set."]);
        var history = await client.GetFromJsonAsync<PagedResponse<SessionSummaryResponse>>(Url, TestJson.Options, _ct);
        history!.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task T12_an_exercise_without_sets_returns_400_on_that_row()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var request = PastWorkout(
            new LoggedExerciseRequest(BackSquat, null, [Set(5, 100)]),
            new LoggedExerciseRequest(BenchPress, null, []));

        var problem = await ReadProblemAsync(await PostAsync(client, Url, request));

        problem.Errors["Exercises[1].Sets"].ShouldBe(["Add at least one set."]);
    }

    [Fact]
    public async Task A_past_workout_in_the_future_or_with_an_empty_set_returns_400()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);

        var future = await ReadProblemAsync(await PostAsync(client, Url, PastWorkout() with { StartedAt = DateTimeOffset.UtcNow.AddDays(1) }));
        future.Errors["startedAt"].ShouldBe(["A finished workout can't be in the future."]);

        var empty = await ReadProblemAsync(await PostAsync(client, Url, PastWorkout(new LoggedExerciseRequest(BackSquat, null, [Set(0)]))));
        empty.Errors["Exercises[0].Sets[0].Reps"].ShouldBe(["Enter the reps, or the time or distance for cardio."]);
    }

    [Fact]
    public async Task Cardio_sets_use_time_instead_of_reps()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var rope = new SetRequest(null, 0, null, null, DurationSeconds: 600, DistanceMeters: null);

        var saved = await ReadAsync(
            await PostAsync(client, Url, PastWorkout(new LoggedExerciseRequest(JumpRope, null, [rope])) with { Type = SessionType.Cardio, Name = null }),
            HttpStatusCode.Created);

        saved.Name.ShouldBe("Cardio workout");
        var set = saved.Exercises.ShouldHaveSingleItem().Sets.ShouldHaveSingleItem();
        (set.Reps, set.DurationSeconds, set.DistanceMeters).ShouldBe((0, 600, (int?)null));
    }

    [Fact]
    public async Task A_drop_set_cant_be_the_first_set()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);

        var problem = await ReadProblemAsync(await PostAsync(client, Url,
            PastWorkout(new LoggedExerciseRequest(BenchPress, null, [Set(10, 40, SetType.Drop)]))));

        problem.Errors["Exercises[0].Sets"].ShouldBe(["A drop set must follow another set."]);
    }

    [Fact]
    public async Task A_finished_workout_can_be_corrected_with_the_full_form()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var saved = await ReadAsync(await PostAsync(client, Url, PastWorkout()), HttpStatusCode.Created);

        var corrected = PastWorkout(new LoggedExerciseRequest(BackSquat, "Belt on", [Set(8, 82.5m), Set(6, 82.5m, SetType.Failure)]))
            with
        { DurationMinutes = 70 };
        var updated = await ReadAsync(await client.PutAsJsonAsync($"{Url}/{saved.Id}", corrected, TestJson.Options, _ct));

        updated.Status.ShouldBe(SessionStatus.Completed);
        updated.DurationMinutes.ShouldBe(70);
        var squat = updated.Exercises.ShouldHaveSingleItem();
        squat.Notes.ShouldBe("Belt on");
        squat.Sets.Select(s => (s.Reps, s.WeightKg, s.Type)).ShouldBe([(8, 82.5m, SetType.Normal), (6, 82.5m, SetType.Failure)]);
    }

    // ---- A live workout -----------------------------------------------------------------------------------

    [Fact]
    public async Task A_live_workout_from_a_plan_goes_start_pause_resume_finish()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);

        var session = await StartAsync(client, FullBodyTemplateId);
        session.Status.ShouldBe(SessionStatus.InProgress);
        session.Name.ShouldBe("Full Body – Beginner");
        session.Exercises.Count.ShouldBe(6);
        var goblet = session.Exercises[0];
        (goblet.ExerciseName, goblet.TargetSets, goblet.TargetRepsMin, goblet.TargetRepsMax).ShouldBe(("Goblet Squat", 3, 10, 12));
        goblet.Sets.ShouldBeEmpty();

        session = await AddSetAsync(client, session, 0, Set(12, 20));
        session = await AddSetAsync(client, session, 0, Set(10, 22.5m));
        session.Exercises[0].Sets.Select(s => (s.SetNumber, s.Reps, s.WeightKg)).ShouldBe([(1, 12, 20m), (2, 10, 22.5m)]);

        session = await ReadAsync(await PostAsync(client, $"{Url}/{session.Id}/pause"));
        session.Status.ShouldBe(SessionStatus.Paused);
        session.PausedAtUtc.ShouldNotBeNull();

        session = await ReadAsync(await PostAsync(client, $"{Url}/{session.Id}/resume"));
        session.Status.ShouldBe(SessionStatus.InProgress);
        session.PausedAtUtc.ShouldBeNull();

        session = await ReadAsync(await PostAsync(client, $"{Url}/{session.Id}/finish", new FinishSessionRequest(null, "Good one")));
        session.Status.ShouldBe(SessionStatus.Completed);
        session.FinishedAtUtc.ShouldNotBeNull();
        session.DurationMinutes.ShouldBe(1); // a test runs in seconds; the minimum is one minute
        session.Notes.ShouldBe("Good one");
        // The five exercises that got no sets were skipped, so they are dropped rather than blocking the finish.
        session.Exercises.ShouldHaveSingleItem().Order.ShouldBe(1);
    }

    [Fact]
    public async Task T12_finishing_without_any_set_returns_400_and_the_workout_stays_open()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var session = await StartAsync(client, FullBodyTemplateId);

        var problem = await ReadProblemAsync(await PostAsync(client, $"{Url}/{session.Id}/finish"));

        problem.Errors["exercises"].ShouldBe(["Log at least one set before finishing the workout."]);
        var still = await client.GetFromJsonAsync<SessionResponse>($"{Url}/{session.Id}", TestJson.Options, _ct);
        still!.Status.ShouldBe(SessionStatus.InProgress);
        still.Exercises.Count.ShouldBe(6);
    }

    [Fact]
    public async Task A_typed_in_duration_wins_over_the_clock()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var session = await StartAsync(client, FullBodyTemplateId);
        session = await AddSetAsync(client, session, 0, Set(10, 20));

        session = await ReadAsync(await PostAsync(client, $"{Url}/{session.Id}/finish", new FinishSessionRequest(55, null)));

        session.DurationMinutes.ShouldBe(55);
    }

    [Fact]
    public async Task Only_one_workout_can_be_in_progress()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var first = await StartAsync(client);

        var second = await PostAsync(client, $"{Url}/start", new StartSessionRequest(null, null, null));

        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var active = await client.GetFromJsonAsync<SessionResponse>($"{Url}/active", TestJson.Options, _ct);
        active!.Id.ShouldBe(first.Id);
    }

    [Fact]
    public async Task Without_an_active_workout_active_returns_204()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);

        var response = await client.GetAsync($"{Url}/active", _ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task A_workout_starts_only_from_an_active_plan_or_a_template()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var draft = await client.PostAsJsonAsync("/api/plans",
            new PlanRequest("Draft day", null, [new PlanExerciseRequest(BenchPress, 3, 8, 10, null, null)]), TestJson.Options, _ct);
        var plan = await draft.Content.ReadFromJsonAsync<PlanResponse>(TestJson.Options, _ct);

        var problem = await ReadProblemAsync(await PostAsync(client, $"{Url}/start", new StartSessionRequest(plan!.Id, null, null)));

        problem.Errors["planId"].ShouldBe(["Activate the plan before starting a workout from it."]);
    }

    [Fact]
    public async Task Editing_the_plan_later_does_not_change_the_workout()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var created = await client.PostAsJsonAsync("/api/plans",
            new PlanRequest("Push", null, [new PlanExerciseRequest(BenchPress, 4, 6, 8, 60m, null)]), TestJson.Options, _ct);
        var plan = (await created.Content.ReadFromJsonAsync<PlanResponse>(TestJson.Options, _ct))!;
        (await PostAsync(client, $"/api/plans/{plan.Id}/status", new ChangePlanStatusRequest(PlanStatus.Active))).EnsureSuccessStatusCode();
        var session = await StartAsync(client, plan.Id);

        var edited = new PlanRequest("Push", null, [new PlanExerciseRequest(BackSquat, 5, 5, 5, 100m, null)]);
        (await client.PutAsJsonAsync($"/api/plans/{plan.Id}", edited, TestJson.Options, _ct)).EnsureSuccessStatusCode();

        var after = await client.GetFromJsonAsync<SessionResponse>($"{Url}/{session.Id}", TestJson.Options, _ct);
        var bench = after!.Exercises.ShouldHaveSingleItem();
        (bench.ExerciseId, bench.TargetSets, bench.TargetRepsMin, bench.TargetRepsMax, bench.TargetWeightKg)
            .ShouldBe((BenchPress, 4, 6, 8, 60m));
        after.PlanId.ShouldBe(plan.Id);
    }

    [Theory]
    [InlineData("pause")]
    [InlineData("resume")]
    [InlineData("finish")]
    [InlineData("start")]
    public async Task A_finished_workout_cant_change_status(string action)
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var saved = await ReadAsync(await PostAsync(client, Url, PastWorkout()), HttpStatusCode.Created);

        var response = await PostAsync(client, $"{Url}/{saved.Id}/{action}");

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Resuming_a_workout_that_isnt_paused_returns_409()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var session = await StartAsync(client);

        var response = await PostAsync(client, $"{Url}/{session.Id}/resume");

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Sets_of_a_finished_workout_are_corrected_with_the_full_form_only()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var saved = await ReadAsync(await PostAsync(client, Url, PastWorkout()), HttpStatusCode.Created);

        var response = await PostAsync(client, $"{Url}/{saved.Id}/exercises/{saved.Exercises[0].Id}/sets", Set(5, 100));

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Exercises_are_added_and_removed_during_a_live_workout()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var session = await StartAsync(client);
        session.Name.ShouldBe("Strength workout");
        session.Exercises.ShouldBeEmpty();

        session = await ReadAsync(await PostAsync(client, $"{Url}/{session.Id}/exercises", new AddSessionExerciseRequest(BenchPress)));
        session = await ReadAsync(await PostAsync(client, $"{Url}/{session.Id}/exercises", new AddSessionExerciseRequest(BackSquat)));
        session.Exercises.Select(e => (e.Order, e.ExerciseId)).ShouldBe([(1, BenchPress), (2, BackSquat)]);

        session = await ReadAsync(await client.DeleteAsync($"{Url}/{session.Id}/exercises/{session.Exercises[0].Id}", _ct));
        session.Exercises.Select(e => (e.Order, e.ExerciseId)).ShouldBe([(1, BackSquat)]);
    }

    [Fact]
    public async Task Drop_sets_follow_a_set_and_deleting_renumbers_the_rest()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var session = await StartAsync(client, FullBodyTemplateId);

        var dropFirst = await ReadProblemAsync(await PostAsync(client,
            $"{Url}/{session.Id}/exercises/{session.Exercises[0].Id}/sets", Set(10, 16, SetType.Drop)));
        dropFirst.Errors["type"].ShouldBe(["A drop set must follow another set."]);

        session = await AddSetAsync(client, session, 0, Set(10, 20));
        session = await AddSetAsync(client, session, 0, Set(8, 15, SetType.Drop));
        session = await AddSetAsync(client, session, 0, Set(10, 20));

        // Deleting the first set leaves the drop set first, which can't be, so it becomes a normal set.
        var first = session.Exercises[0].Sets[0];
        session = await ReadAsync(await client.DeleteAsync($"{Url}/{session.Id}/exercises/{session.Exercises[0].Id}/sets/{first.Id}", _ct));
        session.Exercises[0].Sets.Select(s => (s.SetNumber, s.Type, s.Reps)).ShouldBe([(1, SetType.Normal, 8), (2, SetType.Normal, 10)]);
    }

    [Fact]
    public async Task A_set_can_be_corrected_during_the_workout()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var session = await StartAsync(client, FullBodyTemplateId);
        session = await AddSetAsync(client, session, 0, Set(10, 20));
        var set = session.Exercises[0].Sets[0];

        session = await ReadAsync(await client.PutAsJsonAsync(
            $"{Url}/{session.Id}/exercises/{session.Exercises[0].Id}/sets/{set.Id}",
            new SetRequest(SetType.Warmup, 12, 12.5m, 6, null, null), TestJson.Options, _ct));

        session.Exercises[0].Sets.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            s => s.Type.ShouldBe(SetType.Warmup),
            s => s.Reps.ShouldBe(12),
            s => s.WeightKg.ShouldBe(12.5m),
            s => s.Rpe.ShouldBe(6));
    }

    [Fact]
    public async Task Any_workout_can_be_deleted()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var live = await StartAsync(client, FullBodyTemplateId);
        var past = await ReadAsync(await PostAsync(client, Url, PastWorkout()), HttpStatusCode.Created);

        (await client.DeleteAsync($"{Url}/{live.Id}", _ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await client.DeleteAsync($"{Url}/{past.Id}", _ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await client.GetAsync($"{Url}/{past.Id}", _ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await client.GetAsync($"{Url}/active", _ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    // ---- History, suggestions, start options -------------------------------------------------------------

    [Fact]
    public async Task History_filters_by_date_range_and_exercise()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var june = PastWorkout() with { StartedAt = new DateTimeOffset(2026, 6, 10, 18, 0, 0, TimeSpan.Zero) };
        var july = PastWorkout(new LoggedExerciseRequest(BenchPress, null, [Set(8, 60)]))
            with
        { Name = "Push", StartedAt = new DateTimeOffset(2026, 7, 10, 18, 0, 0, TimeSpan.Zero) };
        await ReadAsync(await PostAsync(client, Url, june), HttpStatusCode.Created);
        await ReadAsync(await PostAsync(client, Url, july), HttpStatusCode.Created);

        var all = await client.GetFromJsonAsync<PagedResponse<SessionSummaryResponse>>(Url, TestJson.Options, _ct);
        all!.Items.Select(s => s.Name).ShouldBe(["Push", "Leg day"]); // newest first

        var inJune = await client.GetFromJsonAsync<PagedResponse<SessionSummaryResponse>>(
            $"{Url}?from=2026-06-01T00:00:00Z&to=2026-07-01T00:00:00Z", TestJson.Options, _ct);
        inJune!.Items.ShouldHaveSingleItem().Name.ShouldBe("Leg day");

        var withBench = await client.GetFromJsonAsync<PagedResponse<SessionSummaryResponse>>(
            $"{Url}?exerciseId={BenchPress}", TestJson.Options, _ct);
        withBench!.Items.ShouldHaveSingleItem().Name.ShouldBe("Push");
    }

    [Fact]
    public async Task Exercise_history_lists_every_finished_workout_with_its_sets()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        await ReadAsync(await PostAsync(client, Url, PastWorkout() with { StartedAt = new DateTimeOffset(2026, 6, 1, 18, 0, 0, TimeSpan.Zero) }), HttpStatusCode.Created);
        await ReadAsync(await PostAsync(client, Url, PastWorkout(new LoggedExerciseRequest(BackSquat, null, [Set(5, 100)]))
            with
        { StartedAt = new DateTimeOffset(2026, 6, 8, 18, 0, 0, TimeSpan.Zero) }), HttpStatusCode.Created);
        await StartAsync(client, FullBodyTemplateId); // in progress: not history yet

        var history = await client.GetFromJsonAsync<PagedResponse<ExerciseHistoryEntry>>(
            $"/api/exercises/{BackSquat}/history", TestJson.Options, _ct);

        history!.TotalCount.ShouldBe(2);
        history.Items[0].StartedAtUtc.ShouldBe(new DateTimeOffset(2026, 6, 8, 18, 0, 0, TimeSpan.Zero));
        history.Items[0].Sets.ShouldHaveSingleItem().WeightKg.ShouldBe(100m);
        history.Items[1].Sets.Count.ShouldBe(4);
    }

    [Fact]
    public async Task Suggestions_put_the_most_used_exercises_first()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var recently = DateTimeOffset.UtcNow.AddDays(-3);
        await ReadAsync(await PostAsync(client, Url, PastWorkout() with { StartedAt = recently }), HttpStatusCode.Created);
        await ReadAsync(await PostAsync(client, Url, PastWorkout() with { StartedAt = recently.AddDays(-2) }), HttpStatusCode.Created);
        await ReadAsync(await PostAsync(client, Url, PastWorkout(new LoggedExerciseRequest(BenchPress, null, [Set(8, 60)]))
            with
        { StartedAt = recently.AddDays(1) }), HttpStatusCode.Created);
        // Older than 90 days: counts as recent, not as most used.
        await ReadAsync(await PostAsync(client, Url, PastWorkout(new LoggedExerciseRequest(JumpRope, null, [Set(0) with { DurationSeconds = 600 }]))
            with
        { StartedAt = DateTimeOffset.UtcNow.AddDays(-200) }), HttpStatusCode.Created);

        var suggestions = await client.GetFromJsonAsync<ExerciseSuggestionsResponse>("/api/exercises/suggestions", TestJson.Options, _ct);

        suggestions!.MostUsed.Select(e => e.Id).ShouldBe([BackSquat, BenchPress]); // squat: 2 workouts, bench: 1
        suggestions.Recent.Select(e => e.Id).ShouldBe([JumpRope]);
    }

    [Fact]
    public async Task Start_options_list_active_plans_and_templates_most_recently_used_first()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var used = await StartAsync(client, 2); // Upper Body template
        (await client.DeleteAsync($"{Url}/{used.Id}", _ct)).EnsureSuccessStatusCode();
        await StartAsync(client, 3); // Lower Body template, used last

        var options = await client.GetFromJsonAsync<StartOptionsResponse>($"{Url}/start-options", TestJson.Options, _ct);

        // The deleted workout no longer counts as a use, so only Lower Body has been used.
        options!.Plans[0].Id.ShouldBe(3);
        options.Plans[0].LastUsedAtUtc.ShouldNotBeNull();
        options.Plans.Select(p => p.Id).ShouldBe([3, 1, 2]); // then templates by name
    }

    // ---- Other users (cross-user 404) --------------------------------------------------------------------

    [Fact]
    public async Task Another_users_workout_exercises_and_sets_are_not_found()
    {
        var owner = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var other = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var session = await StartAsync(owner, FullBodyTemplateId);
        session = await AddSetAsync(owner, session, 0, Set(10, 20));
        var entry = session.Exercises[0];
        var set = entry.Sets[0];

        (await other.GetAsync($"{Url}/{session.Id}", _ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await PostAsync(other, $"{Url}/{session.Id}/pause")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await PostAsync(other, $"{Url}/{session.Id}/exercises/{entry.Id}/sets", Set(10, 20))).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await other.PutAsJsonAsync($"{Url}/{session.Id}/exercises/{entry.Id}/sets/{set.Id}", Set(1, 1), TestJson.Options, _ct))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await other.DeleteAsync($"{Url}/{session.Id}/exercises/{entry.Id}/sets/{set.Id}", _ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await other.DeleteAsync($"{Url}/{session.Id}", _ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var otherHistory = await other.GetFromJsonAsync<PagedResponse<SessionSummaryResponse>>(Url, TestJson.Options, _ct);
        otherHistory!.TotalCount.ShouldBe(0);
        // The owner's workout is untouched.
        var mine = await owner.GetFromJsonAsync<SessionResponse>($"{Url}/{session.Id}", TestJson.Options, _ct);
        mine!.Exercises[0].Sets.ShouldHaveSingleItem().Reps.ShouldBe(10);
    }

    [Fact]
    public async Task A_set_cant_be_added_through_another_workouts_id()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var past = await ReadAsync(await PostAsync(client, Url, PastWorkout()), HttpStatusCode.Created);
        var live = await StartAsync(client, FullBodyTemplateId);

        // The exercise row belongs to the live workout, not to the past one in the URL.
        var response = await PostAsync(client, $"{Url}/{past.Id}/exercises/{live.Exercises[0].Id}/sets", Set(10, 20));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Another_users_private_exercise_cant_be_logged()
    {
        var owner = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var other = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var created = await owner.PostAsJsonAsync("/api/exercises",
            new ExerciseRequest("Secret press", null, null, ExerciseCategory.Chest, Difficulty.Beginner, Equipment.Dumbbell), TestJson.Options, _ct);
        var secret = (await created.Content.ReadFromJsonAsync<ExerciseResponse>(TestJson.Options, _ct))!;

        var problem = await ReadProblemAsync(await PostAsync(other, Url, PastWorkout(new LoggedExerciseRequest(secret.Id, null, [Set(8, 20)]))));

        problem.Errors["exercises"].ShouldBe(["One or more exercises don't exist or were removed from the library."]);
        (await other.GetAsync($"/api/exercises/{secret.Id}/history", _ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
