using Microsoft.AspNetCore.Mvc;
using SportsMonitor.Domain.Interfaces;
using SportsMonitor.Domain.Models;

namespace SportsMonitor.Bff.Controllers;

[ApiController]
[Route("api/divergences")]
public class DivergencesController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetRecent(
        [FromServices] IMatchHistoryRepository repository,
        CancellationToken ct,
        int limit = 50)
    {
        var divergences = await repository.GetRecentDivergencesAsync(limit, ct);
        return Ok(divergences);
    }

    [HttpPost("{id:guid}/verify")]
    public async Task<IActionResult> Verify(
        Guid id,
        [FromBody] VerificationUpdate update,
        [FromServices] IMatchHistoryRepository repository,
        CancellationToken ct)
    {
        await repository.UpdateVerificationAsync(id, update, ct);
        return NoContent();
    }
}
