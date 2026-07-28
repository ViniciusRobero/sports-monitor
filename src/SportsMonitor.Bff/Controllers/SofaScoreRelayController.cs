using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SportsMonitor.Bff.Services;
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
        [FromServices] ISnapshotStore store,
        [FromServices] IMatchResolver resolver,
        [FromServices] IOptions<RelayOptions> relayOptions,
        [FromServices] RelayStatusTracker relayStatus)
    {
        var key = relayOptions.Value.AgentKey;
        if (string.IsNullOrWhiteSpace(key))
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { error = "SofaScore relay is disabled until RelayOptions:AgentKey is configured." });

        Request.Headers.TryGetValue("X-Agent-Key", out var provided);
        if (!KeysMatch(key, provided.ToString()))
            return Unauthorized(new { error = "Invalid or missing X-Agent-Key header." });

        if (matches is null)
            return BadRequest(new { error = "Request body must be an array of normalized matches." });

        foreach (var match in matches)
        {
            // Re-resolve matchId using the BFF's current resolver so that
            // LocalAgent versions using different normalization logic still group
            // correctly with other sources (e.g. old agents that included competition in hash).
            var resolvedId = resolver.ResolveMatchId(
                match.MatchId, match.Source,
                match.HomeTeam, match.AwayTeam,
                match.KickOff, match.Competition);
            store.Upsert(resolvedId != match.MatchId ? match with { MatchId = resolvedId } : match);
        }

        relayStatus.RecordSuccess(matches.Count);
        return Ok(new { count = matches.Count });
    }

    private static bool KeysMatch(string expected, string provided)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var providedBytes = Encoding.UTF8.GetBytes(provided);
        return expectedBytes.Length == providedBytes.Length
            && CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }
}

public class RelayOptions
{
    public string AgentKey { get; set; } = "";
}
