using System.Text.Json;
using System.Text.Json.Serialization;

namespace FitnessApp.IntegrationTests.Infrastructure;

/// <summary>The same JSON contract the API uses (camelCase, enums as names), for requests and responses in tests.</summary>
public static class TestJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };
}
