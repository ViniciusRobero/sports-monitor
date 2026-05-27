namespace SportsMonitor.Domain.Configuration;

public class ApiFootballOptions : ProviderOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://v3.football.api-sports.io";
}
