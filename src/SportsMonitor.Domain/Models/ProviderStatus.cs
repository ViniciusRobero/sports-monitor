namespace SportsMonitor.Domain.Models;

public enum ProviderHealth
{
    Healthy,
    Degraded,
    Down
}

public class ProviderStatus
{
    public string Name { get; set; } = "";
    public bool Enabled { get; set; }
    public ProviderHealth Health { get; set; } = ProviderHealth.Healthy;
    public int ConsecutiveFailures { get; set; }
    public int TotalCollected { get; set; }
    public DateTime? LastSuccessUtc { get; set; }
    public DateTime? LastFailureUtc { get; set; }
    public string? LastError { get; set; }
}
