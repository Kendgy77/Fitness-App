using System.Net;
using System.Net.Http.Json;
using FitnessApp.Api.Common;
using FitnessApp.Api.Features.Measurements;
using FitnessApp.Api.Features.Profile;
using FitnessApp.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace FitnessApp.IntegrationTests.Measurements;

/// <summary>WF07: body measurements over time.</summary>
public sealed class MeasurementsTests(ApiFactory factory)
{
    private const string Url = "/api/measurements";
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    private static DateOnly DaysAgo(int days) => DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-days));

    private static MeasurementRequest Entry(DateOnly day, decimal? weightKg = null, decimal? waistCm = null, decimal? bodyFat = null) =>
        new(day, weightKg, bodyFat, null, null, null, waistCm, null, null, null, null, null, null, null, null);

    private async Task<HttpClient> ClientWithHeightAsync(decimal heightCm = 180)
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var profile = new UpdateProfileRequest(null, null, heightCm, 82, null, "Europe/Warsaw");
        (await client.PutAsJsonAsync("/api/profile", profile, TestJson.Options, _ct)).EnsureSuccessStatusCode();
        return client;
    }

    private async Task<MeasurementResponse> CreateAsync(HttpClient client, MeasurementRequest request)
    {
        var response = await client.PostAsJsonAsync(Url, request, TestJson.Options, _ct);
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(_ct));
        return (await response.Content.ReadFromJsonAsync<MeasurementResponse>(TestJson.Options, _ct))!;
    }

    private async Task<decimal?> ProfileWeightAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<ProfileResponse>("/api/profile", TestJson.Options, _ct))!.WeightKg;

    [Fact]
    public async Task A_measurement_is_saved_with_the_bmi_for_its_weight()
    {
        var client = await ClientWithHeightAsync(180);

        var saved = await CreateAsync(client, Entry(DaysAgo(0), weightKg: 80, waistCm: 84.5m, bodyFat: 15.24m));

        (saved.WeightKg, saved.WaistCm, saved.BodyFatPercent).ShouldBe((80m, 84.5m, 15.2m)); // one decimal place
        saved.Bmi.ShouldBe(new BmiResult(24.7m, BmiCategory.Normal));
    }

    [Fact]
    public async Task A_newer_weight_becomes_the_profile_weight_and_an_older_one_does_not()
    {
        var client = await ClientWithHeightAsync();

        await CreateAsync(client, Entry(DaysAgo(1), weightKg: 80));
        (await ProfileWeightAsync(client)).ShouldBe(80m);

        await CreateAsync(client, Entry(DaysAgo(30), weightKg: 85)); // filling in history
        (await ProfileWeightAsync(client)).ShouldBe(80m);

        await CreateAsync(client, Entry(DaysAgo(0), waistCm: 83)); // no weight: profile untouched
        (await ProfileWeightAsync(client)).ShouldBe(80m);
    }

    [Fact]
    public async Task A_first_weight_creates_the_profile_if_there_is_none()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);

        var saved = await CreateAsync(client, Entry(DaysAgo(0), weightKg: 70));

        saved.Bmi.ShouldBeNull(); // no height yet
        (await ProfileWeightAsync(client)).ShouldBe(70m);
    }

    [Fact]
    public async Task One_entry_per_day()
    {
        var client = await ClientWithHeightAsync();
        await CreateAsync(client, Entry(DaysAgo(2), weightKg: 80));

        var second = await client.PostAsJsonAsync(Url, Entry(DaysAgo(2), waistCm: 84), TestJson.Options, _ct);

        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task An_empty_entry_a_wild_value_or_a_future_day_returns_400()
    {
        var client = await ClientWithHeightAsync();

        var empty = await client.PostAsJsonAsync(Url, Entry(DaysAgo(0)), TestJson.Options, _ct);
        var wild = await client.PostAsJsonAsync(Url, Entry(DaysAgo(0), waistCm: 400), TestJson.Options, _ct);
        var future = await client.PostAsJsonAsync(Url, Entry(DaysAgo(-3), weightKg: 80), TestJson.Options, _ct);

        (await Problem(empty)).Errors["WeightKg"].ShouldBe(["Enter at least one measurement."]);
        (await Problem(wild)).Errors["WaistCm"].ShouldBe(["Waist must be between 40 and 200 cm."]);
        (await Problem(future)).Errors["measuredOn"].ShouldBe(["Measurements can't be in the future."]);
    }

    private async Task<ValidationProblemDetails> Problem(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        return (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(_ct))!;
    }

    [Fact]
    public async Task The_list_is_newest_first_and_filters_by_date()
    {
        var client = await ClientWithHeightAsync();
        await CreateAsync(client, Entry(DaysAgo(20), weightKg: 84));
        await CreateAsync(client, Entry(DaysAgo(10), weightKg: 83));
        await CreateAsync(client, Entry(DaysAgo(0), weightKg: 82));

        var all = await client.GetFromJsonAsync<PagedResponse<MeasurementResponse>>(Url, TestJson.Options, _ct);
        all!.Items.Select(m => m.WeightKg).ShouldBe([82m, 83m, 84m]);

        var middle = await client.GetFromJsonAsync<PagedResponse<MeasurementResponse>>(
            $"{Url}?from={DaysAgo(15):yyyy-MM-dd}&to={DaysAgo(5):yyyy-MM-dd}", TestJson.Options, _ct);
        middle!.Items.ShouldHaveSingleItem().WeightKg.ShouldBe(83m);
    }

    [Fact]
    public async Task An_entry_can_be_corrected_and_deleted()
    {
        var client = await ClientWithHeightAsync();
        var saved = await CreateAsync(client, Entry(DaysAgo(0), weightKg: 80));

        var update = await client.PutAsJsonAsync($"{Url}/{saved.Id}", Entry(DaysAgo(0), weightKg: 79.5m, waistCm: 82), TestJson.Options, _ct);
        update.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ProfileWeightAsync(client)).ShouldBe(79.5m);

        (await client.DeleteAsync($"{Url}/{saved.Id}", _ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await client.GetAsync($"{Url}/{saved.Id}", _ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await ProfileWeightAsync(client)).ShouldBe(79.5m); // the profile keeps the current weight
    }

    [Fact]
    public async Task Another_users_measurements_are_not_found()
    {
        var owner = await ClientWithHeightAsync();
        var other = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var saved = await CreateAsync(owner, Entry(DaysAgo(0), weightKg: 80));

        (await other.GetAsync($"{Url}/{saved.Id}", _ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await other.PutAsJsonAsync($"{Url}/{saved.Id}", Entry(DaysAgo(0), weightKg: 60), TestJson.Options, _ct))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await other.DeleteAsync($"{Url}/{saved.Id}", _ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var list = await other.GetFromJsonAsync<PagedResponse<MeasurementResponse>>(Url, TestJson.Options, _ct);
        list!.TotalCount.ShouldBe(0);
        (await other.GetFromJsonAsync<List<CurrentMeasurement>>($"{Url}/current", TestJson.Options, _ct))!.ShouldBeEmpty();
    }

    private async Task<List<CurrentMeasurement>> SaveCurrentAsync(HttpClient client, MeasurementRequest request)
    {
        var response = await client.PutAsJsonAsync($"{Url}/current", request, TestJson.Options, _ct);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(_ct));
        return (await response.Content.ReadFromJsonAsync<List<CurrentMeasurement>>(TestJson.Options, _ct))!;
    }

    private async Task<List<MeasurementResponse>> AllAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<PagedResponse<MeasurementResponse>>(Url, TestJson.Options, _ct))!.Items.ToList();

    [Fact]
    public async Task Current_shows_each_values_newest_reading_with_its_date()
    {
        var client = await ClientWithHeightAsync();
        await CreateAsync(client, Entry(DaysAgo(10), weightKg: 84, waistCm: 90));
        await CreateAsync(client, Entry(DaysAgo(3), weightKg: 82));

        var current = await client.GetFromJsonAsync<List<CurrentMeasurement>>($"{Url}/current", TestJson.Options, _ct);

        current.ShouldBe([new("weightKg", 82m, DaysAgo(3)), new("waistCm", 90m, DaysAgo(10))]);
    }

    [Fact]
    public async Task Saving_the_profile_measurements_keeps_only_changed_values_in_todays_entry()
    {
        var client = await ClientWithHeightAsync();
        await CreateAsync(client, Entry(DaysAgo(10), weightKg: 84, waistCm: 90));

        // Weight changed, waist the same as before: today's entry gets only the weight.
        var current = await SaveCurrentAsync(client, Entry(DaysAgo(0), weightKg: 82.5m, waistCm: 90));
        current.ShouldBe([new("weightKg", 82.5m, DaysAgo(0)), new("waistCm", 90m, DaysAgo(10))]);
        var today = (await AllAsync(client))[0];
        (today.MeasuredOn, today.WeightKg, today.WaistCm).ShouldBe((DaysAgo(0), 82.5m, (decimal?)null));
        (await ProfileWeightAsync(client)).ShouldBe(82.5m);

        // Saving again the same day changes that entry instead of adding one.
        await SaveCurrentAsync(client, Entry(DaysAgo(0), weightKg: 82, waistCm: 88));
        var all = await AllAsync(client);
        all.Count.ShouldBe(2);
        (all[0].WeightKg, all[0].WaistCm).ShouldBe((82m, 88m));

        // Back to the old values: nothing differs any more, so today's entry goes away.
        current = await SaveCurrentAsync(client, Entry(DaysAgo(0), weightKg: 84, waistCm: 90));
        (await AllAsync(client)).ShouldHaveSingleItem().MeasuredOn.ShouldBe(DaysAgo(10));
        current.ShouldBe([new("weightKg", 84m, DaysAgo(10)), new("waistCm", 90m, DaysAgo(10))]);
    }

    [Fact]
    public async Task Saving_the_profile_measurements_validates_like_an_entry()
    {
        var client = await ClientWithHeightAsync();

        var wild = await client.PutAsJsonAsync($"{Url}/current", Entry(DaysAgo(0), waistCm: 400), TestJson.Options, _ct);
        wild.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var future = await client.PutAsJsonAsync($"{Url}/current", Entry(DaysAgo(-3), waistCm: 80), TestJson.Options, _ct);
        future.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await AllAsync(client)).ShouldBeEmpty();
    }
}
