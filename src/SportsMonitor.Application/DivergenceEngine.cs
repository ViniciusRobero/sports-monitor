using System.Threading.Channels;
using SportsMonitor.Domain.Interfaces;
using SportsMonitor.Domain.Models;

namespace SportsMonitor.Application;

public class DivergenceEngine
{
    private readonly ISnapshotStore _store;
    private readonly IEnumerable<IDivergenceRule> _rules;
    private readonly IMatchHistoryRepository _history;
    private readonly ChannelWriter<Divergence> _alertQueue;

    public DivergenceEngine(
        ISnapshotStore store,
        IEnumerable<IDivergenceRule> rules,
        IMatchHistoryRepository history,
        ChannelWriter<Divergence> alertQueue)
    {
        _store = store;
        _rules = rules;
        _history = history;
        _alertQueue = alertQueue;
    }

    public async Task EvaluateAsync(NormalizedMatch updated, CancellationToken ct = default)
    {
        var otherSources = _store.GetAllForMatch(updated.MatchId)
            .Where(match => match.Source != updated.Source)
            .ToList();

        foreach (var other in otherSources)
        {
            foreach (var rule in _rules)
            {
                foreach (var divergence in rule.Check(updated, other))
                {
                    await _history.SaveDivergenceAsync(divergence, ct);
                    await _alertQueue.WriteAsync(divergence, ct);
                }
            }
        }
    }
}
