using System.Reflection;
using System.Text.Json.Serialization;
using HopeTech.Common.Identity;
using HopeTech.Common.Errors;
using HopeTech.Common.Http;
using HopeTech.Common.Time;
using HopeTech.Common.Tokens;
using HopeTech.Common.Abstractions;
using HopeTech.Common.Options;
using HopeTech.Common.Services;
using HopeTech.Common.Validation;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.Options;
using AspNetCorsOptions = Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions;

namespace HopeTech.Common.Extensions;

/// <summary>
/// The single registration entry point. A host's <c>Program.cs</c> calls
/// <see cref="AddHopeTechApi{TDbContext}"/> and nothing else, which keeps the composition
/// root one line long and the same line in every product.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers everything the library composes, over the product's own context.
    /// </summary>
    /// <typeparam name="TDbContext">
    /// The product's context. Constrained to <see cref="HopeTechIdentityDbContext"/>
    /// because Identity's EF stores and <c>AddDbContext</c> both need the concrete type.
    /// </typeparam>
    /// <param name="services">The container.</param>
    /// <param name="configuration">Configuration, for the validated options objects.</param>
    /// <param name="featureAssemblies">
    /// Assemblies holding the product's validators and AutoMapper profiles - normally just
    /// <c>typeof(Program).Assembly</c>. **Passing nothing is a silent failure**: discovery
    /// scans one assembly at a time, so the product's validators would never be registered
    /// and its requests would never be validated, with nothing failing at startup to say
    /// so (Docs/DECISIONS.md D-0009).
    /// </param>
    public static IServiceCollection AddHopeTechApi<TDbContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        params Assembly[] featureAssemblies)
        where TDbContext : HopeTechIdentityDbContext
    {
        // This assembly always, the product's on top. Distinct, so a host that passes the
        // library's own assembly does not register every validator twice.
        Assembly[] scanned = [.. new[] { typeof(ServiceCollectionExtensions).Assembly }
            .Concat(featureAssemblies)
            .Distinct()];

        return services
            .AddHopeTechOptions(configuration)
            .AddHopeTechPresentation()
            .AddHopeTechValidation(scanned)
            .AddHopeTechMapping(scanned)
            .AddHopeTechPersistence<TDbContext>()
            .AddHopeTechIdentity<TDbContext>()
            .AddHopeTechAuthentication()
            .AddHopeTechAuthorization()
            .AddHopeTechCors()
            .AddHopeTechOpenApi()
            .AddHopeTechPorts()
            .AddHopeTechApplicationServices();
    }

    /// <summary>
    /// Binds every options object and validates it at startup: a missing secret or a
    /// malformed section stops the process instead of failing on the first request.
    /// </summary>
    private static IServiceCollection AddHopeTechOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<CorsOptions>()
            .Bind(configuration.GetSection(CorsOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }

    /// <summary>Controllers, JSON contract, and the error model.</summary>
    private static IServiceCollection AddHopeTechPresentation(this IServiceCollection services)
    {
        services
            .AddControllers(mvc =>
            {
                mvc.Filters.Add<FluentValidationFilter>();

                // [controller] becomes the REST noun: OrderLinesController -> order-lines.
                mvc.Conventions.Add(new RouteTokenTransformerConvention(new KebabCaseParameterTransformer()));
            })
            .AddJsonOptions(json =>
            {
                json.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                json.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                json.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;
            context.ProblemDetails.Extensions["traceId"] =
                System.Diagnostics.Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
        });

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddHttpContextAccessor();

        return services;
    }

    /// <summary>
    /// One validator per request type. Scanned per assembly, because
    /// <c>AddValidatorsFromAssembly</c> takes one at a time.
    /// </summary>
    private static IServiceCollection AddHopeTechValidation(this IServiceCollection services, Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);
        }

        return services;
    }

    /// <summary>AutoMapper profiles, from the library and from the product.</summary>
    private static IServiceCollection AddHopeTechMapping(this IServiceCollection services, Assembly[] assemblies)
    {
        services.AddAutoMapper(configuration => configuration.AddMaps(assemblies));
        return services;
    }

    /// <summary>Ports that every feature leans on; feature ports are registered next to their service.</summary>
    private static IServiceCollection AddHopeTechPorts(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
        services.AddSingleton<IAccessTokenFactory, JwtAccessTokenFactory>();
        return services;
    }

    /// <summary>
    /// The services the library itself provides. A product registers its own
    /// <c>I{Resource}Service</c> pairs in its own host, not here.
    /// </summary>
    private static IServiceCollection AddHopeTechApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }

    private static IServiceCollection AddHopeTechCors(this IServiceCollection services)
    {
        services.AddSingleton<IConfigureOptions<AspNetCorsOptions>, ConfigureCorsPolicy>();
        services.AddCors();
        return services;
    }
}
