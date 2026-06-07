using System.Text.Json;
using Microsoft.Playwright;
using SportsMonitor.Domain.Configuration;
using SportsMonitor.Domain.Interfaces;
using SportsMonitor.Domain.Models;

namespace SportsMonitor.Infrastructure.Providers;

public class SofaScoreProvider : IMatchDataProvider, IAsyncDisposable
{
    private readonly SofaScoreOptions _options;
    private readonly IMatchResolver _resolver;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public string Name => SofaScoreMapper.SourceName;

    public SofaScoreProvider(SofaScoreOptions options, IMatchResolver resolver)
    {
        _options = options;
        _resolver = resolver;
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
        var eventsJson = await page.EvaluateAsync<string>("""
            async () => {
                const r = await fetch('https://api.sofascore.com/api/v1/sport/football/events/live',
                    { headers: { 'Accept': 'application/json, text/plain, */*' } });
                return r.text();
            }
        """);

        using var doc = JsonDocument.Parse(eventsJson);
        if (!doc.RootElement.TryGetProperty("events", out var events))
            return [];

        var matches = new List<NormalizedMatch?>();
        foreach (var ev in events.EnumerateArray())
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

        // $$""" so {{id}} is the C# interpolation and single { } are literal JS braces
        var incidentsJson = await page.EvaluateAsync<string>($$"""
            async () => {
                const r = await fetch('https://api.sofascore.com/api/v1/event/{{id}}/incidents',
                    { headers: { 'Accept': 'application/json, text/plain, */*' } });
                if (!r.ok) return null;
                return r.text();
            }
        """);

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
