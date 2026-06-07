using Microsoft.AspNetCore.Mvc;
using SportsMonitor.Domain.Models;
using SportsMonitor.Workers.Base;

namespace SportsMonitor.Bff.Controllers;

[ApiController]
[Route("api/providers")]
public class ProvidersController : ControllerBase
{
    [HttpGet("status")]
    public IActionResult GetStatus([FromServices] IEnumerable<PollingWorker> workers)
    {
        var statuses = workers.Select(w => w.GetStatus()).ToList();
        return Ok(statuses);
    }
}
