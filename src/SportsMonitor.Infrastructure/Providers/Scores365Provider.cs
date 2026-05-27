using System.Text.Json;
using SportsMonitor.Domain.Configuration;
using SportsMonitor.Domain.Interfaces;
using SportsMonitor.Domain.Models;

namespace SportsMonitor.Infrastructure.Providers;

// 365Scores provides aggregate scores and status only — no individual incidents (goal scorers, cards).
// Useful as a 4th independent source for ScoreMismatch detection.
public class Scores365Provider : IMatchDataProvider
{
    private const string LiveGamesPath =
        "/web/games/?appTypeId=5&langId=31&timezoneName=America%2FSao_Paulo&userCountryId=-1&onlyLive=true";

    private readonly HttpClient _http;
    private readonly IMatchResolver _resolver;

    public Scores365Provider(HttpClient http, Scores365Options options, IMatchResolver resolver)
    {
        _http = http;
        _resolver = resolver;
        _http.BaseAddress ??= new Uri(options.BaseUrl);
        if (!_http.DefaultRequestHeaders.Contains("User-Agent"))
            _http.DefaultRequestHeaders.Add("User-Agent", options.UserAgent);
        if (!_http.DefaultRequestHeaders.Contains("Accept"))
            _http.DefaultRequestHeaders.Add("Accept", "application/json, text/plain, */*");
    }

    public string Name => "365scores";

    public async Task<IReadOnlyList<NormalizedMatch>> GetLiveMatchesAsync(CancellationToken ct)
    {
        using var response = await _http.GetAsync(LiveGamesPath, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("games", out var games) ||
            games.ValueKind != JsonValueKind.Array)
            return [];

        return games.EnumerateArray()
            .Where(g => IsSoccer(g) && IsLive(g))
            .Select(g => MapGame(g, json))
            .Where(m => m is not null)
            .Cast<NormalizedMatch>()
            .ToList();
    }

    private NormalizedMatch? MapGame(JsonElement game, string rawJson)
    {
        if (!game.TryGetProperty("homeCompetitor", out var home) ||
            !game.TryGetProperty("awayCompetitor", out var away))
            return null;

        var homeTeam = home.TryGetProperty("name", out var hn) ? hn.GetString() ?? "" : "";
        var awayTeam = away.TryGetProperty("name", out var an) ? an.GetString() ?? "" : "";
        var homeScore = home.TryGetProperty("score", out var hs) ? (int)hs.GetDouble() : 0;
        var awayScore = away.TryGetProperty("score", out var aws) ? (int)aws.GetDouble() : 0;

        var competition = "";
        if (game.TryGetProperty("competitionId", out var compId))
            competition = compId.ToString();

        var kickOff = game.TryGetProperty("startTime", out var st)
            ? DateTime.TryParse(st.GetString(), out var dt) ? dt.ToUniversalTime() : DateTime.UtcNow
            : DateTime.UtcNow;

        var sourceId = game.TryGetProperty("id", out var id) ? id.GetInt64().ToString() : Guid.NewGuid().ToString();
        var matchId = _resolver.ResolveMatchId(sourceId, Name, homeTeam, awayTeam, kickOff, competition);

        return new NormalizedMatch(
            matchId, homeTeam, awayTeam, competition,
            kickOff, homeScore, awayScore,
            MatchStatus.Live,
            [],     // 365Scores does not expose individual events — score-only source
            Name, DateTime.UtcNow, rawJson
        );
    }

    private static bool IsSoccer(JsonElement game) =>
        game.TryGetProperty("sportId", out var s) && s.GetInt32() == 1;

    private static bool IsLive(JsonElement game) =>
        game.TryGetProperty("statusGroup", out var s) && s.GetInt32() == 1;
}
