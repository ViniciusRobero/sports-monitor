using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SportsMonitor.Domain.Interfaces;
using SportsMonitor.Domain.Models;

namespace SportsMonitor.Workers;

public class AlertWorker : BackgroundService
{
    private readonly ChannelReader<Divergence> _queue;
    private readonly IEnumerable<IAlertChannel> _channels;
    private readonly ILogger<AlertWorker> _logger;

    public AlertWorker(
        ChannelReader<Divergence> queue,
        IEnumerable<IAlertChannel> channels,
        ILogger<AlertWorker> logger)
    {
        _queue = queue;
        _channels = channels;
        _logger = logger;
    }

    public async Task DispatchAsync(Divergence divergence, CancellationToken ct)
    {
        foreach (var channel in _channels)
        {
            try
            {
                await channel.SendAsync(divergence, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Alert channel {Channel} failed.", channel.GetType().Name);
            }
        }
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await foreach (var divergence in _queue.ReadAllAsync(ct))
            await DispatchAsync(divergence, ct);
    }
}
