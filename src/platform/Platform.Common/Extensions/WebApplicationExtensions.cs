using Platform.Common.Options;

namespace Platform.Common.Extensions;

/// <summary>
/// The request pipeline, in one place and in one order. Program.cs calls this and
/// nothing else.
/// </summary>
public static class WebApplicationExtensions
{
    public static WebApplication UsePlatformApiPipeline(this WebApplication app)
    {
        // 1. Unexpected failures become problem+json before anything else can leak them.
        app.UseExceptionHandler();

        // 2. Body-less framework responses (401, 403, 404) get the same shape.
        app.UseStatusCodePages();

        if (app.Environment.IsDevelopment())
        {
            // Served at /openapi/v1.json. Never exposed outside development, and
            // explicitly anonymous because the default-deny fallback policy would
            // otherwise 401 the document itself.
            app.MapOpenApi().AllowAnonymous();
        }

        app.UseHttpsRedirection();

        // 3. CORS runs before auth so a rejected pre-flight still gets its headers.
        app.UseCors(CorsOptions.PolicyName);

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        return app;
    }
}
