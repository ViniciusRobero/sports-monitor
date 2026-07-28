using SportsMonitor.Domain.Models;

namespace SportsMonitor.Bff.Services;

public sealed class RelayStatusTracker(TimeProvider timeProvider)
{
    private static readonly TimeSpan DegradedAfter = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan DownAfter = TimeSpan.FromMinutes(5);

    private readonly object _gate = new();
    private DateTime? _lastSuccessUtc;
    private int _totalCollected;

    public void RecordSuccess(int collected)
    {
        lock (_gate)
        {
            _lastSuccessUtc = timeProvider.GetUtcNow().UtcDateTime;
            _totalCollected += collected;
        }
    }

    public ProviderStatus GetStatus(bool isConfigured)
    {
        lock (_gate)
        {
            if (!isConfigured)
            {
                return CreateStatus(
                    ProviderHealth.Down,
                    "Relay desprotegido: configure RelayOptions:AgentKey no servidor.");
            }

            if (_lastSuccessUtc is null)
            {
                return CreateStatus(
                    ProviderHealth.Down,
                    "LocalAgent ainda não enviou dados desde que o servidor iniciou.");
            }

            var elapsed = timeProvider.GetUtcNow().UtcDateTime - _lastSuccessUtc.Value;
            if (elapsed >= DownAfter)
            {
                return CreateStatus(
                    ProviderHealth.Down,
                    $"LocalAgent desconectado: último envio há {FormatElapsed(elapsed)}.");
            }

            if (elapsed >= DegradedAfter)
            {
                return CreateStatus(
                    ProviderHealth.Degraded,
                    $"LocalAgent atrasado: último envio há {FormatElapsed(elapsed)}.");
            }

            return CreateStatus(ProviderHealth.Healthy, null);
        }
    }

    private ProviderStatus CreateStatus(ProviderHealth health, string? error) => new()
    {
        Name = "SofaScore LocalAgent",
        Enabled = true,
        Health = health,
        ConsecutiveFailures = health == ProviderHealth.Healthy ? 0 : 1,
        TotalCollected = _totalCollected,
        LastSuccessUtc = _lastSuccessUtc,
        LastError = error
    };

    private static string FormatElapsed(TimeSpan elapsed) =>
        elapsed.TotalMinutes < 2
            ? $"{Math.Max(1, (int)elapsed.TotalSeconds)} segundos"
            : $"{(int)elapsed.TotalMinutes} minutos";
}
