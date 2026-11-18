using System.Text.Json.Serialization;
using FitnessApp.Api.Common.Errors;
using FitnessApp.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Api.Common.Extensions;

/// <summary>Service registrations grouped by area, so Program.cs stays a short table of contents.</summary>
public static class ServiceCollectionExtensions
{
    public const string FrontendCorsPolicy = "Frontend";

    public static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        // The connection string is read when the DbContext is first created, not at startup,
        // so integration tests can supply their own (Testcontainers) before that happens.
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("Default");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Connection string 'ConnectionStrings:Default' is not configured.");
            }

            options.UseNpgsql(connectionString);
        });

        services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");
        return services;
    }

    public static IServiceCollection AddApiDefaults(this IServiceCollection services)
    {
        services.AddControllers()
            // Enums travel as strings ("Strength", not 0). An unknown value is rejected with 400 by
            // model binding, instead of crashing later in an Enum.Parse.
            .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddProblemDetails(o => o.CustomizeProblemDetails = ctx =>
            ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier);
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // Services ask for TimeProvider instead of calling DateTime.UtcNow, so tests can control time.
        services.AddSingleton(TimeProvider.System);
        return services;
    }

    public static IServiceCollection AddFrontendCors(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<CorsSettings>().Bind(configuration.GetSection(CorsSettings.SectionName));
        services.AddCors();
        services.AddOptions<Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions>()
            .Configure<Microsoft.Extensions.Options.IOptions<CorsSettings>>((cors, settings) =>
                cors.AddPolicy(FrontendCorsPolicy, policy => policy
                    .WithOrigins(settings.Value.AllowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()));
        return services;
    }

    public static IServiceCollection AddSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(o => o.SwaggerDoc("v1", new() { Title = "Fitness App API", Version = "v1" }));
        return services;
    }
}
