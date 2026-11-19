using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace FitnessApp.Api.Infrastructure.Auth;

/// <summary>Limits on anonymous auth endpoints, per client IP. Slows down password guessing across accounts.</summary>
public sealed class RateLimitSettings
{
    public const string SectionName = "RateLimit";

    public int AuthPermitLimit { get; init; } = 10;
    public int AuthWindowSeconds { get; init; } = 60;
}

public static class RateLimitingSetup
{
    public const string AuthPolicy = "auth";

    public static IServiceCollection AddRateLimiting(this IServiceCollection services)
    {
        services.AddOptions<RateLimitSettings>().BindConfiguration(RateLimitSettings.SectionName);
        services.AddRateLimiter(_ => { });
        services.AddOptions<RateLimiterOptions>()
            .Configure<IOptions<RateLimitSettings>>((limiter, settings) =>
            {
                limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                limiter.AddPolicy(AuthPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = settings.Value.AuthPermitLimit,
                        Window = TimeSpan.FromSeconds(settings.Value.AuthWindowSeconds),
                        QueueLimit = 0,
                    }));
                limiter.OnRejected = async (context, ct) =>
                {
                    var problemDetails = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
                    await problemDetails.WriteAsync(new ProblemDetailsContext
                    {
                        HttpContext = context.HttpContext,
                        ProblemDetails =
                        {
                            Status = StatusCodes.Status429TooManyRequests,
                            Title = "Too many attempts. Wait a moment and try again.",
                        },
                    });
                };
            });
        return services;
    }
}
