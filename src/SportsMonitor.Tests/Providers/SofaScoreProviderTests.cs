using System.Net;
using FluentAssertions;
using SportsMonitor.Domain.Configuration;
using SportsMonitor.Domain.Models;
using SportsMonitor.Infrastructure.Providers;
using SportsMonitor.Infrastructure.Resolvers;

namespace SportsMonitor.Tests.Providers;

public class SofaScoreProviderTests
{
    private const string LiveEventsJson = """
        {
          "events": [
            {
              "id": 99001,
              "homeTeam": { "name": "Flamengo" },
              "awayTeam": { "name": "Palmeiras" },
              "tournament": { "name": "Brasileirao Serie A" },
              "startTimestamp": 1748995200,
              "homeScore": { "current": 1 },
              "awayScore": { "current": 0 },
              "status": { "code": 1, "description": "1st half" }
            }
          ]
        }
        """;

    private const string IncidentsJson = """
        {
          "incidents": [
            {
              "incidentType": "goal",
              "incidentClass": "regular",
              "time": 32,
              "isHome": true,
              "player": { "name": "Pedro" }
            },
            {
              "incidentType": "card",
              "incidentClass": "yellow",
              "time": 45,
              "isHome": false,
              "player": { "name": "Zé Rafael" }
            },
            {
              "incidentType": "goal",
              "incidentClass": "ownGoal",
              "time": 55,
              "isHome": false,
              "player": { "name": "Murilo" }
            }
          ]
        }
        """;

    [Fact]
    public async Task GetLiveMatchesAsync_MapsMatchCorrectly()
    {
        var provider = BuildProvider(LiveEventsJson, IncidentsJson);

        var result = await provider.GetLiveMatchesAsync(CancellationToken.None);

        result.Should().ContainSingle();
        var match = result[0];
        match.Source.Should().Be("sofascore");
        match.HomeTeam.Should().Be("Flamengo");
        match.AwayTeam.Should().Be("Palmeiras");
        match.HomeScore.Should().Be(1);
        match.AwayScore.Should().Be(0);
        match.Status.Should().Be(MatchStatus.Live);
    }

    [Fact]
    public async Task GetLiveMatchesAsync_MapsGoalIncident()
    {
        var provider = BuildProvider(LiveEventsJson, IncidentsJson);

        var result = await provider.GetLiveMatchesAsync(CancellationToken.None);

        result[0].Events.Should().Contain(e =>
            e.Type == EventType.Goal && e.PlayerName == "Pedro" && e.Minute == 32 && e.Team == "home");
    }

    [Fact]
    public async Task GetLiveMatchesAsync_MapsYellowCard()
    {
        var provider = BuildProvider(LiveEventsJson, IncidentsJson);

        var result = await provider.GetLiveMatchesAsync(CancellationToken.None);

        result[0].Events.Should().Contain(e =>
            e.Type == EventType.YellowCard && e.PlayerName == "Zé Rafael" && e.Team == "away");
    }

    [Fact]
    public async Task GetLiveMatchesAsync_MapsOwnGoal()
    {
        var provider = BuildProvider(LiveEventsJson, IncidentsJson);

        var result = await provider.GetLiveMatchesAsync(CancellationToken.None);

        result[0].Events.Should().Contain(e =>
            e.Type == EventType.OwnGoal && e.PlayerName == "Murilo");
    }

    private static SofaScoreProvider BuildProvider(string liveJson, string incidentsJson)
    {
        var handler = new SequentialStubHandler(new[]
        {
            (HttpStatusCode.OK, liveJson),
            (HttpStatusCode.OK, incidentsJson)
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://example.test") };
        return new SofaScoreProvider(http, new SofaScoreOptions(), new FuzzyMatchResolver());
    }

    private sealed class SequentialStubHandler : HttpMessageHandler
    {
        private readonly (HttpStatusCode status, string body)[] _responses;
        private int _index;

        public SequentialStubHandler((HttpStatusCode, string)[] responses) => _responses = responses;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var (status, body) = _responses[_index++ % _responses.Length];
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body)
            });
        }
    }
}
