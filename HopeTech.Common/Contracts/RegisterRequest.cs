namespace HopeTech.Common.Contracts;

/// <summary>Body of <c>POST /api/auth/register</c>.</summary>
/// <param name="Email">The person's email. Unique across all users, and their natural key.</param>
/// <param name="UserName">Display name shown in the UI. Also unique, but not a key.</param>
/// <param name="FirstName">Given name. Required here, though nullable on the row - see D-0007.</param>
/// <param name="LastName">Family name. Required here, though nullable on the row - see D-0007.</param>
/// <param name="Password">Plain-text password. Hashed by Identity; never stored or logged as-is.</param>
public sealed record RegisterRequest(
    string Email,
    string UserName,
    string FirstName,
    string LastName,
    string Password);
