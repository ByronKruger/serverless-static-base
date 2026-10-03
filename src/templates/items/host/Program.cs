using HopeTech.Api.Data;
using HopeTech.Common.Extensions;

// Before the first run:
//   dotnet add package HopeTech.Common
//   dotnet add package Microsoft.EntityFrameworkCore.Design
//   dotnet user-secrets set "Authentication:Jwt:SigningKey" "<32+ characters>"
//   dotnet ef migrations add InitialIdentity --output-dir Data/Migrations
//   dotnet ef database update
//
// The signing key is a real secret: this API both signs and validates with it, and
// options validation stops the process when it is missing or shorter than 32 characters.
var builder = WebApplication.CreateBuilder(args);

// The only composition root. Registration and the request pipeline both come from
// HopeTech.Common, so a different host - a serverless one, say - reuses them verbatim.
//
// This assembly is passed on purpose. Validator and AutoMapper discovery scan one assembly
// at a time, so without it the library would scan only itself: this host's validators and
// profiles would never be registered, every route would still answer, and nothing would
// fail at startup to say so.
builder.Services.AddHopeTechApi<AppDbContext>(builder.Configuration, typeof(Program).Assembly);

var app = builder.Build();

app.UseHopeTechApiPipeline();

await app.RunAsync();

/// <summary>
/// Exposed so an integration test project can boot the real pipeline through
/// <c>WebApplicationFactory&lt;Program&gt;</c>.
/// </summary>
public partial class Program;
