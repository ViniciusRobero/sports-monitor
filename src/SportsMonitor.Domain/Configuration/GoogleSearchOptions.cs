namespace SportsMonitor.Domain.Configuration;

public class GoogleSearchOptions : ProviderOptions
{
    public string ApiKey { get; set; } = "";
    public string SearchEngineId { get; set; } = "";
    public int ResultsPerMatch { get; set; } = 3;
}
