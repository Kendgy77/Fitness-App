using FitnessApp.Api.Features.Auth;
using FitnessApp.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace FitnessApp.Api.Infrastructure.Auth;

public static class AuthSetup
{
    public const string RoleClaimType = "role";

    public static IServiceCollection AddAuth(this IServiceCollection services)
    {
        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Identity: users, roles, password hashing (PBKDF2) and lockout. No cookies: the API uses JWTs.
        services.AddIdentityCore<AppUser>(o =>
            {
                o.User.RequireUniqueEmail = true;

                o.Password.RequiredLength = 8;
                o.Password.RequireUppercase = true;
                o.Password.RequireLowercase = true;
                o.Password.RequireDigit = true;
                o.Password.RequireNonAlphanumeric = false;

                // WF02: three failed attempts lock the account for 15 minutes.
                o.Lockout.MaxFailedAccessAttempts = 3;
                o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                o.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager()
            .AddErrorDescriber<PolishIdentityErrorDescriber>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
            {
                // Keep claim names as in the token ("sub", "role") instead of long legacy URIs.
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Value.Issuer,
                    ValidAudience = jwt.Value.Audience,
                    IssuerSigningKey = JwtTokenService.CreateSigningKey(jwt.Value.Key),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = JwtRegisteredClaimNames.Email,
                    RoleClaimType = RoleClaimType,
                };
            });

        // Secure by default: every endpoint requires a logged-in user unless it says [AllowAnonymous].
        // Forgetting [Authorize] on a new controller can't open a hole.
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}
