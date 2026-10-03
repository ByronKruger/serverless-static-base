namespace HopeTech.Api.Dtos;

/// <summary>
/// The body of <c>POST /api/widgets</c>. One request type per verb - never one
/// <c>WidgetDto</c> shared by create, update and read.
/// </summary>
/// <param name="Name">What the widget is called.</param>
/// <param name="Description">Optional free text.</param>
public sealed record CreateWidgetRequest(string Name, string? Description);
