using System.Collections.Concurrent;
using SportsMonitor.Domain.Models;

namespace SportsMonitor.Infrastructure.Stores;

public class GoogleSnapshotStore
{
    private readonly ConcurrentDictionary<string, GoogleSearchSnapshot> _data = new();

    public void Upsert(GoogleSearchSnapshot snapshot) =>
        _data[snapshot.MatchId] = snapshot;

    public IReadOnlyList<GoogleSearchSnapshot> GetAll() =>
        _data.Values.ToList();

    public GoogleSearchSnapshot? GetForMatch(string matchId) =>
        _data.TryGetValue(matchId, out var snap) ? snap : null;
}
