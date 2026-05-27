using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SportsMonitor.Domain.Configuration;
using SportsMonitor.Domain.Interfaces;
using SportsMonitor.Domain.Models;
using SportsMonitor.Infrastructure.Providers;
using SportsMonitor.Workers.Base;

namespace SportsMonitor.Workers;

public class ApiFootballWorker : PollingWorker
{
    private readonly ApiFootballProvider _provider;
    private readonly IOptionsMonitor<ApiFootballOptions> _options;

    public ApiFootballWorker(
        ApiFootballProvider provider,
        IOptionsMonitor<ApiFootballOptions> options,
        ISnapshotStore store,
        IMatchHistoryRepository history,
        ILogger<ApiFootballWorker> logger)
        : base(store, history, logger)
    {
        _provider = provider;
        _options = options;
    }

    protected override string WorkerName => nameof(ApiFootballWorker);
    protected override bool IsEnabled => _options.CurrentValue.Enabled;
    protected override int PollingIntervalSeconds => _options.CurrentValue.PollingIntervalSeconds;

    protected override Task<IReadOnlyList<NormalizedMatch>> CollectAsync(CancellationToken ct) =>
        _provider.GetLiveMatchesAsync(ct);
}
