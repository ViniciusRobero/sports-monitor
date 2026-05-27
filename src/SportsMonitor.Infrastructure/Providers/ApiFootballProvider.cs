using System.Text.Json;
using SportsMonitor.Domain.Configuration;
using SportsMonitor.Domain.Interfaces;
using SportsMonitor.Domain.Models;

namespace SportsMonitor.Infrastructure.Providers;

public class ApiFootballProvider : IMatchDataProvider
{
    private readonly HttpClient _http;
    private readonly ApiFootballOptions _options;
    private readonly IMatchResolver _resolver;

    public ApiFootballProvider(HttpClient http, ApiFootballOptions options, IMatchResolver resolver)
    {
        _http = http;
        _options = options;
        _resolver = resolver;

        _http.BaseAddress ??= new Uri(_options.BaseUrl);
        if (!string.IsNullOrWhiteSpace(_options.ApiKey) &&
            !_http.DefaultRequestHeaders.Contains("x-apisports-key"))
        {
            _http.DefaultRequestHeaders.Add("x-apisports-key", _options.ApiKey);
        }
    }

    public string Name => "api_football";

    public async Task<IReadOnlyList<NormalizedMatch>> GetLiveMatchesAsync(CancellationToken ct)
    {
        using var response = await _http.GetAsync("/fixtures?live=all", ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        using var document = JsonDocument.Parse(json);

        if (!document.RootElement.TryGetProperty("response", out var fixtures))
            return [];

        return fixtures
            .EnumerateArray()
            .Select(fixture => MapFixture(fixture, json))
            .ToList();
    }

    private NormalizedMatch MapFixture(JsonElement fixture, string rawJson)
    {
        var homeTeam = fixture.GetProperty("teams").GetProperty("home").GetProperty("name").GetString() ?? "";
        var awayTeam = fixture.GetProperty("teams").GetProperty("away").GetProperty("name").GetString() ?? "";
        var competition = fixture.GetProperty("league").GetProperty("name").GetString() ?? "";
        var kickOff = fixture.GetProperty("fixture").GetProperty("date").GetDateTime();
        var sourceMatchId = fixture.GetProperty("fixture").GetProperty("id").GetInt32().ToString();
        var matchId = _resolver.ResolveMatchId(sourceMatchId, Name, homeTeam, awayTeam, kickOff, competition);

        return new NormalizedMatch(
            matchId,
            homeTeam,
            awayTeam,
            competition,
            kickOff,
            GetNullableInt(fixture.GetProperty("goals"), "home") ?? 0,
            GetNullableInt(fixture.GetProperty("goals"), "away") ?? 0,
            MapStatus(fixture.GetProperty("fixture").GetProperty("status").GetProperty("short").GetString()),
            MapEvents(fixture, homeTeam),
            Name,
            DateTime.UtcNow,
            rawJson
        );
    }

    private static IReadOnlyList<MatchEvent> MapEvents(JsonElement fixture, string homeTeam)
    {
        if (!fixture.TryGetProperty("events", out var events) || events.ValueKind != JsonValueKind.Array)
            return [];

        return events
            .EnumerateArray()
            .Select(e => new MatchEvent(
                MapEventType(e.GetProperty("type").GetString(), e.TryGetProperty("detail", out var detail) ? detail.GetString() : null),
                GetNullableInt(e.GetProperty("time"), "elapsed") ?? 0,
                e.GetProperty("player").GetProperty("name").GetString() ?? "",
                string.Equals(e.GetProperty("team").GetProperty("name").GetString(), homeTeam, StringComparison.OrdinalIgnoreCase)
                    ? "home"
                    : "away"))
            .ToList();
    }

    private static int? GetNullableInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind == JsonValueKind.Null)
            return null;

        return property.GetInt32();
    }

    private static EventType MapEventType(string? type, string? detail) => type switch
    {
        "Goal" => EventType.Goal,
        "Card" when string.Equals(detail, "Red Card", StringComparison.OrdinalIgnoreCase) => EventType.RedCard,
        "Card" => EventType.YellowCard,
        "subst" => EventType.Substitution,
        _ => EventType.VAR
    };

    private static MatchStatus MapStatus(string? status) => status switch
    {
        "NS" => MatchStatus.NotStarted,
        "1H" or "2H" or "ET" or "P" => MatchStatus.Live,
        "HT" => MatchStatus.HalfTime,
        "FT" or "AET" or "PEN" => MatchStatus.Finished,
        "PST" => MatchStatus.Postponed,
        "CANC" => MatchStatus.Cancelled,
        _ => MatchStatus.Live
    };
}
