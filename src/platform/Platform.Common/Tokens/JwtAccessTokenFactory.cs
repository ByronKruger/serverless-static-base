using System.Globalization;
using System.Text;
using Platform.Common.Identity;
using Platform.Common.Abstractions;
using Platform.Common.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Platform.Common.Tokens;

/// <summary>
/// Signs access tokens with the symmetric key from <see cref="JwtOptions"/> - the same
/// key the bearer handler validates against, since the API issues its own tokens.
/// </summary>
public sealed class JwtAccessTokenFactory(IOptions<JwtOptions> jwtOptions, IClock clock) : IAccessTokenFactory
{
    private readonly JwtOptions _options = jwtOptions.Value;

    public AccessToken Create(AppUser user, IReadOnlyCollection<string> roles)
    {
        var issuedAt = clock.UtcNow;
        var expiresAt = issuedAt.AddMinutes(_options.AccessTokenLifetimeMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = issuedAt.UtcDateTime,
            NotBefore = issuedAt.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            Claims = new Dictionary<string, object>
            {
                [_options.UserIdClaimType] = user.Id.ToString(CultureInfo.InvariantCulture),
                [_options.RoleClaimType] = roles.ToArray(),
                [JwtRegisteredClaimNames.Email] = user.Email ?? string.Empty,
                [JwtRegisteredClaimNames.PreferredUsername] = user.UserName ?? string.Empty,
                // Distinct per token, so an individual token can be denylisted later.
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString()
            },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
                SecurityAlgorithms.HmacSha256)
        };

        return new AccessToken(new JsonWebTokenHandler().CreateToken(descriptor), expiresAt);
    }
}
