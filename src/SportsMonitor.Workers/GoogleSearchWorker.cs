using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SportsMonitor.Domain.Configuration;
using SportsMonitor.Domain.Interfaces;
using SportsMonitor.Domain.Models;
using SportsMonitor.Infrastructure.Services;
using SportsMonitor.Infrastructure.Stores;

namespace SportsMonitor.Workers;

public class GoogleSearchWorker : BackgroundService
{
    private readonly GoogleSearchService _search;
    private readonly GoogleSnapshotStore _store;
    private readonly ISnapshotStore _snapshotStore;
    private readonly IOptionsMonitor<GoogleSearchOptions> _options;
    private readonly ILogger<GoogleSearchWorker> _logger;

    public GoogleSearchWorker(
        GoogleSearchService search,
        GoogleSnapshotStore store,
        ISnapshotStore snapshotStore,
        IOptionsMonitor<GoogleSearchOptions> options,
        ILogger<GoogleSearchWorker> logger)
    {
        _search = search;
        _store = store;
        _snapshotStore = snapshotStore;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var opts = _options.CurrentValue;
        if (!opts.Enabled)
        {
            _logger.LogInformation("GoogleSearchWorker is disabled. Skipping.");
            return;
        }

        _logger.LogInformation("GoogleSearchWorker started. Interval: {Interval}s", opts.PollingIntervalSeconds);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await SearchAllLiveMatchesAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GoogleSearchWorker failed. Will retry.");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.CurrentValue.PollingIntervalSeconds), ct);
        }
    }

    private async Task SearchAllLiveMatchesAsync(CancellationToken ct)
    {
        var matchIds = _snapshotStore.GetLiveMatchIds();
        foreach (var matchId in matchIds)
        {
            if (ct.IsCancellationRequested) break;

            var sources = _snapshotStore.GetAllForMatch(matchId);
            var first = sources.FirstOrDefault();
            if (first is null) continue;

            var query = $"{first.HomeTeam} {first.AwayTeam} ao vivo resultado";
            var results = await _search.SearchAsync(query, ct);

            _store.Upsert(new GoogleSearchSnapshot(
                matchId,
                first.HomeTeam,
                first.AwayTeam,
                query,
                results,
                DateTime.UtcNow
            ));

            _logger.LogDebug("GoogleSearchWorker: {Home} vs {Away} — {Count} results", first.HomeTeam, first.AwayTeam, results.Count);

            // Small delay between searches to avoid rate limit bursts
            await Task.Delay(500, ct);
        }
    }
}
