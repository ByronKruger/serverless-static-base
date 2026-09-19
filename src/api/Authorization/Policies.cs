namespace Coffeeg.Api.Authorization;

/// <summary>
/// Named authorization policies. Endpoints reference these constants -
/// never an ad-hoc role string (Docs/ARCHITECTURE.md).
/// </summary>
public static class Policies
{
    /// <summary>Administrators only.</summary>
    public const string RequireAdministrator = nameof(RequireAdministrator);

    /// <summary>Moderators and administrators.</summary>
    public const string RequireModerator = nameof(RequireModerator);
}
