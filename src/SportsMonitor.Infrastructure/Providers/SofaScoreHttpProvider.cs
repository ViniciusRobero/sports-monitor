using System.Text.Json;
using Microsoft.Extensions.Logging;
using SportsMonitor.Domain.Configuration;
using SportsMonitor.Domain.Interfaces;
using SportsMonitor.Domain.Models;

namespace SportsMonitor.Infrastructure.Providers;

public class SofaScoreHttpProvider : IMatchDataProvider
{
    private const string LiveEventsPath = "/api/v1/sport/football/events/live";

    private readonly HttpClient _http;
    private readonly IMatchResolver _resolver;
    private readonly ILogger<SofaScoreHttpProvider> _logger;

    public SofaScoreHttpProvider(
        HttpClient http,
        SofaScoreOptions options,
        IMatchResolver resolver,
        ILogger<SofaScoreHttpProvider> logger)
    {
        _http = http;
        _resolver = resolver;
        _logger = logger;

        _http.BaseAddress ??= new Uri(options.BaseUrl);
        if (!_http.DefaultRequestHeaders.Contains("User-Agent"))
            _http.DefaultRequestHeaders.Add("User-Agent", options.UserAgent);
        if (!_http.DefaultRequestHeaders.Contains("Accept"))
            _http.DefaultRequestHeaders.Add("Accept", "application/json, text/plain, */*");
        if (!_http.DefaultRequestHeaders.Contains("Origin"))
            _http.DefaultRequestHeaders.Add("Origin", "https://www.sofascore.com");
        if (!_http.DefaultRequestHeaders.Contains("Referer"))
            _http.DefaultRequestHeaders.Add("Referer", "https://www.sofascore.com/");
        if (!_http.DefaultRequestHeaders.Contains("Accept-Language"))
            _http.DefaultRequestHeaders.Add("Accept-Language", "pt-BR,pt;q=0.9,en-US;q=0.8");
    }

    public string Name => SofaScoreMapper.SourceName;

    public async Task<IReadOnlyList<NormalizedMatch>> GetLiveMatchesAsync(CancellationToken ct)
    {
        using var response = await _http.GetAsync(LiveEventsPath, ct);
        response.EnsureSuccessStatusCode();

        var eventsJson = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(eventsJson);

        if (!doc.RootElement.TryGetProperty("events", out var events) ||
            events.ValueKind != JsonValueKind.Array)
        {
            _logger.LogWarning("SofaScore: 'events' property missing.");
            return [];
        }

        var eventArray = events.EnumerateArray().ToList();
        _logger.LogInformation("SofaScore HTTP: {Count} live events found.", eventArray.Count);

        var matches = new List<NormalizedMatch?>();
        foreach (var ev in eventArray)
        {
            ct.ThrowIfCancellationRequested();
            matches.Add(await FetchMatchAsync(ev, ct));
            await Task.Delay(200, ct);
        }

        return matches.Where(match => match is not null).Cast<NormalizedMatch>().ToList();
    }

    private async Task<NormalizedMatch?> FetchMatchAsync(JsonElement ev, CancellationToken ct)
    {
        var id = ev.TryGetProperty("id", out var idProp) ? idProp.GetInt64() : 0;
        if (id == 0) return null;

        try
        {
            var incidentsJson = await _http.GetStringAsync($"/api/v1/event/{id}/incidents", ct);
            if (string.IsNullOrWhiteSpace(incidentsJson))
                return null;

            return SofaScoreMapper.MapMatch(ev, incidentsJson, id.ToString(), _resolver);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(ex, "SofaScore HTTP: failed to fetch incidents for event {EventId}.", id);
            return null;
        }
    }
}
