using System.Text.Json;
using System.Text.Json.Serialization;

namespace Coffeeg.Api.Tests;

/// <summary>
/// How the API actually serializes, for tests that read a response back into a DTO.
///
/// <c>ReadFromJsonAsync</c> defaults to <see cref="JsonSerializerDefaults.Web"/>, which
/// is case-insensitive but still refuses a string where an enum is expected. The API
/// writes enums as strings (Docs/ARCHITECTURE.md), so without the converter every
/// response carrying a <c>UnitKind</c> fails to parse - a test-side gap, not an API one.
/// </summary>
internal static class ApiJson
{
    public static readonly JsonSerializerOptions Options = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());

        return options;
    }
}
