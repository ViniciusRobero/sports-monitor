using System.Net;
using FluentAssertions;
using SportsMonitor.Domain.Configuration;
using SportsMonitor.Infrastructure.Providers;
using SportsMonitor.Infrastructure.Resolvers;

namespace SportsMonitor.Tests.Providers;

public class ApiFootballProviderTests
{
    [Fact]
    public async Task GetLiveMatchesAsync_MapsFixtureToNormalizedMatch()
    {
        var json = """
        {
          "response": [
            {
              "fixture": {
                "id": 123,
                "date": "2026-05-26T21:00:00+00:00",
                "status": { "short": "1H" }
              },
              "league": { "name": "Brasileirao Serie A" },
              "teams": {
                "home": { "name": "Flamengo" },
                "away": { "name": "Palmeiras" }
              },
              "goals": { "home": 1, "away": 0 },
              "events": [
                {
                  "time": { "elapsed": 32 },
                  "team": { "name": "Flamengo" },
                  "player": { "name": "Pedro" },
                  "type": "Goal",
                  "detail": "Normal Goal"
                }
              ]
            }
          ]
        }
        """;
        var http = new HttpClient(new StubHandler(json))
        {
            BaseAddress = new Uri("https://example.test")
        };
        var provider = new ApiFootballProvider(http, new ApiFootballOptions(), new FuzzyMatchResolver());

        var result = await provider.GetLiveMatchesAsync(CancellationToken.None);

        result.Should().ContainSingle();
        var match = result[0];
        match.Source.Should().Be("api_football");
        match.HomeTeam.Should().Be("Flamengo");
        match.AwayTeam.Should().Be("Palmeiras");
        match.Competition.Should().Be("Brasileirao Serie A");
        match.HomeScore.Should().Be(1);
        match.AwayScore.Should().Be(0);
        match.Events.Should().ContainSingle(e => e.Minute == 32 && e.PlayerName == "Pedro");
        match.RawJson.Should().Be(json);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly string _json;

        public StubHandler(string json)
        {
            _json = json;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.RequestUri!.PathAndQuery.Should().Be("/fixtures?live=all");

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_json)
            });
        }
    }
}
