namespace Platform.Api.Dtos;

/// <summary>Liveness payload returned by <c>GET /api/health</c>.</summary>
/// <param name="Status">Always <c>"Healthy"</c> when the process can serve requests.</param>
/// <param name="Environment">The hosting environment name.</param>
/// <param name="CheckedAtUtc">When the check ran, UTC.</param>
public sealed record HealthResponse(string Status, string Environment, DateTimeOffset CheckedAtUtc);
