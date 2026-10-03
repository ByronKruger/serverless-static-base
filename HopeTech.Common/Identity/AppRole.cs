using Microsoft.AspNetCore.Identity;

namespace HopeTech.Common.Identity;

/// <summary>
/// A role, keyed by <see cref="int"/> to match <see cref="AppUser"/>. The rows are
/// seeded from <see cref="Authorization.Roles"/> so the names in code and in the
/// database cannot drift.
/// </summary>
public class AppRole : IdentityRole<int>;
