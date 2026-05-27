using Microsoft.AspNetCore.Mvc;
using SportsMonitor.Infrastructure.Stores;

namespace SportsMonitor.Bff.Controllers;

[ApiController]
[Route("api/google-results")]
public class GoogleController : ControllerBase
{
    [HttpGet]
    public IActionResult GetAll([FromServices] GoogleSnapshotStore store) =>
        Ok(store.GetAll());

    [HttpGet("{matchId}")]
    public IActionResult GetForMatch(string matchId, [FromServices] GoogleSnapshotStore store)
    {
        var snap = store.GetForMatch(matchId);
        return snap is null ? NotFound() : Ok(snap);
    }
}
