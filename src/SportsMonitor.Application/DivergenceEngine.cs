using System.Collections.Concurrent;
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
    private readonly Func<DateTimeOffset> _clock;

    private readonly ConcurrentDictionary<string, DateTimeOffset> _recentDivergences = new();
    private static readonly TimeSpan DedupWindow = TimeSpan.FromMinutes(5);

    public DivergenceEngine(
        ISnapshotStore store,
        IEnumerable<IDivergenceRule> rules,
        IMatchHistoryRepository history,
        ChannelWriter<Divergence> alertQueue,
        Func<DateTimeOffset>? clock = null)
    {
        _store = store;
        _rules = rules;
        _history = history;
        _alertQueue = alertQueue;
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
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
                    var key = BuildDedupKey(divergence);
                    var now = _clock();

                    if (_recentDivergences.TryGetValue(key, out var lastSeen)
                        && now - lastSeen < DedupWindow)
                    {
                        continue;
                    }

                    _recentDivergences[key] = now;
                    await _history.SaveDivergenceAsync(divergence, ct);
                    await _alertQueue.WriteAsync(divergence, ct);
                }
            }
        }
    }

    private static string BuildDedupKey(Divergence d)
    {
        string ordA, valA, ordB, valB;
        if (string.Compare(d.SourceA, d.SourceB, StringComparison.Ordinal) <= 0)
            (ordA, valA, ordB, valB) = (d.SourceA, d.SourceAValue, d.SourceB, d.SourceBValue);
        else
            (ordA, valA, ordB, valB) = (d.SourceB, d.SourceBValue, d.SourceA, d.SourceAValue);

        return $"{d.MatchId}_{d.Type}_{ordA}_{ordB}_{valA}_{valB}";
    }
}
