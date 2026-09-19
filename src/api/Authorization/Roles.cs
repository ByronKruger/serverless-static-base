namespace Coffeeg.Api.Authorization;

/// <summary>
/// Role names as they appear in the token. See Docs/domain/user-permissions.md.
/// </summary>
public static class Roles
{
    /// <summary>Can do everything a moderator can, plus dashboards and role management.</summary>
    public const string Administrator = "Administrator";

    /// <summary>Can manage beverage types and their ingredients.</summary>
    public const string Moderator = "Moderator";

    /// <summary>A registered person who manages their own preferences.</summary>
    public const string User = "User";
}
