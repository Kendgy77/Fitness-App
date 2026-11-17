using FitnessApp.Api.Infrastructure.Auth;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace FitnessApp.UnitTests.Auth;

public sealed class JwtTokenServiceTests
{
    private static readonly JwtOptions Options = new()
    {
        Key = "unit-tests-signing-key-not-a-secret-0123456789",
        Issuer = "test-issuer",
        Audience = "test-audience",
        ExpiryMinutes = 60,
    };

    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 11, 20, 18, 0, 0, TimeSpan.Zero));

    private JwtTokenService CreateService() => new(Microsoft.Extensions.Options.Options.Create(Options), _clock);

    [Fact]
    public void Token_carries_user_id_email_and_roles()
    {
        var token = CreateService().CreateToken("user-1", "ala@test.local", ["User", "Admin"]);

        var jwt = new JsonWebToken(token.Value);
        jwt.Subject.ShouldBe("user-1");
        jwt.GetClaim(JwtRegisteredClaimNames.Email).Value.ShouldBe("ala@test.local");
        jwt.Claims.Where(c => c.Type == AuthSetup.RoleClaimType).Select(c => c.Value).ShouldBe(["User", "Admin"]);
        jwt.Issuer.ShouldBe("test-issuer");
        jwt.Audiences.ShouldBe(["test-audience"]);
    }

    [Fact]
    public void Token_expires_after_the_configured_minutes()
    {
        var token = CreateService().CreateToken("user-1", "ala@test.local", []);

        token.ExpiresAt.ShouldBe(_clock.GetUtcNow().AddMinutes(60));
        new JsonWebToken(token.Value).ValidTo.ShouldBe(_clock.GetUtcNow().AddMinutes(60).UtcDateTime);
    }

    [Fact]
    public void Each_token_has_a_unique_id()
    {
        var service = CreateService();

        var first = new JsonWebToken(service.CreateToken("user-1", "a@test.local", []).Value).Id;
        var second = new JsonWebToken(service.CreateToken("user-1", "a@test.local", []).Value).Id;

        first.ShouldNotBe(second);
    }

    [Fact]
    public async Task Token_signature_validates_only_with_the_same_key()
    {
        var token = CreateService().CreateToken("user-1", "a@test.local", []);
        var handler = new JsonWebTokenHandler();

        var withRightKey = await handler.ValidateTokenAsync(token.Value, Parameters(Options.Key));
        var withWrongKey = await handler.ValidateTokenAsync(token.Value, Parameters("another-key-of-sufficient-length-0123456789"));

        withRightKey.IsValid.ShouldBeTrue();
        withWrongKey.IsValid.ShouldBeFalse();
    }

    private static TokenValidationParameters Parameters(string key) => new()
    {
        ValidIssuer = Options.Issuer,
        ValidAudience = Options.Audience,
        IssuerSigningKey = JwtTokenService.CreateSigningKey(key),
        // The token was issued at the fake "now"; lifetime is covered by the test above.
        ValidateLifetime = false,
    };
}
