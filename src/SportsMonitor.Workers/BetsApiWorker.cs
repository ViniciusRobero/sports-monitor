using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SportsMonitor.Domain.Configuration;
using SportsMonitor.Domain.Interfaces;
using SportsMonitor.Domain.Models;
using SportsMonitor.Infrastructure.Providers;
using SportsMonitor.Workers.Base;

namespace SportsMonitor.Workers;

public class BetsApiWorker : PollingWorker
{
    private readonly BetsApiProvider _provider;
    private readonly IOptionsMonitor<BetsApiOptions> _options;

    public BetsApiWorker(
        BetsApiProvider provider,
        IOptionsMonitor<BetsApiOptions> options,
        ISnapshotStore store,
        IMatchHistoryRepository history,
        ILogger<BetsApiWorker> logger)
        : base(store, history, logger)
    {
        _provider = provider;
        _options = options;
    }

    protected override string WorkerName => nameof(BetsApiWorker);
    protected override bool IsEnabled => _options.CurrentValue.Enabled;
    protected override int PollingIntervalSeconds => _options.CurrentValue.PollingIntervalSeconds;

    protected override Task<IReadOnlyList<NormalizedMatch>> CollectAsync(CancellationToken ct) =>
        _provider.GetLiveMatchesAsync(ct);
}
