using System.Net.Http.Json;
using System.Text.Json.Serialization;
using SportsMonitor.Domain.Configuration;
using SportsMonitor.Domain.Models;

namespace SportsMonitor.Infrastructure.Services;

public class GoogleSearchService(HttpClient http, GoogleSearchOptions options)
{
    public async Task<IReadOnlyList<GoogleSearchResult>> SearchAsync(string query, CancellationToken ct)
    {
        var url = $"https://www.googleapis.com/customsearch/v1" +
                  $"?key={options.ApiKey}" +
                  $"&cx={options.SearchEngineId}" +
                  $"&q={Uri.EscapeDataString(query)}" +
                  $"&num={options.ResultsPerMatch}";

        var response = await http.GetFromJsonAsync<GoogleApiResponse>(url, ct);
        return response?.Items?
            .Select(i => new GoogleSearchResult(i.Title ?? "", i.Snippet ?? "", i.Link ?? ""))
            .ToList() ?? [];
    }

    private record GoogleApiResponse([property: JsonPropertyName("items")] List<GoogleApiItem>? Items);
    private record GoogleApiItem(
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("snippet")] string? Snippet,
        [property: JsonPropertyName("link")] string? Link);
}
