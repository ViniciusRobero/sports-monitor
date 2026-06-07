using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using SportsMonitor.Domain.Configuration;
using SportsMonitor.Domain.Interfaces;
using SportsMonitor.Domain.Models;

namespace SportsMonitor.Infrastructure.Providers;

public class SofaScoreProvider : IMatchDataProvider, IAsyncDisposable
{
    private const int FetchTimeoutMs = 20_000;

    private readonly SofaScoreOptions _options;
    private readonly IMatchResolver _resolver;
    private readonly ILogger<SofaScoreProvider> _logger;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public string Name => SofaScoreMapper.SourceName;

    public SofaScoreProvider(SofaScoreOptions options, IMatchResolver resolver, ILogger<SofaScoreProvider> logger)
    {
        _options = options;
        _resolver = resolver;
        _logger = logger;
    }

    private async Task<IBrowser> GetBrowserAsync()
    {
        if (_browser is not null) return _browser;
        await _initLock.WaitAsync();
        try
        {
            if (_browser is not null) return _browser;
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new()
            {
                Headless = true,
                Args = ["--no-sandbox", "--disable-dev-shm-usage"]
            });
        }
        finally { _initLock.Release(); }
        return _browser;
    }

    public async Task<IReadOnlyList<NormalizedMatch>> GetLiveMatchesAsync(CancellationToken ct)
    {
        var browser = await GetBrowserAsync();

        await using var context = await browser.NewContextAsync(new()
        {
            UserAgent = _options.UserAgent,
            ExtraHTTPHeaders = new Dictionary<string, string>
            {
                ["Origin"] = "https://www.sofascore.com",
                ["Referer"] = "https://www.sofascore.com/",
                ["Accept-Language"] = "pt-BR,pt;q=0.9,en-US;q=0.8"
            }
        });

        var page = await context.NewPageAsync();

        // fetch() runs inside real Chromium — genuine Chrome TLS fingerprint (JA3/JA4)
        // AbortController ensures the JS fetch does not hang if the server stalls
        string? eventsJson;
        try
        {
            eventsJson = await page.EvaluateAsync<string?>($$"""
                async () => {
                    const ctrl = new AbortController();
                    const t = setTimeout(() => ctrl.abort(), {{FetchTimeoutMs}});
                    try {
                        const r = await fetch('https://api.sofascore.com/api/v1/sport/football/events/live',
                            { signal: ctrl.signal, headers: { 'Accept': 'application/json, text/plain, */*' } });
                        if (!r.ok) return '__HTTP_' + r.status;
                        return await r.text();
                    } catch(e) {
                        return '__ERR_' + e.message;
                    } finally {
                        clearTimeout(t);
                    }
                }
            """);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SofaScore: page.EvaluateAsync timed out or threw.");
            return [];
        }

        if (string.IsNullOrWhiteSpace(eventsJson) || eventsJson.StartsWith("__"))
        {
            _logger.LogWarning("SofaScore events/live failed: {Result}", eventsJson ?? "(null)");
            return [];
        }

        using var doc = JsonDocument.Parse(eventsJson);
        if (!doc.RootElement.TryGetProperty("events", out var events))
        {
            _logger.LogWarning("SofaScore: 'events' property missing. Keys: {Keys}",
                string.Join(", ", doc.RootElement.EnumerateObject().Select(p => p.Name)));
            return [];
        }

        var eventArray = events.EnumerateArray().ToList();
        _logger.LogInformation("SofaScore: {Count} live events found.", eventArray.Count);

        var matches = new List<NormalizedMatch?>();
        foreach (var ev in eventArray)
        {
            ct.ThrowIfCancellationRequested();
            matches.Add(await FetchMatchAsync(page, ev, ct));
            await Task.Delay(200, ct);
        }

        return matches.Where(m => m is not null).Cast<NormalizedMatch>().ToList();
    }

    private async Task<NormalizedMatch?> FetchMatchAsync(IPage page, JsonElement ev, CancellationToken ct)
    {
        var id = ev.TryGetProperty("id", out var idProp) ? idProp.GetInt64() : 0;
        if (id == 0) return null;

        string? incidentsJson;
        try
        {
            incidentsJson = await page.EvaluateAsync<string?>($$"""
                async () => {
                    const ctrl = new AbortController();
                    const t = setTimeout(() => ctrl.abort(), {{FetchTimeoutMs}});
                    try {
                        const r = await fetch('https://api.sofascore.com/api/v1/event/{{id}}/incidents',
                            { signal: ctrl.signal, headers: { 'Accept': 'application/json, text/plain, */*' } });
                        if (!r.ok) return null;
                        return await r.text();
                    } catch(e) {
                        return null;
                    } finally {
                        clearTimeout(t);
                    }
                }
            """);
        }
        catch
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(incidentsJson) || incidentsJson == "null")
            return null;

        return SofaScoreMapper.MapMatch(ev, incidentsJson, id.ToString(), _resolver);
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null) await _browser.CloseAsync();
        _playwright?.Dispose();
    }
}
