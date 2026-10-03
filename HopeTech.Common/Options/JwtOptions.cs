using System.ComponentModel.DataAnnotations;

namespace HopeTech.Common.Options;

/// <summary>
/// JWT settings. The API both issues and validates its own access tokens
/// (Docs/DECISIONS.md D-0005), so the signing key is shared between the two and is a
/// secret: it belongs in user secrets locally and in the host's configuration provider
/// everywhere else.
///
/// Validated at startup - a missing key or audience stops the process rather than
/// producing an API that silently accepts nothing, or worse, signs with a weak key.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Authentication:Jwt";

    /// <summary>Value written to, and required in, the <c>iss</c> claim.</summary>
    [Required(AllowEmptyStrings = false)]
    public string Issuer { get; init; } = string.Empty;

    /// <summary>Value written to, and required in, the <c>aud</c> claim.</summary>
    [Required(AllowEmptyStrings = false)]
    public string Audience { get; init; } = string.Empty;

    /// <summary>
    /// Symmetric signing key for HMAC-SHA256. 32 bytes is the minimum the algorithm
    /// accepts, so a shorter key is a configuration error rather than a weak choice.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    [MinLength(32, ErrorMessage = "The JWT signing key must be at least 32 characters.")]
    public string SigningKey { get; init; } = string.Empty;

    /// <summary>How long an issued access token stays valid.</summary>
    [Range(1, 1440)]
    public int AccessTokenLifetimeMinutes { get; init; } = 60;

    /// <summary>Claim type holding the caller's roles.</summary>
    [Required(AllowEmptyStrings = false)]
    public string RoleClaimType { get; init; } = "roles";

    /// <summary>Claim type holding the caller's stable user id.</summary>
    [Required(AllowEmptyStrings = false)]
    public string UserIdClaimType { get; init; } = "sub";
}
