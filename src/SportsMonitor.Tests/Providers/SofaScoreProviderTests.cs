using System.Text.Json;
using FluentAssertions;
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
              "status": { "code": 6, "description": "1st half", "type": "inprogress" }
            }
          ]
        }
        """;

    private const string HalfTimeEventsJson = """
        {
          "events": [
            {
              "id": 99002,
              "homeTeam": { "name": "Flamengo" },
              "awayTeam": { "name": "Palmeiras" },
              "tournament": { "name": "Brasileirao Serie A" },
              "startTimestamp": 1748995200,
              "homeScore": { "current": 1 },
              "awayScore": { "current": 0 },
              "status": { "code": 7, "description": "Halftime", "type": "inprogress" }
            }
          ]
        }
        """;

    private const string FinishedEventsJson = """
        {
          "events": [
            {
              "id": 99003,
              "homeTeam": { "name": "Flamengo" },
              "awayTeam": { "name": "Palmeiras" },
              "tournament": { "name": "Brasileirao Serie A" },
              "startTimestamp": 1748995200,
              "homeScore": { "current": 2 },
              "awayScore": { "current": 1 },
              "status": { "code": 100, "description": "Finished", "type": "finished" }
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
    public void MapMatch_MapsMatchCorrectly()
    {
        var match = MapFirstEvent(LiveEventsJson, IncidentsJson);

        match.Should().NotBeNull();
        match!.Source.Should().Be("sofascore");
        match.HomeTeam.Should().Be("Flamengo");
        match.AwayTeam.Should().Be("Palmeiras");
        match.HomeScore.Should().Be(1);
        match.AwayScore.Should().Be(0);
        match.Status.Should().Be(MatchStatus.Live);
    }

    [Fact]
    public void MapMatch_MapsGoalIncident()
    {
        var match = MapFirstEvent(LiveEventsJson, IncidentsJson);

        match!.Events.Should().Contain(e =>
            e.Type == EventType.Goal && e.PlayerName == "Pedro" && e.Minute == 32 && e.Team == "home");
    }

    [Fact]
    public void MapMatch_MapsYellowCard()
    {
        var match = MapFirstEvent(LiveEventsJson, IncidentsJson);

        match!.Events.Should().Contain(e =>
            e.Type == EventType.YellowCard && e.PlayerName == "Zé Rafael" && e.Team == "away");
    }

    [Fact]
    public void MapMatch_MapsOwnGoal()
    {
        var match = MapFirstEvent(LiveEventsJson, IncidentsJson);

        match!.Events.Should().Contain(e =>
            e.Type == EventType.OwnGoal && e.PlayerName == "Murilo");
    }

    [Fact]
    public void MapMatch_MapsHalftimeStatus()
    {
        var match = MapFirstEvent(HalfTimeEventsJson, IncidentsJson);
        match!.Status.Should().Be(MatchStatus.HalfTime);
    }

    [Fact]
    public void MapMatch_MapsFinishedStatus()
    {
        var match = MapFirstEvent(FinishedEventsJson, IncidentsJson);
        match!.Status.Should().Be(MatchStatus.Finished);
    }

    private static NormalizedMatch? MapFirstEvent(string eventsJson, string incidentsJson)
    {
        using var doc = JsonDocument.Parse(eventsJson);
        var ev = doc.RootElement.GetProperty("events")[0];
        var id = ev.GetProperty("id").GetInt64().ToString();
        return SofaScoreMapper.MapMatch(ev, incidentsJson, id, new FuzzyMatchResolver());
    }
}
