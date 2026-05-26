using SportsMonitor.Domain.Models;

namespace SportsMonitor.Domain.Interfaces;

public interface IMatchDataProvider
{
    string Name { get; }
    Task<IReadOnlyList<NormalizedMatch>> GetLiveMatchesAsync(CancellationToken ct);
}
