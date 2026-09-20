using System.Text.Json.Serialization;
using Coffeeg.Api.Exceptions;
using Coffeeg.Api.Helpers;
using Coffeeg.Api.Interfaces;
using Coffeeg.Api.Interfaces.Services;
using Coffeeg.Api.Options;
using Coffeeg.Api.Services;
using Coffeeg.Api.Validation;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.Options;
using AspNetCorsOptions = Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions;

namespace Coffeeg.Api.Extensions;

/// <summary>
/// The single registration entry point for the API. Program.cs calls
/// <see cref="AddCoffeegApi"/> and nothing else, which keeps the composition root
/// one line long and reusable from another host.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCoffeegApi(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddCoffeegOptions(configuration)
            .AddCoffeegPresentation()
            .AddCoffeegValidation()
            .AddCoffeegMapping()
            .AddCoffeegPersistence()
            .AddCoffeegIdentity()
            .AddCoffeegAuthentication()
            .AddCoffeegAuthorization()
            .AddCoffeegCors()
            .AddCoffeegOpenApi()
            .AddCoffeegPlatformServices()
            .AddCoffeegApplicationServices();

    /// <summary>
    /// Binds every options object and validates it at startup: a missing secret or a
    /// malformed section stops the process instead of failing on the first request.
    /// </summary>
    private static IServiceCollection AddCoffeegOptions(this IServiceCollection services, IConfiguration configuration)
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
    private static IServiceCollection AddCoffeegPresentation(this IServiceCollection services)
    {
        services
            .AddControllers(mvc =>
            {
                mvc.Filters.Add<FluentValidationFilter>();

                // [controller] becomes the REST noun: BeverageTypesController -> beverage-types.
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

    /// <summary>One validator per request type, discovered from this assembly.</summary>
    private static IServiceCollection AddCoffeegValidation(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(ServiceCollectionExtensions).Assembly, includeInternalTypes: true);
        return services;
    }

    /// <summary>AutoMapper profiles from <c>MapperProfiles/</c>.</summary>
    private static IServiceCollection AddCoffeegMapping(this IServiceCollection services)
    {
        services.AddAutoMapper(configuration =>
            configuration.AddMaps(typeof(ServiceCollectionExtensions).Assembly));
        return services;
    }

    /// <summary>Ports that every feature leans on; feature ports are registered next to their service.</summary>
    private static IServiceCollection AddCoffeegPlatformServices(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
        services.AddSingleton<IAccessTokenFactory, JwtAccessTokenFactory>();
        return services;
    }

    /// <summary>One line per <c>I{Resource}Service</c>. Features add themselves here.</summary>
    private static IServiceCollection AddCoffeegApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IBeverageTypeService, BeverageTypeService>();
        services.AddScoped<IBeveragePreferenceService, BeveragePreferenceService>();
        return services;
    }

    private static IServiceCollection AddCoffeegCors(this IServiceCollection services)
    {
        services.AddSingleton<IConfigureOptions<AspNetCorsOptions>, ConfigureCorsPolicy>();
        services.AddCors();
        return services;
    }
}
