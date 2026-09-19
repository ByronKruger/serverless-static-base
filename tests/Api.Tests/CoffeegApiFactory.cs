using Coffeeg.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Coffeeg.Api.Tests;

/// <summary>
/// Boots the real application - real pipeline, real binding, real Problem Details -
/// with configuration supplied in memory and the database swapped for an in-memory
/// provider. API tests hit HTTP; they never new up a controller.
/// </summary>
public sealed class CoffeegApiFactory : WebApplicationFactory<Program>
{
    /// <summary>
    /// Fixed per factory, and deliberately not generated inside the options callback:
    /// <c>AddDbContext</c> registers its options as scoped, so a name built in the
    /// callback would hand every scope its own empty database - including one without
    /// the seeded roles.
    /// </summary>
    private readonly string _databaseName = $"coffeeg-tests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Jwt:Issuer"] = "https://identity.coffeeg.test",
                ["Authentication:Jwt:Audience"] = "coffeeg-api",
                ["Authentication:Jwt:SigningKey"] = "test-signing-key-that-is-long-enough-for-hmac-sha256",
                ["Cors:AllowedOrigins:0"] = "https://web.coffeeg.test",
                ["Database:ConnectionString"] = "InMemory"
            }));

        builder.ConfigureTestServices(services =>
        {
            // Both the options object and the configuration callback that applied
            // UseSqlServer have to go. Leaving the callback behind registers two
            // providers in one container, which EF refuses to resolve.
            services.RemoveAll<IDbContextOptionsConfiguration<CoffeegDbContext>>();
            services.RemoveAll<DbContextOptions<CoffeegDbContext>>();
            services.RemoveAll<DbContextOptions>();

            services.AddDbContext<CoffeegDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // The in-memory store starts empty, so the roles seeded by the migration have to
        // be created here too - registration grants one, and it must exist.
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CoffeegDbContext>().Database.EnsureCreated();

        return host;
    }
}
