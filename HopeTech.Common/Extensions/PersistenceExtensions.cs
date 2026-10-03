using HopeTech.Common.Identity;
using HopeTech.Common.Abstractions;
using HopeTech.Common.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HopeTech.Common.Extensions;

/// <summary>
/// Data access registration. Repositories are registered here too, so no other
/// layer ever sees a <see cref="HopeTechIdentityDbContext"/>.
/// </summary>
public static class PersistenceExtensions
{
    /// <summary>
    /// Registers the product's context against the configured provider, plus the library's
    /// own ports over it.
    /// </summary>
    /// <typeparam name="TDbContext">
    /// The product's context. <c>AddDbContext</c> needs the concrete type, which is why
    /// this is generic rather than taking the base class.
    /// </typeparam>
    public static IServiceCollection AddHopeTechPersistence<TDbContext>(this IServiceCollection services)
        where TDbContext : HopeTechIdentityDbContext
    {
        services.AddDbContext<TDbContext>((serviceProvider, builder) =>
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

        // The library's own port. A product registers its repositories in its own host.
        services.AddScoped<IUserRepository, IdentityUserRepository>();

        return services;
    }
}
