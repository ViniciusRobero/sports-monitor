namespace SportsMonitor.Domain.Configuration;

public class BetsApiOptions : ProviderOptions
{
    public string Token { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.b365api.com";
}
