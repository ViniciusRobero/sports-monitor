using Microsoft.AspNetCore.SignalR;
using SportsMonitor.Bff.Hubs;
using SportsMonitor.Domain.Interfaces;
using SportsMonitor.Domain.Models;

namespace SportsMonitor.Bff.Alerts;

public class SignalRAlertChannel : IAlertChannel
{
    private readonly IHubContext<AlertHub> _hub;

    public SignalRAlertChannel(IHubContext<AlertHub> hub)
    {
        _hub = hub;
    }

    public Task SendAsync(Divergence divergence, CancellationToken ct) =>
        _hub.Clients.All.SendAsync("DivergenceDetected", divergence, ct);
}
