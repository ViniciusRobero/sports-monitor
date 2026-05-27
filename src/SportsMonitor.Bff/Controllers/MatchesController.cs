using Microsoft.AspNetCore.Mvc;
using SportsMonitor.Domain.Interfaces;

namespace SportsMonitor.Bff.Controllers;

[ApiController]
[Route("api/matches")]
public class MatchesController : ControllerBase
{
    [HttpGet("live")]
    public IActionResult GetLive([FromServices] ISnapshotStore store)
    {
        var matches = store.GetLiveMatchIds()
            .Select(store.GetAllForMatch)
            .ToList();

        return Ok(matches);
    }
}
