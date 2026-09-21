using System.Globalization;
using System.Security.Claims;
using Coffeeg.Api.Interfaces;
using Coffeeg.Api.Options;
using Microsoft.Extensions.Options;

namespace Coffeeg.Api.Helpers;

/// <summary>
/// Reads the caller off the current <see cref="HttpContext"/>. This is the only
/// place outside the pipeline that touches ASP.NET identity primitives.
/// </summary>
public sealed class HttpContextCurrentUser(
    IHttpContextAccessor httpContextAccessor,
    IOptions<JwtOptions> jwtOptions) : ICurrentUser
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public int? UserKey =>
        int.TryParse(
            Principal?.FindFirstValue(_jwt.UserIdClaimType),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var userKey)
            ? userKey
            : null;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(string role) => Principal?.IsInRole(role) ?? false;
}
