using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SportsMonitor.Bff.Services;
using SportsMonitor.Domain.Models;
using SportsMonitor.Workers.Base;

namespace SportsMonitor.Bff.Controllers;

[ApiController]
[Route("api/providers")]
public class ProvidersController : ControllerBase
{
    [HttpGet("status")]
    public IActionResult GetStatus(
        [FromServices] IEnumerable<PollingWorker> workers,
        [FromServices] RelayStatusTracker relayStatus,
        [FromServices] IOptions<RelayOptions> relayOptions)
    {
        var statuses = workers.Select(w => w.GetStatus()).ToList();
        statuses.Add(relayStatus.GetStatus(
            !string.IsNullOrWhiteSpace(relayOptions.Value.AgentKey)));
        return Ok(statuses);
    }
}
