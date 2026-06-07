using SportsMonitor.Domain.Interfaces;
using SportsMonitor.Domain.Models;
using System.Collections.Concurrent;

namespace SportsMonitor.Infrastructure.Stores;

public class InMemorySnapshotStore : ISnapshotStore
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, NormalizedMatch>> _data = new();

    public event Action<NormalizedMatch>? SnapshotUpdated;

    public void Upsert(NormalizedMatch match)
    {
        var bySource = _data.GetOrAdd(match.MatchId, _ => new ConcurrentDictionary<string, NormalizedMatch>());
        bySource[match.Source] = match;
        SnapshotUpdated?.Invoke(match);
    }

    public IReadOnlyList<NormalizedMatch> GetAllForMatch(string matchId) =>
        _data.TryGetValue(matchId, out var bySource)
            ? bySource.Values.ToList()
            : [];

    public IReadOnlyList<string> GetLiveMatchIds() =>
        _data
            .Where(kv => kv.Value.Values.Any(m =>
                m.Status == MatchStatus.Live ||
                m.Status == MatchStatus.HalfTime))
            .Select(kv => kv.Key)
            .ToList();

    public void RemoveMatch(string matchId) =>
        _data.TryRemove(matchId, out _);
}
