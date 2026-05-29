namespace SportsMonitor.Domain.Configuration;

public class DemoOptions
{
    public bool Enabled { get; set; } = false;
    public int TickIntervalSeconds { get; set; } = 20;
}
