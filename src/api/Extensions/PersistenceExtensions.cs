using Coffeeg.Api.Data;
using Coffeeg.Api.Interfaces.Repositories;
using Coffeeg.Api.Options;
using Coffeeg.Api.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Coffeeg.Api.Extensions;

/// <summary>
/// Data access registration. Repositories are registered here too, so no other
/// layer ever sees a <see cref="CoffeegDbContext"/>.
/// </summary>
public static class PersistenceExtensions
{
    public static IServiceCollection AddCoffeegPersistence(this IServiceCollection services)
    {
        services.AddDbContext<CoffeegDbContext>((serviceProvider, builder) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;

            builder.UseSqlServer(options.ConnectionString, sqlServer =>
            {
                sqlServer.CommandTimeout(options.CommandTimeoutSeconds);
                sqlServer.EnableRetryOnFailure();
                sqlServer.MigrationsHistoryTable("__EFMigrationsHistory", "dbo");
            });

            builder.EnableSensitiveDataLogging(options.EnableSensitiveDataLogging);
        });

        // Repository registrations go here, one line per port.
        services.AddScoped<IUserRepository, IdentityUserRepository>();
        services.AddScoped<IBeverageTypeRepository, BeverageTypeRepository>();
        services.AddScoped<IBeveragePreferenceRepository, BeveragePreferenceRepository>();

        return services;
    }
}
