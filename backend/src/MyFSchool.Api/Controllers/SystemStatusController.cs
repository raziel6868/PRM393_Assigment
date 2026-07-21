using Microsoft.AspNetCore.Mvc;
using MyFSchool.Api.Contracts;
using MyFSchool.Application.Readiness;

namespace MyFSchool.Api.Controllers;

[ApiController]
public sealed class SystemStatusController(IReadinessProbe readinessProbe) : ControllerBase
{
    [HttpGet("/health")]
    [ProducesResponseType<HealthResponse>(StatusCodes.Status200OK)]
    public ActionResult<HealthResponse> Health()
    {
        return Ok(new HealthResponse("healthy", HttpContext.TraceIdentifier));
    }

    [HttpGet("/ready")]
    [ProducesResponseType<ReadinessResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ReadinessResponse>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ReadinessResponse>> Ready(CancellationToken cancellationToken)
    {
        var report = await readinessProbe.CheckAsync(cancellationToken);
        var response = new ReadinessResponse(
            report.IsReady ? "ready" : "notReady",
            report.Components
                .Select(component => new ReadinessComponentResponse(
                    component.Name,
                    component.IsReady ? "ready" : "notReady"))
                .ToArray(),
            HttpContext.TraceIdentifier);

        return report.IsReady
            ? Ok(response)
            : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
    }
}
