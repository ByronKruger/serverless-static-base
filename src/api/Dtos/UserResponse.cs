namespace Coffeeg.Api.Dtos;

/// <summary>The signed-in person, as the client sees them.</summary>
/// <param name="Id">Surrogate key. Stable, and safe to put in a URL.</param>
/// <param name="Email">The person's email - unique, and their natural key.</param>
/// <param name="UserName">Display name shown in the navigation header.</param>
/// <param name="Roles">Role names the UI uses to hide what the caller cannot do.</param>
public sealed record UserResponse(
    int Id,
    string Email,
    string UserName,
    IReadOnlyCollection<string> Roles);
