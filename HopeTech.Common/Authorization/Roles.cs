namespace HopeTech.Common.Authorization;

/// <summary>
/// Role names as they appear in the token. See Docs/domain/user-permissions.md.
/// </summary>
public static class Roles
{
    /// <summary>Can do everything a moderator can, plus dashboards and role management.</summary>
    public const string Administrator = "Administrator";

    /// <summary>Can manage the reference data a product puts in their care.</summary>
    public const string Moderator = "Moderator";

    /// <summary>A registered person, with access to their own data and nothing else.</summary>
    public const string User = "User";
}
