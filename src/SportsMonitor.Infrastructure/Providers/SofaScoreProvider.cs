using System.Text.Json;
using SportsMonitor.Domain.Configuration;
using SportsMonitor.Domain.Interfaces;
using SportsMonitor.Domain.Models;

namespace SportsMonitor.Infrastructure.Providers;

public class SofaScoreProvider : IMatchDataProvider
{
    private readonly HttpClient _http;
    private readonly IMatchResolver _resolver;

    public SofaScoreProvider(HttpClient http, SofaScoreOptions options, IMatchResolver resolver)
    {
        _http = http;
        _resolver = resolver;
        _http.BaseAddress ??= new Uri(options.BaseUrl);
        if (!_http.DefaultRequestHeaders.Contains("User-Agent"))
            _http.DefaultRequestHeaders.Add("User-Agent", options.UserAgent);
    }

    public string Name => "sofascore";

    public async Task<IReadOnlyList<NormalizedMatch>> GetLiveMatchesAsync(CancellationToken ct)
    {
        // sport/1 = soccer
        using var response = await _http.GetAsync("/api/v1/sport/football/events/live", ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("events", out var events))
            return [];

        var tasks = events.EnumerateArray()
            .Select(ev => FetchMatchAsync(ev, ct))
            .ToList();

        var matches = await Task.WhenAll(tasks);
        return matches.Where(m => m is not null).Cast<NormalizedMatch>().ToList();
    }

    private async Task<NormalizedMatch?> FetchMatchAsync(JsonElement ev, CancellationToken ct)
    {
        var id = ev.TryGetProperty("id", out var idProp) ? idProp.GetInt64() : 0;
        if (id == 0) return null;

        using var response = await _http.GetAsync($"/api/v1/event/{id}/incidents", ct);
        if (!response.IsSuccessStatusCode) return null;

        var incidentsJson = await response.Content.ReadAsStringAsync(ct);
        return MapMatch(ev, incidentsJson, id.ToString());
    }

    private NormalizedMatch? MapMatch(JsonElement ev, string incidentsJson, string sourceId)
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
        var matchId = _resolver.ResolveMatchId(sourceId, Name, homeTeam, awayTeam, kickOff, competition);
        var events = MapIncidents(incidentsJson, homeTeam);

        return new NormalizedMatch(
            matchId, homeTeam, awayTeam, competition,
            kickOff, homeScore, awayScore, status,
            events, Name, DateTime.UtcNow, incidentsJson
        );
    }

    private static IReadOnlyList<MatchEvent> MapIncidents(string incidentsJson, string homeTeam)
    {
        using var doc = JsonDocument.Parse(incidentsJson);
        if (!doc.RootElement.TryGetProperty("incidents", out var incidents))
            return [];

        return incidents.EnumerateArray()
            .Select(i => MapIncident(i, homeTeam))
            .Where(e => e is not null)
            .Cast<MatchEvent>()
            .ToList();
    }

    private static MatchEvent? MapIncident(JsonElement incident, string homeTeam)
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

        // own goals are a separate incidentClass in SofaScore
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

    private static MatchStatus MapStatus(JsonElement ev)
    {
        if (!ev.TryGetProperty("status", out var status)) return MatchStatus.Live;
        var code = status.TryGetProperty("code", out var c) ? c.GetInt32() : -1;
        return code switch
        {
            0 => MatchStatus.NotStarted,
            6 or 7 => MatchStatus.HalfTime,
            100 => MatchStatus.Finished,
            60 or 70 => MatchStatus.Postponed,
            _ => MatchStatus.Live
        };
    }
}
