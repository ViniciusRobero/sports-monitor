using Microsoft.AspNetCore.Mvc;
using SportsMonitor.Domain.Interfaces;
using SportsMonitor.Domain.Models;

namespace SportsMonitor.Bff.Controllers;

[ApiController]
[Route("api/relay")]
public class SofaScoreRelayController : ControllerBase
{
    [HttpPost("sofascore")]
    public IActionResult Relay(
        [FromBody] List<NormalizedMatch>? matches,
        [FromServices] ISnapshotStore store)
    {
        if (matches is null)
            return BadRequest(new { error = "Request body must be an array of normalized matches." });

        foreach (var match in matches)
            store.Upsert(match);

        return Ok(new { count = matches.Count });
    }
}
