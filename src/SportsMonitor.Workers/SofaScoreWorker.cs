using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SportsMonitor.Domain.Configuration;
using SportsMonitor.Domain.Interfaces;
using SportsMonitor.Domain.Models;
using SportsMonitor.Infrastructure.Providers;
using SportsMonitor.Workers.Base;

namespace SportsMonitor.Workers;

public class SofaScoreWorker : PollingWorker
{
    private readonly SofaScoreProvider _provider;
    private readonly IOptionsMonitor<SofaScoreOptions> _options;

    public SofaScoreWorker(
        SofaScoreProvider provider,
        IOptionsMonitor<SofaScoreOptions> options,
        ISnapshotStore store,
        IMatchHistoryRepository history,
        ILogger<SofaScoreWorker> logger)
        : base(store, history, logger)
    {
        _provider = provider;
        _options = options;
    }

    protected override string WorkerName => nameof(SofaScoreWorker);
    protected override bool IsEnabled => _options.CurrentValue.Enabled;
    protected override int PollingIntervalSeconds => _options.CurrentValue.PollingIntervalSeconds;

    protected override Task<IReadOnlyList<NormalizedMatch>> CollectAsync(CancellationToken ct) =>
        _provider.GetLiveMatchesAsync(ct);
}
