namespace Platform.Common.Contracts;

/// <summary>
/// One autocomplete match. The smallest thing that identifies a person to somebody
/// looking for them.
///
/// There is deliberately no email. Email is the sign-in credential (D-0009), and a search
/// that returned it would let anyone confirm which addresses are registered - the same
/// leak <c>auth.invalid_credentials</c> exists to prevent.
/// </summary>
/// <param name="Id">Surrogate key. What the profile route is keyed on.</param>
/// <param name="UserName">Unique display name. Always present.</param>
/// <param name="FirstName">Given name, or <c>null</c> on an account predating D-0016.</param>
/// <param name="LastName">Family name, or <c>null</c> on an account predating D-0016.</param>
public sealed record UserSummaryResponse(
    int Id,
    string UserName,
    string? FirstName,
    string? LastName);
