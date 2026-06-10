using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using SportsMonitor.Domain.Configuration;
using SportsMonitor.Domain.Models;

namespace SportsMonitor.Infrastructure.Services;

public class GoogleSearchService(HttpClient http, GoogleSearchOptions options)
{
    public async Task<IReadOnlyList<GoogleSearchResult>> SearchAsync(string query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey) ||
            options.ApiKey.StartsWith("SUBSTITUIR") ||
            string.IsNullOrWhiteSpace(options.SearchEngineId) ||
            options.SearchEngineId.StartsWith("SUBSTITUIR"))
            throw new InvalidOperationException(
                "Google Search credentials not configured. Set Providers:Google:ApiKey and Providers:Google:SearchEngineId in appsettings.Production.json.");

        var url = $"https://www.googleapis.com/customsearch/v1" +
                  $"?key={options.ApiKey}" +
                  $"&cx={options.SearchEngineId}" +
                  $"&q={Uri.EscapeDataString(query)}" +
                  $"&num={options.ResultsPerMatch}";

        using var response = await http.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"Google API error {(int)response.StatusCode}: {body[..Math.Min(300, body.Length)]}");
        }

        var result = await response.Content.ReadFromJsonAsync<GoogleApiResponse>(ct);
        return result?.Items?
            .Select(i => new GoogleSearchResult(i.Title ?? "", i.Snippet ?? "", i.Link ?? ""))
            .ToList() ?? [];
    }

    private record GoogleApiResponse([property: JsonPropertyName("items")] List<GoogleApiItem>? Items);
    private record GoogleApiItem(
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("snippet")] string? Snippet,
        [property: JsonPropertyName("link")] string? Link);
}
