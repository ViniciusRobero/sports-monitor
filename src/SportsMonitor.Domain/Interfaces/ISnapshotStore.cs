using SportsMonitor.Domain.Models;

namespace SportsMonitor.Domain.Interfaces;

public interface ISnapshotStore
{
    void Upsert(NormalizedMatch match);
    event Action<NormalizedMatch>? SnapshotUpdated;
    IReadOnlyList<NormalizedMatch> GetAllForMatch(string matchId);
    IReadOnlyList<string> GetLiveMatchIds();
    void RemoveMatch(string matchId);
}
