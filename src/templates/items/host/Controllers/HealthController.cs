using HopeTech.Api.Dtos;
using HopeTech.Common.Abstractions;
using HopeTech.Common.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HopeTech.Api.Controllers;

/// <summary>
/// Liveness probe. Deliberately anonymous and deliberately free of any business meaning -
/// it exists so hosts, smoke tests, and the integration suite have a stable endpoint to
/// hit.
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
