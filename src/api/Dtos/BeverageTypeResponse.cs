namespace Coffeeg.Api.Dtos;

/// <summary>
/// A beverage type in a list: enough to pick one, without its ingredients. Fetching the
/// detail is a second call, so the picker stays small however many ingredients a type has.
/// </summary>
/// <param name="Id">Surrogate key.</param>
/// <param name="Name">Display name. "Coffee", "Whiskey".</param>
/// <param name="Description">Optional elaboration.</param>
public sealed record BeverageTypeResponse(int Id, string Name, string? Description);
