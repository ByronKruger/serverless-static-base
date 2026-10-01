using Microsoft.AspNetCore.Mvc;

namespace Platform.Common.Contracts;

/// <summary>
/// Query string of <c>GET /api/users</c>. A transport-only contract, which is why it
/// carries binding attributes and lives here rather than being passed as loose scalars
/// (Docs/ARCHITECTURE.md).
///
/// Written with init-only properties rather than as a positional record on purpose:
/// <see cref="Validation.FluentValidationFilter"/> resolves a validator from the runtime
/// type of each bound action argument, so the query has to arrive as one bound complex
/// object for <see cref="Validation.UserSearchRequestValidator"/> to run at all.
/// Two loose <c>[FromQuery]</c> scalars would bind fine and never be validated.
/// </summary>
public sealed record UserSearchRequest
{
    /// <summary>
    /// What to look for, matched against username, first name and surname. Named <c>q</c>
    /// on the wire because that is what a search box sends.
    /// </summary>
    [FromQuery(Name = "q")]
    public string Term { get; init; } = string.Empty;

    /// <summary>
    /// How many matches to return. Absent means <see cref="DefaultLimit"/>; the validator
    /// bounds anything supplied.
    /// </summary>
    [FromQuery(Name = "limit")]
    public int? Limit { get; init; }

    /// <summary>Enough to fill a dropdown without inviting a scroll.</summary>
    public const int DefaultLimit = 10;

    /// <summary>Shortest useful term. One letter would match most of the table.</summary>
    public const int MinimumTermLength = 2;

    /// <summary>Upper bound on <see cref="Limit"/>. An autocomplete that pages is a different feature.</summary>
    public const int MaximumLimit = 25;
}
