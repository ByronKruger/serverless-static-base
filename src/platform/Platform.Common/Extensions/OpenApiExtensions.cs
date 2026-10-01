using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace Platform.Common.Extensions;

/// <summary>
/// OpenAPI document generation. Every action documents its status codes; the
/// operationId of an existing action never changes (Docs/ARCHITECTURE.md).
/// </summary>
public static class OpenApiExtensions
{
    public static IServiceCollection AddPlatformOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());
        return services;
    }
}

/// <summary>Declares the bearer scheme so generated clients and doc UIs can authenticate.</summary>
internal sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider schemeProvider)
    : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var schemes = await schemeProvider.GetAllSchemesAsync();
        if (schemes.All(scheme => scheme.Name != "Bearer"))
        {
            return;
        }

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT access token issued by the configured authority."
        };
    }
}
