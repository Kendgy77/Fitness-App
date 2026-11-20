using System.Net;
using System.Net.Http.Json;
using FitnessApp.Api.Common;
using FitnessApp.Api.Features.Exercises;
using FitnessApp.Api.Features.Plans;
using FitnessApp.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace FitnessApp.IntegrationTests.Plans;

/// <summary>WF05: training plans. Test IDs refer to the thesis RTM.</summary>
public sealed class PlansTests(ApiFactory factory)
{
    private const string Url = "/api/plans";
    private const int FullBodyTemplateId = 1;
    // From the seeded exercise catalog.
    private const int BenchPress = 1;
    private const int Deadlift = 10;
    private const int BackSquat = 36;
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    private static PlanRequest NewPlan(string name = "Push day", params int[] exerciseIds) => new(
        name,
        "Chest, shoulders, triceps.",
        [.. (exerciseIds.Length == 0 ? [BenchPress, BackSquat] : exerciseIds)
            .Select(id => new PlanExerciseRequest(id, TargetSets: 4, TargetReps: 8, TargetWeightKg: 60m, Notes: "Slow down"))]);

    private async Task<PlanResponse> CreateAsync(HttpClient client, PlanRequest? request = null)
    {
        var response = await client.PostAsJsonAsync(Url, request ?? NewPlan(), TestJson.Options, _ct);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<PlanResponse>(TestJson.Options, _ct))!;
    }

    private async Task<HttpResponseMessage> SetStatusAsync(HttpClient client, int id, PlanStatus status) =>
        await client.PostAsJsonAsync($"{Url}/{id}/status", new ChangePlanStatusRequest(status), TestJson.Options, _ct);

    // ---- Create and read (T09, T10) ------------------------------------------------------------------------

    [Fact]
    public async Task T09_a_plan_is_saved_with_its_exercises_in_order()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);

        var response = await client.PostAsJsonAsync(Url, NewPlan("Push day", BenchPress, Deadlift, BackSquat), TestJson.Options, _ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<PlanResponse>(TestJson.Options, _ct);
        response.Headers.Location!.ToString().ShouldEndWith($"{Url}/{created!.Id}");

        var plan = await client.GetFromJsonAsync<PlanResponse>($"{Url}/{created.Id}", TestJson.Options, _ct);
        plan!.Name.ShouldBe("Push day");
        plan.Status.ShouldBe(PlanStatus.Draft);
        plan.IsTemplate.ShouldBeFalse();
        plan.Exercises.Select(e => e.ExerciseId).ShouldBe([BenchPress, Deadlift, BackSquat]);
        plan.Exercises.Select(e => e.Order).ShouldBe([1, 2, 3]);
        var first = plan.Exercises[0];
        first.ExerciseName.ShouldBe("Barbell Bench Press");
        first.Category.ShouldBe(ExerciseCategory.Chest);
        (first.TargetSets, first.TargetReps, first.TargetWeightKg, first.Notes).ShouldBe((4, 8, 60m, "Slow down"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task T10_a_plan_without_a_name_returns_400_and_is_not_saved(string name)
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);

        var response = await client.PostAsJsonAsync(Url, NewPlan(name), TestJson.Options, _ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(_ct);
        problem!.Errors["Name"].ShouldContain("Enter the plan name.");
        var plans = await client.GetFromJsonAsync<PagedResponse<PlanSummaryResponse>>(Url, TestJson.Options, _ct);
        plans!.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Invalid_sets_or_reps_return_400_naming_the_row()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var request = NewPlan() with { Exercises = [new PlanExerciseRequest(BenchPress, 0, 8, null, null)] };

        var response = await client.PostAsJsonAsync(Url, request, TestJson.Options, _ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(_ct);
        problem!.Errors["Exercises[0].TargetSets"].ShouldBe(["Sets must be between 1 and 20."]);
    }

    [Fact]
    public async Task An_unknown_exercise_returns_400()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);

        var response = await client.PostAsJsonAsync(Url, NewPlan("Plan", 999_999), TestJson.Options, _ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(_ct);
        problem!.Errors.ShouldContainKey("exercises");
    }

    [Fact]
    public async Task Another_users_custom_exercise_can_not_be_put_in_a_plan()
    {
        var alice = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var bob = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var alicesExercise = await (await alice.PostAsJsonAsync("/api/exercises",
                new ExerciseRequest($"Secret move {Guid.NewGuid():N}", "", "", ExerciseCategory.Core, Difficulty.Beginner, Equipment.Other),
                TestJson.Options, _ct))
            .Content.ReadFromJsonAsync<ExerciseResponse>(TestJson.Options, _ct);

        var response = await bob.PostAsJsonAsync(Url, NewPlan("Plan", alicesExercise!.Id), TestJson.Options, _ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_draft_without_exercises_can_be_saved()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);

        var plan = await CreateAsync(client, NewPlan() with { Exercises = [] });

        plan.Exercises.ShouldBeEmpty();
    }

    [Fact]
    public async Task The_list_shows_own_plans_newest_change_first_and_filters_by_status()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var older = await CreateAsync(client, NewPlan("Older"));
        var newer = await CreateAsync(client, NewPlan("Newer"));
        await SetStatusAsync(client, older.Id, PlanStatus.Active);

        var all = await client.GetFromJsonAsync<PagedResponse<PlanSummaryResponse>>(Url, TestJson.Options, _ct);
        var active = await client.GetFromJsonAsync<PagedResponse<PlanSummaryResponse>>($"{Url}?status=Active", TestJson.Options, _ct);

        // Activating "Older" changed it most recently, so it is now first.
        all!.Items.Select(p => p.Id).ShouldBe([older.Id, newer.Id]);
        all.Items[0].ExerciseCount.ShouldBe(2);
        active!.Items.ShouldHaveSingleItem().Id.ShouldBe(older.Id);
    }

    // ---- Edit -------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Saving_replaces_the_name_and_the_exercise_list_in_the_new_order()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var plan = await CreateAsync(client, NewPlan("Day A", BenchPress, BackSquat));

        var response = await client.PutAsJsonAsync($"{Url}/{plan.Id}", NewPlan("Day A v2", BackSquat, Deadlift, BenchPress), TestJson.Options, _ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<PlanResponse>(TestJson.Options, _ct);
        updated!.Name.ShouldBe("Day A v2");
        updated.Exercises.Select(e => e.ExerciseId).ShouldBe([BackSquat, Deadlift, BenchPress]);
        updated.Exercises.Select(e => e.Order).ShouldBe([1, 2, 3]);
    }

    [Fact]
    public async Task An_active_plan_can_still_be_edited()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var plan = await CreateAsync(client);
        await SetStatusAsync(client, plan.Id, PlanStatus.Active);

        var response = await client.PutAsJsonAsync($"{Url}/{plan.Id}", NewPlan("Renamed"), TestJson.Options, _ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task An_archived_plan_is_read_only_until_restored()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var plan = await CreateAsync(client);
        await SetStatusAsync(client, plan.Id, PlanStatus.Archived);

        (await client.PutAsJsonAsync($"{Url}/{plan.Id}", NewPlan("Nope"), TestJson.Options, _ct)).StatusCode
            .ShouldBe(HttpStatusCode.Conflict);

        (await SetStatusAsync(client, plan.Id, PlanStatus.Draft)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.PutAsJsonAsync($"{Url}/{plan.Id}", NewPlan("Back again"), TestJson.Options, _ct)).StatusCode
            .ShouldBe(HttpStatusCode.OK);
    }

    // ---- Status -----------------------------------------------------------------------------------------------

    [Fact]
    public async Task A_plan_goes_from_draft_to_active_to_archived()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var plan = await CreateAsync(client);

        var activated = await (await SetStatusAsync(client, plan.Id, PlanStatus.Active)).Content.ReadFromJsonAsync<PlanResponse>(TestJson.Options, _ct);
        var archived = await (await SetStatusAsync(client, plan.Id, PlanStatus.Archived)).Content.ReadFromJsonAsync<PlanResponse>(TestJson.Options, _ct);

        activated!.Status.ShouldBe(PlanStatus.Active);
        archived!.Status.ShouldBe(PlanStatus.Archived);
    }

    [Fact]
    public async Task An_empty_plan_can_not_be_activated()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var plan = await CreateAsync(client, NewPlan() with { Exercises = [] });

        var response = await SetStatusAsync(client, plan.Id, PlanStatus.Active);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(_ct);
        problem!.Errors["status"].ShouldBe(["Add at least one exercise before activating the plan."]);
    }

    [Fact]
    public async Task A_disallowed_change_returns_409()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var plan = await CreateAsync(client);
        await SetStatusAsync(client, plan.Id, PlanStatus.Archived);

        var response = await SetStatusAsync(client, plan.Id, PlanStatus.Active);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Asking_for_the_current_status_again_is_harmless()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var plan = await CreateAsync(client);

        (await SetStatusAsync(client, plan.Id, PlanStatus.Draft)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ---- Delete -----------------------------------------------------------------------------------------------

    [Fact]
    public async Task A_draft_can_be_deleted()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var plan = await CreateAsync(client);

        (await client.DeleteAsync($"{Url}/{plan.Id}", _ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await client.GetAsync($"{Url}/{plan.Id}", _ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task A_plan_that_was_active_is_archived_instead_of_deleted()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var plan = await CreateAsync(client);
        await SetStatusAsync(client, plan.Id, PlanStatus.Active);

        (await client.DeleteAsync($"{Url}/{plan.Id}", _ct)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    // ---- Templates --------------------------------------------------------------------------------------------

    [Fact]
    public async Task Three_templates_are_available_to_every_user()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);

        var templates = await client.GetFromJsonAsync<PagedResponse<PlanSummaryResponse>>($"{Url}/templates", TestJson.Options, _ct);

        templates!.Items.Count.ShouldBe(3);
        templates.Items.ShouldAllBe(t => t.IsTemplate && t.ExerciseCount > 0);
        var fullBody = await client.GetFromJsonAsync<PlanResponse>($"{Url}/{FullBodyTemplateId}", TestJson.Options, _ct);
        fullBody!.Name.ShouldBe("Full Body – Beginner");
        fullBody.Exercises.Count.ShouldBe(6);
    }

    [Fact]
    public async Task Templates_are_not_in_the_users_own_list()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);

        var mine = await client.GetFromJsonAsync<PagedResponse<PlanSummaryResponse>>(Url, TestJson.Options, _ct);

        mine!.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Copying_a_template_creates_an_own_draft_with_the_same_exercises()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);

        var response = await client.PostAsync($"{Url}/{FullBodyTemplateId}/copy", null, _ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var copy = await response.Content.ReadFromJsonAsync<PlanResponse>(TestJson.Options, _ct);
        copy!.Id.ShouldBeGreaterThanOrEqualTo(1000);
        copy.IsTemplate.ShouldBeFalse();
        copy.Status.ShouldBe(PlanStatus.Draft);
        copy.Name.ShouldBe("Full Body – Beginner");
        copy.Exercises.Count.ShouldBe(6);
    }

    [Fact]
    public async Task Copying_an_own_plan_marks_the_copy_in_its_name()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var plan = await CreateAsync(client, NewPlan("Leg day"));

        var copy = await (await client.PostAsync($"{Url}/{plan.Id}/copy", null, _ct)).Content.ReadFromJsonAsync<PlanResponse>(TestJson.Options, _ct);

        copy!.Name.ShouldBe("Leg day (copy)");
        copy.Exercises.Select(e => e.ExerciseId).ShouldBe(plan.Exercises.Select(e => e.ExerciseId));
    }

    [Fact]
    public async Task Templates_can_not_be_changed_by_users()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);

        (await client.PutAsJsonAsync($"{Url}/{FullBodyTemplateId}", NewPlan(), TestJson.Options, _ct)).StatusCode
            .ShouldBe(HttpStatusCode.Forbidden);
        (await SetStatusAsync(client, FullBodyTemplateId, PlanStatus.Archived)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await client.DeleteAsync($"{Url}/{FullBodyTemplateId}", _ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // ---- Privacy ----------------------------------------------------------------------------------------------

    [Fact]
    public async Task Another_users_plan_does_not_exist_for_you()
    {
        var alice = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var bob = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var plan = await CreateAsync(alice);

        (await bob.GetAsync($"{Url}/{plan.Id}", _ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await bob.PutAsJsonAsync($"{Url}/{plan.Id}", NewPlan(), TestJson.Options, _ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await SetStatusAsync(bob, plan.Id, PlanStatus.Active)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await bob.PostAsync($"{Url}/{plan.Id}/copy", null, _ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await bob.DeleteAsync($"{Url}/{plan.Id}", _ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var bobsPlans = await bob.GetFromJsonAsync<PagedResponse<PlanSummaryResponse>>(Url, TestJson.Options, _ct);
        bobsPlans!.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Anonymous_callers_get_401()
    {
        var client = factory.CreateClient();

        (await client.GetAsync(Url, _ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.GetAsync($"{Url}/templates", _ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
