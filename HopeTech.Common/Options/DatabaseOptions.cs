using System.ComponentModel.DataAnnotations;

namespace HopeTech.Common.Options;

/// <summary>
/// Persistence settings. The connection string is a secret: it comes from user
/// secrets locally and from the host's configuration provider everywhere else.
/// </summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    [Required(AllowEmptyStrings = false)]
    public string ConnectionString { get; init; } = string.Empty;

    [Range(1, 600)]
    public int CommandTimeoutSeconds { get; init; } = 30;

    /// <summary>Never enable outside local development - it logs parameter values.</summary>
    public bool EnableSensitiveDataLogging { get; init; }
}
