using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SportsMonitor.Domain.Configuration;
using SportsMonitor.Domain.Interfaces;
using SportsMonitor.Domain.Models;
using SportsMonitor.Infrastructure.Providers;
using SportsMonitor.Workers.Base;

namespace SportsMonitor.Workers;

public class Scores365Worker : PollingWorker
{
    private readonly Scores365Provider _provider;
    private readonly IOptionsMonitor<Scores365Options> _options;

    public Scores365Worker(
        Scores365Provider provider,
        IOptionsMonitor<Scores365Options> options,
        ISnapshotStore store,
        IMatchHistoryRepository history,
        ILogger<Scores365Worker> logger)
        : base(store, history, logger)
    {
        _provider = provider;
        _options = options;
    }

    protected override string WorkerName => nameof(Scores365Worker);
    protected override bool IsEnabled => _options.CurrentValue.Enabled;
    protected override int PollingIntervalSeconds => _options.CurrentValue.PollingIntervalSeconds;

    protected override Task<IReadOnlyList<NormalizedMatch>> CollectAsync(CancellationToken ct) =>
        _provider.GetLiveMatchesAsync(ct);
}
