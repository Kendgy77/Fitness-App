using System.Net;
using System.Net.Http.Json;
using FitnessApp.Api.Common;
using FitnessApp.Api.Features.Exercises;
using FitnessApp.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace FitnessApp.IntegrationTests.Exercises;

/// <summary>
/// WF04: the exercise library. Test IDs refer to the thesis RTM. Tests share one database and run in parallel,
/// so they never assert exact totals of the global catalog (admin tests may add to it); they assert properties.
/// </summary>
public sealed class ExercisesTests(ApiFactory factory)
{
    private const string Url = "/api/exercises";
    private const string AdminUrl = "/api/admin/exercises";
    private const int BenchPressId = 1; // "Barbell Bench Press" from the seeded catalog
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    private static ExerciseRequest NewExercise(string? name = null) => new(
        Name: name ?? $"Exercise {Guid.NewGuid():N}",
        Description: "Technique.",
        Muscles: "Glutes",
        Category: ExerciseCategory.Legs,
        Difficulty: Difficulty.Beginner,
        Equipment: Equipment.Bodyweight);

    private async Task<PagedResponse<ExerciseResponse>> SearchAsync(HttpClient client, string queryString = "")
    {
        var page = await client.GetFromJsonAsync<PagedResponse<ExerciseResponse>>($"{Url}{queryString}", TestJson.Options, _ct);
        return page!;
    }

    private async Task<ExerciseResponse> CreateAsync(HttpClient client, ExerciseRequest request, string url = Url)
    {
        var response = await client.PostAsJsonAsync(url, request, TestJson.Options, _ct);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ExerciseResponse>(TestJson.Options, _ct))!;
    }

    // ---- Browsing (T07, T08) -------------------------------------------------------------------------------

    [Fact]
    public async Task T07_library_lists_exercises_with_descriptions_and_muscles()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);

        var page = await SearchAsync(client);

        page.TotalCount.ShouldBeGreaterThanOrEqualTo(60);
        page.Items.Count.ShouldBe(20);
        page.Items.ShouldAllBe(e => e.Description.Length > 0 && e.Muscles.Length > 0);
        page.Items.Select(e => e.Name).ShouldBeInOrder(SortDirection.Ascending, StringComparer.Create(
            System.Globalization.CultureInfo.InvariantCulture, ignoreCase: false));
    }

    [Fact]
    public async Task T08_filtering_by_category_returns_only_that_category()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);

        var page = await SearchAsync(client, "?category=Chest&pageSize=100");

        page.TotalCount.ShouldBeGreaterThanOrEqualTo(9);
        page.Items.ShouldAllBe(e => e.Category == ExerciseCategory.Chest);
    }

    [Fact]
    public async Task Filters_combine()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);

        var page = await SearchAsync(client, "?category=Legs&equipment=Machine&difficulty=Beginner&pageSize=100");

        page.Items.ShouldNotBeEmpty();
        page.Items.ShouldAllBe(e => e.Category == ExerciseCategory.Legs
            && e.Equipment == Equipment.Machine && e.Difficulty == Difficulty.Beginner);
    }

    [Theory]
    [InlineData("bench press")]
    [InlineData("BENCH PRESS")]   // different case
    [InlineData("arbell bench")]  // part of a word
    public async Task Search_ignores_case_and_matches_parts_of_the_name(string search)
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);

        var page = await SearchAsync(client, $"?search={Uri.EscapeDataString(search)}");

        page.Items.Select(e => e.Id).ShouldContain(BenchPressId);
    }

    [Fact]
    public async Task Search_ignores_accents_in_custom_exercise_names()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var exercise = await CreateAsync(client, NewExercise($"Wiosłowanie TRX {Guid.NewGuid():N}"));

        var page = await SearchAsync(client, "?search=wioslowanie%20trx&onlyCustom=true");

        page.Items.ShouldHaveSingleItem().Id.ShouldBe(exercise.Id);
    }

    [Fact]
    public async Task Search_also_matches_muscles()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);

        var page = await SearchAsync(client, "?search=triceps&pageSize=100");

        page.Items.ShouldNotBeEmpty();
        page.Items.ShouldAllBe(e => e.Muscles.Contains("riceps") || e.Name.Contains("riceps"));
    }

    [Fact]
    public async Task Like_wildcards_in_the_search_are_treated_as_text()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);

        var page = await SearchAsync(client, $"?search={Uri.EscapeDataString("%_")}");

        page.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Paging_returns_consecutive_pages_without_overlap()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);

        var first = await SearchAsync(client, "?category=Back&pageSize=5&page=1");
        var second = await SearchAsync(client, "?category=Back&pageSize=5&page=2");

        first.Items.Count.ShouldBe(5);
        second.Items.ShouldNotBeEmpty();
        first.Items.Select(e => e.Id).Intersect(second.Items.Select(e => e.Id)).ShouldBeEmpty();
        first.TotalPages.ShouldBe((int)Math.Ceiling(first.TotalCount / 5.0));
    }

    [Theory]
    [InlineData("?pageSize=101")]
    [InlineData("?page=0")]
    [InlineData("?category=Neck")]
    public async Task Invalid_query_returns_400(string queryString)
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);

        var response = await client.GetAsync($"{Url}{queryString}", _ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_global_exercise_can_be_read_by_id()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);

        var exercise = await client.GetFromJsonAsync<ExerciseResponse>($"{Url}/{BenchPressId}", TestJson.Options, _ct);

        exercise!.Name.ShouldBe("Barbell Bench Press");
        exercise.IsCustom.ShouldBeFalse();
    }

    [Fact]
    public async Task Unknown_id_returns_404()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);

        (await client.GetAsync($"{Url}/999999", _ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Anonymous_callers_get_401()
    {
        var client = factory.CreateClient();

        (await client.GetAsync(Url, _ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ---- Custom exercises ----------------------------------------------------------------------------------

    [Fact]
    public async Task A_user_can_add_a_custom_exercise_and_find_it()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var request = NewExercise();

        var response = await client.PostAsJsonAsync(Url, request, TestJson.Options, _ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<ExerciseResponse>(TestJson.Options, _ct);
        created!.IsCustom.ShouldBeTrue();
        response.Headers.Location!.ToString().ShouldEndWith($"{Url}/{created.Id}");
        created.Id.ShouldBeGreaterThanOrEqualTo(1000);

        var mine = await SearchAsync(client, "?onlyCustom=true");
        mine.Items.ShouldHaveSingleItem().Name.ShouldBe(request.Name);
    }

    [Fact]
    public async Task A_custom_exercise_is_invisible_and_untouchable_for_other_users()
    {
        var alice = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var bob = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var exercise = await CreateAsync(alice, NewExercise());

        (await bob.GetAsync($"{Url}/{exercise.Id}", _ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await bob.PutAsJsonAsync($"{Url}/{exercise.Id}", NewExercise(), TestJson.Options, _ct)).StatusCode
            .ShouldBe(HttpStatusCode.NotFound);
        (await bob.DeleteAsync($"{Url}/{exercise.Id}", _ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var bobsSearch = await SearchAsync(bob, $"?search={Uri.EscapeDataString(exercise.Name)}");
        bobsSearch.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task A_name_already_in_the_catalog_returns_409_whatever_the_case()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);

        var response = await client.PostAsJsonAsync(Url,
            NewExercise("BARBELL BENCH PRESS"), TestJson.Options, _ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(_ct);
        problem!.Title.ShouldBe("An exercise with this name already exists.");
    }

    [Fact]
    public async Task Two_users_may_each_have_a_custom_exercise_with_the_same_name()
    {
        var name = $"My exercise {Guid.NewGuid():N}";
        var alice = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var bob = await factory.CreateAuthenticatedClientAsync(ct: _ct);

        await CreateAsync(alice, NewExercise(name));
        await CreateAsync(bob, NewExercise(name));
    }

    [Fact]
    public async Task Missing_category_returns_400_instead_of_a_default_value()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);

        var response = await client.PostAsJsonAsync(Url, NewExercise() with { Category = null }, TestJson.Options, _ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(_ct);
        problem!.Errors["Category"].ShouldBe(["Choose a category."]);
    }

    [Fact]
    public async Task A_user_can_edit_their_custom_exercise()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var exercise = await CreateAsync(client, NewExercise());
        var changed = NewExercise() with { Difficulty = Difficulty.Advanced };

        var response = await client.PutAsJsonAsync($"{Url}/{exercise.Id}", changed, TestJson.Options, _ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<ExerciseResponse>(TestJson.Options, _ct);
        updated!.Name.ShouldBe(changed.Name);
        updated.Difficulty.ShouldBe(Difficulty.Advanced);
    }

    [Fact]
    public async Task Users_can_not_change_global_exercises()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);

        (await client.PutAsJsonAsync($"{Url}/{BenchPressId}", NewExercise(), TestJson.Options, _ct)).StatusCode
            .ShouldBe(HttpStatusCode.Forbidden);
        (await client.DeleteAsync($"{Url}/{BenchPressId}", _ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Archiving_hides_an_exercise_from_the_library_but_keeps_it_readable()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var exercise = await CreateAsync(client, NewExercise());

        (await client.DeleteAsync($"{Url}/{exercise.Id}", _ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await SearchAsync(client, "?onlyCustom=true")).Items.ShouldBeEmpty();
        var archived = await client.GetFromJsonAsync<ExerciseResponse>($"{Url}/{exercise.Id}", TestJson.Options, _ct);
        archived!.IsArchived.ShouldBeTrue();

        (await client.PutAsJsonAsync($"{Url}/{exercise.Id}", NewExercise(), TestJson.Options, _ct)).StatusCode
            .ShouldBe(HttpStatusCode.Conflict);
        (await client.DeleteAsync($"{Url}/{exercise.Id}", _ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task An_archived_name_can_be_used_again()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var exercise = await CreateAsync(client, NewExercise());
        await client.DeleteAsync($"{Url}/{exercise.Id}", _ct);

        await CreateAsync(client, NewExercise(exercise.Name));
    }

    // ---- Administrator -------------------------------------------------------------------------------------

    [Fact]
    public async Task Regular_users_can_not_use_admin_endpoints()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);

        (await client.PostAsJsonAsync(AdminUrl, NewExercise(), TestJson.Options, _ct)).StatusCode
            .ShouldBe(HttpStatusCode.Forbidden);
        (await client.DeleteAsync($"{AdminUrl}/{BenchPressId}", _ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task An_admin_adds_a_global_exercise_that_every_user_sees()
    {
        var admin = await factory.CreateAdminClientAsync(_ct);
        var user = await factory.CreateAuthenticatedClientAsync(ct: _ct);

        var created = await CreateAsync(admin, NewExercise(), AdminUrl);

        created.IsCustom.ShouldBeFalse();
        var seen = await user.GetFromJsonAsync<ExerciseResponse>($"{Url}/{created.Id}", TestJson.Options, _ct);
        seen!.Name.ShouldBe(created.Name);
    }

    [Fact]
    public async Task An_admin_edits_and_archives_a_global_exercise()
    {
        var admin = await factory.CreateAdminClientAsync(_ct);
        var created = await CreateAsync(admin, NewExercise(), AdminUrl);
        var changed = NewExercise() with { Category = ExerciseCategory.Core };

        var put = await admin.PutAsJsonAsync($"{AdminUrl}/{created.Id}", changed, TestJson.Options, _ct);
        put.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await put.Content.ReadFromJsonAsync<ExerciseResponse>(TestJson.Options, _ct))!.Category.ShouldBe(ExerciseCategory.Core);

        (await admin.DeleteAsync($"{AdminUrl}/{created.Id}", _ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var search = await SearchAsync(admin, $"?search={Uri.EscapeDataString(changed.Name)}");
        search.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task An_admin_can_not_touch_a_users_private_exercise()
    {
        var admin = await factory.CreateAdminClientAsync(_ct);
        var user = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var custom = await CreateAsync(user, NewExercise());

        (await admin.PutAsJsonAsync($"{AdminUrl}/{custom.Id}", NewExercise(), TestJson.Options, _ct)).StatusCode
            .ShouldBe(HttpStatusCode.NotFound);
        (await admin.DeleteAsync($"{AdminUrl}/{custom.Id}", _ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
