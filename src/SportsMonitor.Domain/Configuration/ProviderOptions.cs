namespace SportsMonitor.Domain.Configuration;

public class ProviderOptions
{
    public bool Enabled { get; set; } = true;
    public int PollingIntervalSeconds { get; set; } = 60;
}
