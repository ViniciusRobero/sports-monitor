using SportsMonitor.Domain.Models;

namespace SportsMonitor.Domain.Interfaces;

public interface IMatchHistoryRepository
{
    Task SaveSnapshotAsync(NormalizedMatch match, CancellationToken ct);
    Task SaveDivergenceAsync(Divergence divergence, CancellationToken ct);
    Task UpdateVerificationAsync(Guid divergenceId, VerificationUpdate update, CancellationToken ct);
    Task<IReadOnlyList<Divergence>> GetRecentDivergencesAsync(int limit, CancellationToken ct);
}
