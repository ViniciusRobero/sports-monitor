using System.Text.Json;
using SportsMonitor.Domain.Configuration;
using SportsMonitor.Domain.Interfaces;
using SportsMonitor.Domain.Models;

namespace SportsMonitor.Infrastructure.Providers;

public class BetsApiProvider : IMatchDataProvider
{
    private readonly HttpClient _http;
    private readonly BetsApiOptions _options;
    private readonly IMatchResolver _resolver;

    public BetsApiProvider(HttpClient http, BetsApiOptions options, IMatchResolver resolver)
    {
        _http = http;
        _options = options;
        _resolver = resolver;
        _http.BaseAddress ??= new Uri(_options.BaseUrl);
    }

    public string Name => "bet365";

    public async Task<IReadOnlyList<NormalizedMatch>> GetLiveMatchesAsync(CancellationToken ct)
    {
        // Step 1: get list of live soccer events (sport_id=1 = soccer on BetsAPI)
        using var listResponse = await _http.GetAsync($"/v1/bet365/inplay_filter?sport_id=1&token={_options.Token}", ct);
        listResponse.EnsureSuccessStatusCode();

        var listJson = await listResponse.Content.ReadAsStringAsync(ct);
        using var listDoc = JsonDocument.Parse(listJson);

        if (!listDoc.RootElement.TryGetProperty("results", out var results))
            return [];

        var tasks = results
            .EnumerateArray()
            .Select(ev => FetchEventAsync(ev, ct))
            .ToList();

        var matches = await Task.WhenAll(tasks);
        return matches.Where(m => m is not null).Cast<NormalizedMatch>().ToList();
    }

    private async Task<NormalizedMatch?> FetchEventAsync(JsonElement ev, CancellationToken ct)
    {
        var fi = ev.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
        if (fi is null) return null;

        using var response = await _http.GetAsync($"/v1/bet365/event?FI={fi}&stats=1&token={_options.Token}", ct);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync(ct);
        return MapEvent(json, fi);
    }

    private NormalizedMatch? MapEvent(string json, string fi)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("results", out var results)) return null;

        // find the EV (event) node
        var eventNode = results.EnumerateArray()
            .FirstOrDefault(n => n.TryGetProperty("type", out var t) && t.GetString() == "EV");

        if (eventNode.ValueKind == JsonValueKind.Undefined) return null;

        var name = eventNode.TryGetProperty("NA", out var na) ? na.GetString() ?? "" : "";
        var (homeTeam, awayTeam) = SplitTeams(name);
        var competition = eventNode.TryGetProperty("CT", out var ct2) ? ct2.GetString() ?? "" : "";
        var score = eventNode.TryGetProperty("SS", out var ss) ? ss.GetString() ?? "0-0" : "0-0";
        var (homeScore, awayScore) = ParseScore(score);
        var minute = eventNode.TryGetProperty("LM", out var lm) ? lm.GetString() ?? "0" : "0";

        var matchId = _resolver.ResolveMatchId(fi, Name, homeTeam, awayTeam, DateTime.UtcNow.Date, competition);

        var events = MapEvents(results, homeTeam);

        return new NormalizedMatch(
            matchId,
            homeTeam,
            awayTeam,
            competition,
            DateTime.UtcNow.Date,
            homeScore,
            awayScore,
            MatchStatus.Live,
            events,
            Name,
            DateTime.UtcNow,
            json
        );
    }

    private static IReadOnlyList<MatchEvent> MapEvents(JsonElement results, string homeTeam)
    {
        // ST nodes are individual timeline events
        return results.EnumerateArray()
            .Where(n => n.TryGetProperty("type", out var t) && t.GetString() == "ST")
            .Select(n => MapSingleEvent(n, homeTeam))
            .Where(e => e is not null)
            .Cast<MatchEvent>()
            .ToList();
    }

    private static MatchEvent? MapSingleEvent(JsonElement node, string homeTeam)
    {
        var et = node.TryGetProperty("ET", out var etProp) ? etProp.GetString() : null;
        var eventType = et switch
        {
            "IGoal" => EventType.Goal,
            "IYellowCard" => EventType.YellowCard,
            "IRedCard" => EventType.RedCard,
            "ISubstitution" => EventType.Substitution,
            _ => (EventType?)null
        };

        if (eventType is null) return null;

        var minute = node.TryGetProperty("TM", out var tm) ? int.TryParse(tm.GetString(), out var m) ? m : 0 : 0;
        var teamId = node.TryGetProperty("TI", out var ti) ? ti.GetString() : "1";
        var team = teamId == "1" ? "home" : "away";

        // LA field contains player info — format confirmed to be "PlayerName - EventType Minute'"
        // e.g. "Pedro - Goal 32'" or "Pedro (Arrascaeta) - Goal 32'"
        // TODO: update parser once real payload is confirmed
        var label = node.TryGetProperty("LA", out var la) ? la.GetString() ?? "" : "";
        var playerName = ExtractPlayerName(label);

        return new MatchEvent(eventType.Value, minute, playerName, team);
    }

    // Extracts player name from BetsAPI LA label.
    // Expected formats (to be confirmed with real payload):
    //   "Pedro - Goal 32'"
    //   "Pedro (Arrascaeta) - Goal 32'"
    //   "Pedro"
    public static string ExtractPlayerName(string label)
    {
        if (string.IsNullOrWhiteSpace(label)) return "";

        var dashIndex = label.IndexOf(" - ", StringComparison.Ordinal);
        var raw = dashIndex >= 0 ? label[..dashIndex].Trim() : label.Trim();

        // strip trailing apostrophe-minute pattern if no dash separator e.g. "Pedro 32'"
        var spaceIndex = raw.LastIndexOf(' ');
        if (spaceIndex >= 0 && raw[spaceIndex..].TrimStart().EndsWith('\''))
            raw = raw[..spaceIndex].Trim();

        return raw;
    }

    private static (string home, string away) SplitTeams(string name)
    {
        var sep = name.IndexOf(" v ", StringComparison.Ordinal);
        if (sep < 0) return (name, "");
        return (name[..sep].Trim(), name[(sep + 3)..].Trim());
    }

    private static (int home, int away) ParseScore(string score)
    {
        var parts = score.Split('-');
        if (parts.Length != 2) return (0, 0);
        return (int.TryParse(parts[0].Trim(), out var h) ? h : 0,
                int.TryParse(parts[1].Trim(), out var a) ? a : 0);
    }
}
