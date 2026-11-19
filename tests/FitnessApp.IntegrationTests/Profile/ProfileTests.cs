using System.Net;
using System.Net.Http.Json;
using FitnessApp.Api.Features.Profile;
using FitnessApp.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace FitnessApp.IntegrationTests.Profile;

/// <summary>WF03 (profile) and WF10 (BMI). Test IDs refer to the thesis RTM.</summary>
public sealed class ProfileTests(ApiFactory factory)
{
    private const string Url = "/api/profile";
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    private static UpdateProfileRequest FullProfile() => new(
        BirthDate: new DateOnly(1999, 4, 12),
        Sex: Sex.Male,
        HeightCm: 183m,
        WeightKg: 78m,
        Goal: TrainingGoal.BuildMuscle,
        TimeZone: "Europe/Warsaw");

    [Fact]
    public async Task A_new_user_gets_an_empty_profile()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);

        var profile = await client.GetFromJsonAsync<ProfileResponse>(Url, TestJson.Options, _ct);

        profile!.WeightKg.ShouldBeNull();
        profile.Bmi.ShouldBeNull();
        profile.TimeZone.ShouldBe("Europe/Warsaw");
        profile.MissingForBmi.ShouldBe(["weightKg", "heightCm"]);
        profile.UpdatedAtUtc.ShouldBeNull();
    }

    [Fact]
    public async Task T05_saved_profile_data_is_returned_on_the_next_read()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);

        var put = await client.PutAsJsonAsync(Url, FullProfile(), TestJson.Options, _ct);
        put.StatusCode.ShouldBe(HttpStatusCode.OK);

        var profile = await client.GetFromJsonAsync<ProfileResponse>(Url, TestJson.Options, _ct);
        profile!.BirthDate.ShouldBe(new DateOnly(1999, 4, 12));
        profile.Age.ShouldNotBeNull();
        profile.Age.Value.ShouldBeGreaterThanOrEqualTo(27);
        profile.Sex.ShouldBe(Sex.Male);
        profile.HeightCm.ShouldBe(183m);
        profile.WeightKg.ShouldBe(78m);
        profile.Goal.ShouldBe(TrainingGoal.BuildMuscle);
        profile.UpdatedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task Saving_again_replaces_the_profile_and_null_clears_a_field()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        await client.PutAsJsonAsync(Url, FullProfile(), TestJson.Options, _ct);

        var put = await client.PutAsJsonAsync(Url, FullProfile() with { WeightKg = 80.5m, Goal = null }, TestJson.Options, _ct);

        var profile = await put.Content.ReadFromJsonAsync<ProfileResponse>(TestJson.Options, _ct);
        profile!.WeightKg.ShouldBe(80.5m);
        profile.Goal.ShouldBeNull();
    }

    [Theory]
    [InlineData(-70)]
    [InlineData(0)]
    [InlineData(19.9)]
    [InlineData(400.1)]
    public async Task T06_impossible_body_weight_returns_400_and_saves_nothing(decimal weightKg)
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        await client.PutAsJsonAsync(Url, FullProfile(), TestJson.Options, _ct);

        var put = await client.PutAsJsonAsync(Url, FullProfile() with { WeightKg = weightKg }, TestJson.Options, _ct);

        put.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await put.Content.ReadFromJsonAsync<ValidationProblemDetails>(_ct);
        problem!.Errors["WeightKg"].ShouldBe(["Masa ciała musi wynosić od 20 do 400 kg."]);

        var profile = await client.GetFromJsonAsync<ProfileResponse>(Url, TestJson.Options, _ct);
        profile!.WeightKg.ShouldBe(78m);
    }

    [Theory]
    [InlineData(99)]
    [InlineData(251)]
    public async Task Impossible_height_returns_400(decimal heightCm)
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);

        var put = await client.PutAsJsonAsync(Url, FullProfile() with { HeightCm = heightCm }, TestJson.Options, _ct);

        put.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_birth_date_in_the_future_returns_400_on_the_birthDate_field()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var future = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);

        var put = await client.PutAsJsonAsync(Url, FullProfile() with { BirthDate = future }, TestJson.Options, _ct);

        put.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await put.Content.ReadFromJsonAsync<ValidationProblemDetails>(_ct);
        problem!.Errors["birthDate"].ShouldBe(["Podaj poprawną datę urodzenia (wiek od 13 do 120 lat)."]);
    }

    [Fact]
    public async Task An_unknown_time_zone_returns_400()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);

        var put = await client.PutAsJsonAsync(Url, FullProfile() with { TimeZone = "Mars/Olympus_Mons" }, TestJson.Options, _ct);

        put.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await put.Content.ReadFromJsonAsync<ValidationProblemDetails>(_ct);
        problem!.Errors.ShouldContainKey("timeZone");
    }

    [Theory]
    [InlineData("\"Robot\"")]
    [InlineData("1")] // enums are accepted by name only, so a number like 7 can't become an undefined value
    public async Task An_unknown_or_numeric_enum_value_returns_400_not_500(string sexJson)
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        using var body = new StringContent($"{{\"sex\": {sexJson}, \"heightCm\": 180}}", System.Text.Encoding.UTF8, "application/json");

        var put = await client.PutAsync(Url, body, _ct);

        put.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task T16_bmi_is_calculated_from_profile_weight_and_height()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);

        var put = await client.PutAsJsonAsync(Url, FullProfile(), TestJson.Options, _ct);

        var profile = await put.Content.ReadFromJsonAsync<ProfileResponse>(TestJson.Options, _ct);
        profile!.Bmi.ShouldBe(new BmiResult(23.3m, BmiCategory.Normal));
        profile.MissingForBmi.ShouldBeEmpty();
    }

    [Fact]
    public async Task T17_without_height_bmi_is_not_calculated_and_the_missing_field_is_named()
    {
        var client = await factory.CreateAuthenticatedClientAsync(ct: _ct);

        var put = await client.PutAsJsonAsync(Url, FullProfile() with { HeightCm = null }, TestJson.Options, _ct);

        put.StatusCode.ShouldBe(HttpStatusCode.OK);
        var profile = await put.Content.ReadFromJsonAsync<ProfileResponse>(TestJson.Options, _ct);
        profile!.Bmi.ShouldBeNull();
        profile.MissingForBmi.ShouldBe(["heightCm"]);
    }

    [Fact]
    public async Task A_user_never_sees_another_users_profile()
    {
        var alice = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        var bob = await factory.CreateAuthenticatedClientAsync(ct: _ct);
        await alice.PutAsJsonAsync(Url, FullProfile(), TestJson.Options, _ct);

        var bobsProfile = await bob.GetFromJsonAsync<ProfileResponse>(Url, TestJson.Options, _ct);

        bobsProfile!.WeightKg.ShouldBeNull();
        bobsProfile.BirthDate.ShouldBeNull();
    }

    [Fact]
    public async Task Anonymous_callers_get_401()
    {
        var client = factory.CreateClient();

        (await client.GetAsync(Url, _ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.PutAsJsonAsync(Url, FullProfile(), TestJson.Options, _ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
