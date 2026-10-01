using Platform.Api.Dtos;
using Platform.Common.Abstractions;
using Platform.Common.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Platform.Api.Controllers;

/// <summary>
/// Liveness probe. Deliberately anonymous and deliberately free of any business
/// meaning - it exists so hosts, smoke tests, and the API integration suite have a
/// stable endpoint to hit.
/// </summary>
public sealed class HealthController(IClock clock, IHostEnvironment environment) : ApiControllerBase
{
    /// <summary>Reports that the process is up.</summary>
    [HttpGet]
    [AllowAnonymous]
    [EndpointName("GetHealth")]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    public ActionResult<HealthResponse> Get() =>
        Ok(new HealthResponse("Healthy", environment.EnvironmentName, clock.UtcNow));
}
