using System.Net;
using FluentAssertions;
using SportsMonitor.Domain.Configuration;
using SportsMonitor.Domain.Models;
using SportsMonitor.Infrastructure.Providers;
using SportsMonitor.Infrastructure.Resolvers;

namespace SportsMonitor.Tests.Providers;

public class Scores365ProviderTests
{
    private static readonly DateTime ProviderNow =
        new(2026, 5, 27, 22, 0, 0, DateTimeKind.Utc);

    private const string LiveGamesJson = """
        {
          "liveGamesCount": 2,
          "games": [
            {
              "id": 4632001,
              "sportId": 1,
              "competitionId": 113,
              "statusGroup": 1,
              "gameTime": 34.0,
              "startTime": "2026-05-27T21:00:00-03:00",
              "homeCompetitor": { "id": 1, "name": "Flamengo", "score": 1.0 },
              "awayCompetitor": { "id": 2, "name": "Palmeiras", "score": 0.0 }
            },
            {
              "id": 4632002,
              "sportId": 2,
              "competitionId": 99,
              "statusGroup": 1,
              "startTime": "2026-05-27T20:00:00-03:00",
              "homeCompetitor": { "id": 3, "name": "Team A", "score": 0.0 },
              "awayCompetitor": { "id": 4, "name": "Team B", "score": 0.0 }
            },
            {
              "id": 4632003,
              "sportId": 1,
              "competitionId": 113,
              "statusGroup": 4,
              "startTime": "2026-05-27T18:00:00-03:00",
              "homeCompetitor": { "id": 5, "name": "São Paulo", "score": 2.0 },
              "awayCompetitor": { "id": 6, "name": "Corinthians", "score": 1.0 }
            }
          ]
        }
        """;

    [Fact]
    public async Task GetLiveMatchesAsync_ReturnsOnlySoccerGames()
    {
        var provider = BuildProvider(LiveGamesJson);

        var result = await provider.GetLiveMatchesAsync(CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(m => m.Source.Should().Be("365scores"));
    }

    [Fact]
    public async Task GetLiveMatchesAsync_MapsTeamNamesAndScores()
    {
        var provider = BuildProvider(LiveGamesJson);

        var result = await provider.GetLiveMatchesAsync(CancellationToken.None);

        var match = result[0];
        match.Source.Should().Be("365scores");
        match.HomeTeam.Should().Be("Flamengo");
        match.AwayTeam.Should().Be("Palmeiras");
        match.HomeScore.Should().Be(1);
        match.AwayScore.Should().Be(0);
    }

    [Fact]
    public async Task GetLiveMatchesAsync_ReturnsEmptyEvents()
    {
        var provider = BuildProvider(LiveGamesJson);

        var result = await provider.GetLiveMatchesAsync(CancellationToken.None);

        result[0].Events.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLiveMatchesAsync_WhenNoGamesArray_ReturnsEmpty()
    {
        var provider = BuildProvider("""{ "liveGamesCount": 0 }""");

        var result = await provider.GetLiveMatchesAsync(CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLiveMatchesAsync_ExcludesGameWithOldKickOff()
    {
        var provider = BuildProvider("""
            {
              "games": [{
                "id": 4632999,
                "sportId": 1,
                "startTime": "2026-05-27T10:00:00Z",
                "homeCompetitor": { "name": "Old Home", "score": 1 },
                "awayCompetitor": { "name": "Old Away", "score": 0 }
              }]
            }
            """);

        var result = await provider.GetLiveMatchesAsync(CancellationToken.None);

        result.Should().BeEmpty();
    }

    private static Scores365Provider BuildProvider(string json)
    {
        var handler = new StubHandler(json);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://example.test") };
        return new Scores365Provider(
            http,
            new Scores365Options(),
            new FuzzyMatchResolver(),
            new FixedTimeProvider(ProviderNow));
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly string _json;
        public StubHandler(string json) => _json = json;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_json)
            });
    }
}
