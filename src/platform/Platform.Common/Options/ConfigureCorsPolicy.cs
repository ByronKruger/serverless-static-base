using Microsoft.Extensions.Options;
using AspNetCorsOptions = Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions;

namespace Platform.Common.Options;

/// <summary>
/// Builds the single named CORS policy from validated configuration, so the
/// allowed origins are resolved after <c>ValidateOnStart</c> has had its say.
/// </summary>
internal sealed class ConfigureCorsPolicy(IOptions<CorsOptions> corsOptions)
    : IConfigureOptions<AspNetCorsOptions>
{
    public void Configure(AspNetCorsOptions options) =>
        options.AddPolicy(CorsOptions.PolicyName, policy => policy
            .WithOrigins(corsOptions.Value.AllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
}
