using Platform.Common.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace Platform.Common.Extensions;

/// <summary>
/// Policy-based authorization. The fallback policy makes the API default-deny:
/// an endpoint is authenticated unless it opts out with <c>[AllowAnonymous]</c>.
/// </summary>
public static class AuthorizationExtensions
{
    public static IServiceCollection AddPlatformAuthorization(this IServiceCollection services)
    {
        services
            .AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build())
            .AddPolicy(Policies.RequireAdministrator, policy => policy
                .RequireAuthenticatedUser()
                .RequireRole(Roles.Administrator))
            .AddPolicy(Policies.RequireModerator, policy => policy
                .RequireAuthenticatedUser()
                .RequireRole(Roles.Administrator, Roles.Moderator));

        return services;
    }
}
