namespace SportsMonitor.Domain.Configuration;

public class ProvidersConfig
{
    public ProviderOptions SofaScore { get; set; } = new();
    public ProviderOptions Api365Scores { get; set; } = new();
    public ApiFootballOptions ApiFootball { get; set; } = new();
    public ProviderOptions Betfair { get; set; } = new();
}
