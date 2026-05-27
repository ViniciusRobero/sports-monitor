using SportsMonitor.Domain.Models;

namespace SportsMonitor.Domain.Interfaces;

public interface IAlertChannel
{
    Task SendAsync(Divergence divergence, CancellationToken ct);
}
