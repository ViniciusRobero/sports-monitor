using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SportsMonitor.Domain.Interfaces;
using SportsMonitor.Domain.Models;

namespace SportsMonitor.Workers.Base;

public abstract class PollingWorker : BackgroundService, IRefreshable
{
    private readonly ISnapshotStore _store;
    private readonly IMatchHistoryRepository _history;
    private readonly ILogger _logger;

    protected PollingWorker(
        ISnapshotStore store,
        IMatchHistoryRepository history,
        ILogger logger)
    {
        _store = store;
        _history = history;
        _logger = logger;
    }

    protected abstract string WorkerName { get; }
    protected abstract bool IsEnabled { get; }
    protected abstract int PollingIntervalSeconds { get; }
    protected abstract Task<IReadOnlyList<NormalizedMatch>> CollectAsync(CancellationToken ct);

    public async Task<int> CollectOnceAsync(CancellationToken ct)
    {
        var matches = await CollectAsync(ct);

        foreach (var match in matches)
        {
            await _history.SaveSnapshotAsync(match, ct);
            _store.Upsert(match);
        }

        return matches.Count;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!IsEnabled)
        {
            _logger.LogInformation("{Worker} is disabled. Skipping.", WorkerName);
            return;
        }

        _logger.LogInformation("{Worker} started. Interval: {Interval}s", WorkerName, PollingIntervalSeconds);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var count = await CollectOnceAsync(ct);
                _logger.LogDebug("{Worker} collected {Count} live matches.", WorkerName, count);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "{Worker} collection failed. Will retry.", WorkerName);
            }

            await Task.Delay(TimeSpan.FromSeconds(PollingIntervalSeconds), ct);
        }
    }
}
