using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Routing;

namespace Platform.Common.Http;

/// <summary>
/// Turns the <c>[controller]</c> route token into the REST noun: <c>OrderLines</c>
/// becomes <c>order-lines</c>. Without it the URL would inherit C# casing and the
/// resource names in Docs/domain would not match the routes.
/// </summary>
public sealed partial class KebabCaseParameterTransformer : IOutboundParameterTransformer
{
    public string? TransformOutbound(object? value)
    {
        var name = value?.ToString();

        return string.IsNullOrEmpty(name)
            ? null
            : WordBoundary().Replace(name, "$1-$2").ToLowerInvariant();
    }

    [GeneratedRegex("([a-z0-9])([A-Z])")]
    private static partial Regex WordBoundary();
}
