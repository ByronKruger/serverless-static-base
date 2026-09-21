namespace Coffeeg.Api.Dtos;

/// <summary>
/// One person as anybody signed in may see them: who they are, and how they like their
/// drinks (D-0017).
///
/// <paramref name="Preferences"/> reuses <see cref="BeveragePreferenceResponse"/> rather
/// than declaring a narrower shape of its own. That DTO carries nothing owner-only, and a
/// second read shape for the same data is how two readers of it drift apart. Like
/// <see cref="UserSummaryResponse"/>, this carries no email.
/// </summary>
/// <param name="Id">Surrogate key.</param>
/// <param name="UserName">Unique display name. Always present.</param>
/// <param name="FirstName">Given name, or <c>null</c> on an account predating D-0016.</param>
/// <param name="LastName">Family name, or <c>null</c> on an account predating D-0016.</param>
/// <param name="CreatedAtUtc">When the account was registered. UTC.</param>
/// <param name="Preferences">Their saved preferences, newest first. Empty, never null.</param>
public sealed record UserProfileResponse(
    int Id,
    string UserName,
    string? FirstName,
    string? LastName,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyCollection<BeveragePreferenceResponse> Preferences);
