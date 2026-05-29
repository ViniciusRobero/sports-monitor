using Microsoft.AspNetCore.Mvc;
using SportsMonitor.Domain.Interfaces;

namespace SportsMonitor.Bff.Controllers;

[ApiController]
[Route("api/refresh")]
public class RefreshController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Refresh(
        [FromServices] IEnumerable<IRefreshable> workers,
        CancellationToken ct)
    {
        var tasks = workers.Select(w => w.CollectOnceAsync(ct));
        var counts = await Task.WhenAll(tasks);
        return Ok(new { collected = counts.Sum() });
    }
}
