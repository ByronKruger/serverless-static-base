using Platform.Api.Data;
using Platform.Common.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Composition root (Docs/ARCHITECTURE.md: DI composition root is Program.cs only).
// Registration and pipeline both come from Platform.Common, so a different host can reuse
// them verbatim - see Docs/DECISIONS.md D-0001 and D-0021.
//
// This assembly is passed on purpose. Validator and AutoMapper discovery scan one assembly
// at a time, so without it the library would scan only itself: this host's validators and
// profiles would never be registered, every route would still answer, and nothing would
// fail at startup to say so (D-0022).
builder.Services.AddPlatformApi<AppDbContext>(builder.Configuration, typeof(Program).Assembly);

var app = builder.Build();

app.UsePlatformApiPipeline();

await app.RunAsync();

/// <summary>
/// Exposed so <c>tests/Api.Tests</c> can boot the real pipeline through
/// <c>WebApplicationFactory&lt;Program&gt;</c>.
/// </summary>
public partial class Program;
