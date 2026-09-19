using System.Text;
using Coffeeg.Api.Data;
using Coffeeg.Api.Entities;
using Coffeeg.Api.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Coffeeg.Api.Extensions;

/// <summary>
/// JWT bearer authentication, validated against the same symmetric key the API signs
/// with (Docs/DECISIONS.md D-0009). There is no OIDC metadata discovery: this API is
/// its own issuer.
/// </summary>
public static class AuthenticationExtensions
{
    public static IServiceCollection AddCoffeegAuthentication(this IServiceCollection services)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
            {
                var options = jwt.Value;

                // Claims arrive as written: no legacy mapping of `sub` to a WS-Fed URI.
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = options.Issuer,
                    ValidateAudience = true,
                    ValidAudience = options.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
                    RoleClaimType = options.RoleClaimType,
                    NameClaimType = options.UserIdClaimType,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        return services;
    }

    /// <summary>
    /// ASP.NET Identity for user and role storage. <c>AddIdentityCore</c> rather than
    /// <c>AddIdentity</c>: this is an API, so it needs the managers and none of the
    /// cookie authentication that <c>AddIdentity</c> would register alongside them.
    /// </summary>
    public static IServiceCollection AddCoffeegIdentity(this IServiceCollection services)
    {
        services
            .AddIdentityCore<AppUser>(options =>
            {
                // Email is this system's natural key for a person, so Identity must
                // treat it as unique too - not just the database index.
                options.User.RequireUniqueEmail = true;

                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;

                options.Lockout.MaxFailedAccessAttempts = 10;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<AppRole>()
            .AddEntityFrameworkStores<CoffeegDbContext>();

        return services;
    }
}
