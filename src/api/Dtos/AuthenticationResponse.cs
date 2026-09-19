namespace Coffeeg.Api.Dtos;

/// <summary>Returned by both register and login: a token plus who it belongs to.</summary>
/// <param name="AccessToken">Signed JWT to send as <c>Authorization: Bearer</c>.</param>
/// <param name="ExpiresAtUtc">When the token stops being accepted. UTC.</param>
/// <param name="User">The authenticated person.</param>
public sealed record AuthenticationResponse(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc,
    UserResponse User);
