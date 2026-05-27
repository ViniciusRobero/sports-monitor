namespace SportsMonitor.Domain.Configuration;

public class SofaScoreOptions : ProviderOptions
{
    public string BaseUrl { get; set; } = "https://api.sofascore.com";
    public string UserAgent { get; set; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36";
}
