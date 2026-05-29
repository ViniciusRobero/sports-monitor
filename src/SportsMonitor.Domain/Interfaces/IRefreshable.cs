namespace SportsMonitor.Domain.Interfaces;

public interface IRefreshable
{
    Task<int> CollectOnceAsync(CancellationToken ct);
}
