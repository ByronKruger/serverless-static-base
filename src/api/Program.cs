using Coffeeg.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Composition root (Docs/ARCHITECTURE.md: DI composition root is Program.cs only).
// Registration and pipeline both live behind one hosting-agnostic entry point each,
// so a different host can reuse them verbatim - see Docs/DECISIONS.md D-0001.
builder.Services.AddCoffeegApi(builder.Configuration);

var app = builder.Build();

app.UseCoffeegApiPipeline();

await app.RunAsync();

/// <summary>
/// Exposed so <c>tests/Api.Tests</c> can boot the real pipeline through
/// <c>WebApplicationFactory&lt;Program&gt;</c>.
/// </summary>
public partial class Program;
