using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using SportsMonitor.Domain.Configuration;
using SportsMonitor.Domain.Interfaces;
using SportsMonitor.Domain.Models;

namespace SportsMonitor.Infrastructure.Providers;

// Fetching SofaScore's API (api.sofascore.com) is a moving target because of Cloudflare:
//
//   - A plain HTTP GET (no browser Origin/Sec-Fetch headers) is treated as a legitimate API
//     client and usually passes — this is the fast, simple happy path.
//   - An in-browser fetch() from www.sofascore.com carries an Origin/Sec-Fetch-Site that triggers
//     a 403 on the api subdomain (the cross-origin XHR needs a cf_clearance the page never gets).
//   - Headless Chromium is fingerprinted and 403'd; even headful real Chrome's in-page fetch 403s.
//
// So we try multiple transports in order of cost and log which one wins, falling back to the
// browser only when the cheap HTTP paths fail:
//   1. .NET HttpClient GET                (no browser, no Origin)
//   2. Playwright APIRequest GET          (separate HTTP stack, carries warmed-session cookies)
//   3. In-page fetch() from a warmed page (mirrors the site's own call)
//   4. Intercept the page's own /events/live response
public class SofaScoreProvider : IMatchDataProvider, IAsyncDisposable
{
    private const int FetchTimeoutMs = 20_000;
    private const string EventsLiveUrl = "https://api.sofascore.com/api/v1/sport/football/events/live";
    private const string HomeUrl = "https://www.sofascore.com/";
    private const string LiveScoreUrl = "https://www.sofascore.com/football/livescore";

    private readonly SofaScoreOptions _options;
    private readonly IMatchResolver _resolver;
    private readonly ILogger<SofaScoreProvider> _logger;
    private readonly HttpClient _http;

    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private IPage? _page;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    // Once a transport proves it works, stick to it so we don't pay for the whole ladder each call.
    private FetchTransport _preferred = FetchTransport.Unknown;

    private enum FetchTransport { Unknown, Http, ApiRequest, Browser }

    // Backoff to protect the IP's reputation: hammering SofaScore while blocked is exactly what
    // gets an IP flagged. On a block we go quiet for an exponentially growing window (cap 30 min),
    // and reset the moment a request succeeds.
    private DateTime _blockedUntilUtc = DateTime.MinValue;
    private int _blockStreak;

    public string Name => SofaScoreMapper.SourceName;

    public SofaScoreProvider(SofaScoreOptions options, IMatchResolver resolver, ILogger<SofaScoreProvider> logger)
    {
        _options = options;
        _resolver = resolver;
        _logger = logger;

        _http = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All })
        {
            Timeout = TimeSpan.FromMilliseconds(FetchTimeoutMs)
        };
        // Mirror a generic browser-ish API client (no Origin — that is what trips the api 403).
        _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", options.UserAgent);
        _http.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json, text/plain, */*");
        _http.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "pt-BR,pt;q=0.9,en-US;q=0.8");
    }

    public async Task<IReadOnlyList<NormalizedMatch>> GetLiveMatchesAsync(CancellationToken ct)
    {
        // Respect the cooldown set after a previous block — don't even touch the network.
        if (DateTime.UtcNow < _blockedUntilUtc)
        {
            _logger.LogDebug("SofaScore: backing off until {Until:HH:mm:ss} (block streak {Streak}); skipping.",
                _blockedUntilUtc.ToLocalTime(), _blockStreak);
            return [];
        }

        var eventsJson = await FetchJsonAsync(EventsLiveUrl, ct);

        if (IsBlocked(eventsJson))
        {
            _blockStreak++;
            // 2, 4, 8, 16 min ... capped at 30 min.
            var backoff = TimeSpan.FromSeconds(Math.Min(1800, 60 * Math.Pow(2, _blockStreak)));
            _blockedUntilUtc = DateTime.UtcNow + backoff;
            _logger.LogWarning("SofaScore blocked on all transports ({Result}); backing off {Minutes:0.#} min (streak {Streak}).",
                Truncate(eventsJson), backoff.TotalMinutes, _blockStreak);
            return [];
        }

        // Success — clear any backoff state.
        _blockStreak = 0;
        _blockedUntilUtc = DateTime.MinValue;

        using var doc = JsonDocument.Parse(eventsJson!);
        if (!doc.RootElement.TryGetProperty("events", out var events))
        {
            _logger.LogWarning("SofaScore: 'events' missing. Body: {Body}", Truncate(eventsJson, 500));
            return [];
        }

        var eventArray = events.EnumerateArray().ToList();
        _logger.LogInformation("SofaScore: {Count} live events found.", eventArray.Count);

        var matches = new List<NormalizedMatch?>();
        foreach (var ev in eventArray)
        {
            ct.ThrowIfCancellationRequested();
            matches.Add(await FetchMatchAsync(ev, ct));
            await Task.Delay(200, ct);
        }

        return matches.Where(m => m is not null).Cast<NormalizedMatch>().ToList();
    }

    // Tries each transport in order, returning the first that works and remembering whether the
    // cheap HTTP path is viable (so we can skip straight to the browser when it isn't).
    // Crucially, the browser path leads with a top-level NAVIGATION to the API URL — that is the
    // one shape that works in a real browser (Sec-Fetch-Mode: navigate, no Origin). An in-page
    // fetch() sends Origin/Sec-Fetch-Site: cors and gets 403.
    private async Task<string?> FetchJsonAsync(string url, CancellationToken ct)
    {
        // 1. Plain HTTP GET — cheapest, no browser. (Often 403'd by the edge's client fingerprint.)
        if (_preferred is FetchTransport.Unknown or FetchTransport.Http)
        {
            var http = await TryHttpAsync(url, ct);
            if (!IsBlocked(http)) { Prefer(FetchTransport.Http, "HttpClient"); return http; }
            _logger.LogDebug("SofaScore: HttpClient blocked ({Result}).", Truncate(http));
            if (_preferred == FetchTransport.Http) _preferred = FetchTransport.Unknown;
        }

        var page = await GetWarmPageAsync(ct);

        // 2. Top-level navigation — mirrors a real browser opening the URL directly.
        var nav = await TryNavigateAsync(page, url);
        if (!IsBlocked(nav)) { Prefer(FetchTransport.Browser, "navigation"); return nav; }
        _logger.LogDebug("SofaScore: navigation blocked ({Result}).", Truncate(nav));

        // 3. Playwright APIRequest — context cookies, no page Origin.
        var api = await TryApiRequestAsync(url);
        if (!IsBlocked(api)) { Prefer(FetchTransport.Browser, "APIRequest"); return api; }
        _logger.LogDebug("SofaScore: APIRequest blocked ({Result}).", Truncate(api));

        // 4. In-page fetch from the warmed page.
        var inproc = await InProcFetchAsync(page, url);
        if (!IsBlocked(inproc)) { Prefer(FetchTransport.Browser, "in-page fetch"); return inproc; }
        _logger.LogDebug("SofaScore: in-page fetch blocked ({Result}).", Truncate(inproc));

        // 5. Intercept the page's own /events/live response (events list only).
        if (url == EventsLiveUrl)
        {
            var icpt = await InterceptEventsAsync(page, ct);
            if (!IsBlocked(icpt)) { Prefer(FetchTransport.Browser, "intercept"); return icpt; }
            _logger.LogDebug("SofaScore: intercept blocked ({Result}), re-warming.", Truncate(icpt));
            await InvalidateContextAsync();
        }

        return "__ERR_all_transports_blocked";
    }

    private void Prefer(FetchTransport transport, string name)
    {
        if (_preferred != transport)
            _logger.LogInformation("SofaScore: using {Name} transport.", name);
        _preferred = transport;
    }

    // Navigates the page straight to the API URL and reads the raw response body. A top-level
    // navigation has no Origin/CORS and reuses the warmed session — the shape that passes the edge.
    private static async Task<string?> TryNavigateAsync(IPage page, string url)
    {
        try
        {
            var resp = await page.GotoAsync(url, new()
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = FetchTimeoutMs
            });
            if (resp is null) return "__ERR_null_response";
            if (resp.Status != 200) return "__HTTP_" + resp.Status;
            return await resp.TextAsync();
        }
        catch (Exception ex)
        {
            return "__ERR_" + ex.Message.Split('\n')[0];
        }
    }

    private async Task<string?> TryHttpAsync(string url, CancellationToken ct)
    {
        try
        {
            using var resp = await _http.GetAsync(url, ct);
            if (!resp.IsSuccessStatusCode) return "__HTTP_" + (int)resp.StatusCode;
            return await resp.Content.ReadAsStringAsync(ct);
        }
        catch (Exception ex)
        {
            return "__ERR_" + ex.Message.Split('\n')[0];
        }
    }

    // Uses the warmed BrowserContext's request stack — sends the cleared-session cookies but no
    // page Origin/Sec-Fetch headers, so it looks like an API client rather than a cross-origin XHR.
    private async Task<string?> TryApiRequestAsync(string url)
    {
        if (_context is null) return "__ERR_no_context";
        try
        {
            var resp = await _context.APIRequest.GetAsync(url, new()
            {
                Headers = new Dictionary<string, string> { ["Accept"] = "application/json, text/plain, */*" }
            });
            if (resp.Status != 200) return "__HTTP_" + resp.Status;
            return await resp.TextAsync();
        }
        catch (Exception ex)
        {
            return "__ERR_" + ex.Message.Split('\n')[0];
        }
    }

    private async Task<IBrowser> GetBrowserAsync()
    {
        if (_browser is not null) return _browser;
        _playwright ??= await Playwright.CreateAsync();

        string[] args =
        [
            "--no-sandbox",
            "--disable-dev-shm-usage",
            "--disable-blink-features=AutomationControlled",
            "--disable-infobars",
            "--start-maximized"
        ];

        // A real, installed Chrome (channel) is far less detectable than bundled Chromium. Fall
        // back to bundled Chromium if the user has no system Chrome.
        try
        {
            _browser = await _playwright.Chromium.LaunchAsync(new()
            {
                Channel = "chrome",
                Headless = _options.Headless,
                Args = args
            });
            _logger.LogDebug("SofaScore: launched system Chrome (channel=chrome, headless={Headless}).", _options.Headless);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SofaScore: system Chrome unavailable, falling back to bundled Chromium.");
            _browser = await _playwright.Chromium.LaunchAsync(new()
            {
                Headless = _options.Headless,
                Args = args
            });
        }

        return _browser;
    }

    // Returns a page that has already passed the Cloudflare challenge. Created lazily and reused.
    private async Task<IPage> GetWarmPageAsync(CancellationToken ct)
    {
        if (_page is not null && !_page.IsClosed) return _page;

        await _initLock.WaitAsync(ct);
        try
        {
            if (_page is not null && !_page.IsClosed) return _page;

            var browser = await GetBrowserAsync();
            _context ??= await CreateContextAsync(browser);
            _page = await _context.NewPageAsync();

            await WarmUpAsync(_page, ct);
            return _page;
        }
        finally { _initLock.Release(); }
    }

    private async Task<IBrowserContext> CreateContextAsync(IBrowser browser)
    {
        var context = await browser.NewContextAsync(new()
        {
            UserAgent = _options.UserAgent,
            Locale = "pt-BR",
            TimezoneId = "America/Sao_Paulo",
            ViewportSize = new() { Width = 1920, Height = 1080 },
            ExtraHTTPHeaders = new Dictionary<string, string>
            {
                ["Accept-Language"] = "pt-BR,pt;q=0.9,en-US;q=0.8"
            }
        });

        // Hide the most common automation signals before any page script runs.
        await context.AddInitScriptAsync("""
            Object.defineProperty(navigator, 'webdriver', { get: () => undefined });
            Object.defineProperty(navigator, 'plugins', { get: () => [1, 2, 3, 4, 5] });
            Object.defineProperty(navigator, 'languages', { get: () => ['pt-BR', 'pt', 'en-US', 'en'] });
            window.chrome = { runtime: {} };
        """);

        return context;
    }

    // Load the homepage so Cloudflare issues cf_clearance, then wait for any challenge to clear.
    private async Task WarmUpAsync(IPage page, CancellationToken ct)
    {
        await page.GotoAsync(HomeUrl, new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 45_000
        });

        // Cloudflare's interstitial ("Just a moment...") reloads itself once the JS challenge
        // is solved. Poll briefly until the real page replaces it.
        for (var attempt = 0; attempt < 15; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            string title;
            try { title = await page.TitleAsync(); }
            catch { title = ""; }

            if (!title.Contains("Just a moment", StringComparison.OrdinalIgnoreCase) &&
                !title.Contains("Attention Required", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("SofaScore: warm-up complete (title '{Title}').", title);
                return;
            }

            await Task.Delay(1_000, ct);
        }

        _logger.LogWarning("SofaScore: warm-up still on challenge page after timeout; will attempt fetch anyway.");
    }

    // Runs fetch() inside the warmed page (default credentials, like the site's own call).
    private static async Task<string?> InProcFetchAsync(IPage page, string url)
    {
        try
        {
            return await page.EvaluateAsync<string?>($$"""
                async () => {
                    const ctrl = new AbortController();
                    const t = setTimeout(() => ctrl.abort(), {{FetchTimeoutMs}});
                    try {
                        const r = await fetch('{{url}}', {
                            signal: ctrl.signal,
                            headers: { 'Accept': 'application/json, text/plain, */*' }
                        });
                        if (!r.ok) return '__HTTP_' + r.status;
                        return await r.text();
                    } catch (e) {
                        return '__ERR_' + e.message;
                    } finally {
                        clearTimeout(t);
                    }
                }
                """);
        }
        catch (Exception ex)
        {
            return "__ERR_" + ex.Message.Split('\n')[0];
        }
    }

    // Last resort for events: navigate to the live page and grab the /events/live response that
    // the page fires by itself — passes because it is the site's own request.
    private async Task<string?> InterceptEventsAsync(IPage page, CancellationToken ct)
    {
        try
        {
            var waiter = page.WaitForResponseAsync(
                r => r.Url.Contains("/sport/football/events/live"),
                new() { Timeout = 30_000 });

            await page.GotoAsync(LiveScoreUrl, new()
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 30_000
            });

            var resp = await waiter;
            if (resp.Status != 200) return "__HTTP_" + resp.Status;
            return await resp.TextAsync();
        }
        catch (TimeoutException)
        {
            return "__ERR_intercept_timeout";
        }
        catch (Exception ex)
        {
            return "__ERR_" + ex.Message.Split('\n')[0];
        }
    }

    private async Task<NormalizedMatch?> FetchMatchAsync(JsonElement ev, CancellationToken ct)
    {
        var id = ev.TryGetProperty("id", out var idProp) ? idProp.GetInt64() : 0;
        if (id == 0) return null;

        var incidentsJson = await FetchJsonAsync(
            $"https://api.sofascore.com/api/v1/event/{id}/incidents", ct);

        // Incidents are best-effort: a match with no detail still contributes score/status.
        if (IsBlocked(incidentsJson) || incidentsJson == "null")
            incidentsJson = """{"incidents":[]}""";

        return SofaScoreMapper.MapMatch(ev, incidentsJson!, id.ToString(), _resolver);
    }

    private async Task InvalidateContextAsync()
    {
        await _initLock.WaitAsync();
        try
        {
            if (_context is not null)
            {
                try { await _context.CloseAsync(); } catch { /* already gone */ }
                _context = null;
                _page = null;
            }
        }
        finally { _initLock.Release(); }
    }

    private static bool IsBlocked(string? json) =>
        string.IsNullOrWhiteSpace(json) ||
        json.StartsWith("__", StringComparison.Ordinal) ||
        // A Cloudflare interstitial comes back as HTML (status 200) instead of JSON.
        !json.TrimStart().StartsWith('{') ||
        json.Contains("\"challenge\"", StringComparison.Ordinal);

    private static string Truncate(string? value, int max = 80) =>
        value is null ? "(null)" : value[..Math.Min(max, value.Length)];

    public async ValueTask DisposeAsync()
    {
        _http.Dispose();
        if (_context is not null) { try { await _context.CloseAsync(); } catch { } }
        if (_browser is not null) { try { await _browser.CloseAsync(); } catch { } }
        _playwright?.Dispose();
        _initLock.Dispose();
    }
}
