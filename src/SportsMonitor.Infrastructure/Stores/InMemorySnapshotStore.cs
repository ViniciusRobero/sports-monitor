using SportsMonitor.Domain.Interfaces;
using SportsMonitor.Domain.Models;
using System.Collections.Concurrent;

namespace SportsMonitor.Infrastructure.Stores;

public class InMemorySnapshotStore : ISnapshotStore
{
    private static readonly TimeSpan MaximumLiveMatchAge = TimeSpan.FromHours(6);
    private static readonly TimeSpan MaximumSnapshotAge = TimeSpan.FromMinutes(5);

    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, NormalizedMatch>> _data = new();
    private readonly TimeProvider _timeProvider;

    public InMemorySnapshotStore(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

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

    public IReadOnlyList<string> GetLiveMatchIds()
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        PruneExpiredSnapshots(now);

        return _data
            .Where(kv => kv.Value.Values.Any(m => IsCurrentLiveSnapshot(m, now)))
            .Select(kv => kv.Key)
            .ToList();
    }

    private void PruneExpiredSnapshots(DateTime now)
    {
        foreach (var (matchId, bySource) in _data)
        {
            foreach (var (source, match) in bySource)
            {
                if (IsExpiredSnapshot(match, now))
                    bySource.TryRemove(source, out _);
            }

            if (bySource.IsEmpty)
                _data.TryRemove(matchId, out _);
        }
    }

    private static bool IsExpiredSnapshot(NormalizedMatch match, DateTime now) =>
        match.KickOff < now - MaximumLiveMatchAge ||
        match.CollectedAt < now - MaximumSnapshotAge;

    private static bool IsCurrentLiveSnapshot(NormalizedMatch match, DateTime now) =>
        (match.Status == MatchStatus.Live || match.Status == MatchStatus.HalfTime) &&
        !IsExpiredSnapshot(match, now);

    public void RemoveMatch(string matchId) =>
        _data.TryRemove(matchId, out _);
}
