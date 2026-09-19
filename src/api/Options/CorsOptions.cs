using System.ComponentModel.DataAnnotations;

namespace Coffeeg.Api.Options;

/// <summary>
/// The Angular client is a separate origin, so allowed origins are configuration,
/// never a wildcard.
/// </summary>
public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    /// <summary>Policy name referenced by the request pipeline.</summary>
    public const string PolicyName = "CoffeegWebClient";

    [Required]
    [MinLength(1, ErrorMessage = "At least one allowed origin must be configured.")]
    public string[] AllowedOrigins { get; init; } = [];
}
