namespace FitnessApp.Api.Common.Extensions;

/// <summary>Frontend origins allowed to call the API. Each environment lists only its own frontend.</summary>
public sealed class CorsSettings
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; init; } = [];
}
