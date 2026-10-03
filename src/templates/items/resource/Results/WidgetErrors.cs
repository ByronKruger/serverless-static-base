using HopeTech.Common.Results;

namespace HopeTech.Api.Results;

/// <summary>
/// The widget error codes. A code earns a named class here once it has two callers;
/// while it has one, it stays a private member of the service that returns it.
///
/// Codes are stable and machine-readable - a client branches on <c>code</c> in the
/// problem+json body, never on the human-readable title.
/// </summary>
public static class WidgetErrors
{
    public static Error NotFound(int id) =>
        Error.NotFound("widget.not_found", $"No widget with id {id} exists.");

    public static Error NameTaken(string name) =>
        Error.Conflict("widget.name_taken", $"A widget called '{name}' already exists.");
}
