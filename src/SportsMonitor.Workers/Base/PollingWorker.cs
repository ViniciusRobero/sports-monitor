using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SportsMonitor.Domain.Interfaces;
using SportsMonitor.Domain.Models;

namespace SportsMonitor.Workers.Base;

public abstract class PollingWorker : BackgroundService, IRefreshable
{
    private const int MaxRetries = 3;
    private const int DegradedThreshold = 3;
    private const int DownThreshold = 10;

    private readonly ISnapshotStore _store;
    private readonly IMatchHistoryRepository _history;
    private readonly ILogger _logger;

    private int _consecutiveFailures;
    private DateTime? _lastSuccessUtc;
    private DateTime? _lastFailureUtc;
    private string? _lastError;
    private int _totalCollected;

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

    /// <summary>
    /// Exposes the current health status of this worker for the /api/providers/status endpoint.
    /// </summary>
    public ProviderStatus GetStatus() => new()
    {
        Name = WorkerName,
        Enabled = IsEnabled,
        Health = _consecutiveFailures >= DownThreshold
            ? ProviderHealth.Down
            : _consecutiveFailures >= DegradedThreshold
                ? ProviderHealth.Degraded
                : ProviderHealth.Healthy,
        ConsecutiveFailures = _consecutiveFailures,
        TotalCollected = _totalCollected,
        LastSuccessUtc = _lastSuccessUtc,
        LastFailureUtc = _lastFailureUtc,
        LastError = _lastError
    };

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
            for (int attempt = 1; attempt <= MaxRetries && !ct.IsCancellationRequested; attempt++)
            {
                try
                {
                    var count = await CollectOnceAsync(ct);

                    _consecutiveFailures = 0;
                    _lastSuccessUtc = DateTime.UtcNow;
                    _totalCollected += count;

                    _logger.LogDebug("{Worker} collected {Count} live matches.", WorkerName, count);
                    break;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    if (attempt < MaxRetries)
                    {
                        var backoffSeconds = (int)Math.Pow(2, attempt); // 2s, 4s, 8s
                        _logger.LogWarning(
                            ex,
                            "{Worker} attempt {Attempt}/{MaxRetries} failed. Retrying in {Backoff}s.",
                            WorkerName, attempt, MaxRetries, backoffSeconds);

                        try
                        {
                            await Task.Delay(TimeSpan.FromSeconds(backoffSeconds), ct);
                        }
                        catch (OperationCanceledException) when (ct.IsCancellationRequested)
                        {
                            return;
                        }
                    }
                    else
                    {
                        // All retries exhausted
                        _consecutiveFailures++;
                        _lastFailureUtc = DateTime.UtcNow;
                        _lastError = ex.Message;

                        if (_consecutiveFailures >= DownThreshold)
                        {
                            _logger.LogError(
                                ex,
                                "{Worker} DOWN — {Failures} consecutive failures. Last error: {Error}",
                                WorkerName, _consecutiveFailures, ex.Message);
                        }
                        else if (_consecutiveFailures >= DegradedThreshold)
                        {
                            _logger.LogWarning(
                                ex,
                                "{Worker} DEGRADED — {Failures} consecutive failures.",
                                WorkerName, _consecutiveFailures);
                        }
                        else
                        {
                            _logger.LogWarning(
                                ex,
                                "{Worker} collection failed after {MaxRetries} retries. Will retry next cycle.",
                                WorkerName, MaxRetries);
                        }
                    }
                }
            }

            // Wait for next polling interval
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(PollingIntervalSeconds), ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return;
            }
        }
    }
}
