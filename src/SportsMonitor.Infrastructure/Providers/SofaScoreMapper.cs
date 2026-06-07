using System.Runtime.CompilerServices;
using System.Text.Json;
using SportsMonitor.Domain.Interfaces;
using SportsMonitor.Domain.Models;

[assembly: InternalsVisibleTo("SportsMonitor.Tests")]

namespace SportsMonitor.Infrastructure.Providers;

internal static class SofaScoreMapper
{
    internal const string SourceName = "sofascore";

    internal static NormalizedMatch? MapMatch(JsonElement ev, string incidentsJson, string sourceId, IMatchResolver resolver)
    {
        var homeTeam = ev.GetProperty("homeTeam").GetProperty("name").GetString() ?? "";
        var awayTeam = ev.GetProperty("awayTeam").GetProperty("name").GetString() ?? "";
        var competition = ev.TryGetProperty("tournament", out var t)
            ? t.GetProperty("name").GetString() ?? "" : "";

        var kickOff = ev.TryGetProperty("startTimestamp", out var ts)
            ? DateTimeOffset.FromUnixTimeSeconds(ts.GetInt64()).UtcDateTime
            : DateTime.UtcNow;

        var homeScore = 0;
        var awayScore = 0;
        if (ev.TryGetProperty("homeScore", out var hs) && hs.TryGetProperty("current", out var hc))
            homeScore = hc.GetInt32();
        if (ev.TryGetProperty("awayScore", out var aws) && aws.TryGetProperty("current", out var ac))
            awayScore = ac.GetInt32();

        var status = MapStatus(ev);
        var matchId = resolver.ResolveMatchId(sourceId, SourceName, homeTeam, awayTeam, kickOff, competition);
        var matchEvents = MapIncidents(incidentsJson, homeTeam);

        return new NormalizedMatch(
            matchId, homeTeam, awayTeam, competition,
            kickOff, homeScore, awayScore, status,
            matchEvents, SourceName, DateTime.UtcNow, incidentsJson
        );
    }

    internal static IReadOnlyList<MatchEvent> MapIncidents(string incidentsJson, string homeTeam)
    {
        using var doc = JsonDocument.Parse(incidentsJson);
        if (!doc.RootElement.TryGetProperty("incidents", out var incidents))
            return [];

        return incidents.EnumerateArray()
            .Select(MapIncident)
            .Where(e => e is not null)
            .Cast<MatchEvent>()
            .ToList();
    }

    internal static MatchStatus MapStatus(JsonElement ev)
    {
        if (!ev.TryGetProperty("status", out var status)) return MatchStatus.Live;

        var type = status.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "";
        var description = status.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "";

        return type switch
        {
            "notstarted" => MatchStatus.NotStarted,
            "finished" => MatchStatus.Finished,
            "postponed" => MatchStatus.Postponed,
            "cancelled" or "canceled" => MatchStatus.Cancelled,
            "inprogress" when description.Contains("Halftime", StringComparison.OrdinalIgnoreCase)
                           || description.Equals("HT", StringComparison.OrdinalIgnoreCase)
                => MatchStatus.HalfTime,
            "inprogress" => MatchStatus.Live,
            _ => status.TryGetProperty("code", out var c) ? c.GetInt32() switch
            {
                0 => MatchStatus.NotStarted,
                7 => MatchStatus.HalfTime,
                100 => MatchStatus.Finished,
                60 => MatchStatus.Postponed,
                70 => MatchStatus.Cancelled,
                _ => MatchStatus.Live
            } : MatchStatus.Live
        };
    }

    private static MatchEvent? MapIncident(JsonElement incident)
    {
        var incidentType = incident.TryGetProperty("incidentType", out var it) ? it.GetString() : null;
        var eventType = incidentType switch
        {
            "goal" => EventType.Goal,
            "card" => MapCardType(incident),
            "substitution" => EventType.Substitution,
            _ => (EventType?)null
        };

        if (eventType is null) return null;

        if (incidentType == "goal")
        {
            var incidentClass = incident.TryGetProperty("incidentClass", out var ic) ? ic.GetString() : null;
            if (incidentClass == "ownGoal") eventType = EventType.OwnGoal;
            if (incidentClass == "penalty") eventType = EventType.Penalty;
        }

        var minute = incident.TryGetProperty("time", out var time) ? time.GetInt32() : 0;
        var playerName = incident.TryGetProperty("player", out var p)
            ? p.TryGetProperty("name", out var pn) ? pn.GetString() ?? "" : ""
            : "";
        var isHome = incident.TryGetProperty("isHome", out var ih) && ih.GetBoolean();
        var team = isHome ? "home" : "away";

        return new MatchEvent(eventType.Value, minute, playerName, team);
    }

    private static EventType MapCardType(JsonElement incident)
    {
        var incidentClass = incident.TryGetProperty("incidentClass", out var ic) ? ic.GetString() : null;
        return incidentClass == "red" || incidentClass == "yellowRed" ? EventType.RedCard : EventType.YellowCard;
    }
}
