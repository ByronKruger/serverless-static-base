namespace Coffeeg.Api.Results;

/// <summary>
/// An expected failure. Unexpected failures are exceptions and are handled by
/// <see cref="Exceptions.GlobalExceptionHandler"/>, not by this type.
/// </summary>
/// <param name="Code">Stable, machine-readable code, e.g. <c>preference.duplicate_beverage_type</c>.</param>
/// <param name="Description">Human-readable description safe to return to the caller.</param>
/// <param name="Type">Category that decides the HTTP status code.</param>
/// <param name="Failures">Per-field messages, for <see cref="ErrorType.Validation"/> only.</param>
public sealed record Error(
    string Code,
    string Description,
    ErrorType Type,
    IReadOnlyDictionary<string, string[]>? Failures = null)
{
    public static Error Validation(string code, string description) =>
        new(code, description, ErrorType.Validation);

    public static Error Validation(IReadOnlyDictionary<string, string[]> failures) =>
        new("validation.failed", "One or more validation errors occurred.", ErrorType.Validation, failures);

    public static Error Unauthorized(string code, string description) =>
        new(code, description, ErrorType.Unauthorized);

    public static Error Forbidden(string code, string description) =>
        new(code, description, ErrorType.Forbidden);

    public static Error NotFound(string code, string description) =>
        new(code, description, ErrorType.NotFound);

    public static Error Conflict(string code, string description) =>
        new(code, description, ErrorType.Conflict);
}
