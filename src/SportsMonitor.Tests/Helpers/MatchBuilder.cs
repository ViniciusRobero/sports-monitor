using SportsMonitor.Domain.Models;

namespace SportsMonitor.Tests.Helpers;

/// <summary>
/// Fluent builder para NormalizedMatch em testes — evita repetição de construtores longos.
/// </summary>
public class MatchBuilder
{
    private string _matchId = "match-001";
    private string _homeTeam = "Flamengo";
    private string _awayTeam = "Palmeiras";
    private string _competition = "Brasileirão Serie A";
    private DateTime _kickOff = new(2026, 5, 26, 21, 0, 0, DateTimeKind.Utc);
    private int _homeScore = 0;
    private int _awayScore = 0;
    private MatchStatus _status = MatchStatus.Live;
    private readonly List<MatchEvent> _events = [];
    private string _source = "api_football";

    public static MatchBuilder Create() => new();

    public MatchBuilder WithMatchId(string id) { _matchId = id; return this; }
    public MatchBuilder WithTeams(string home, string away) { _homeTeam = home; _awayTeam = away; return this; }
    public MatchBuilder WithScore(int home, int away) { _homeScore = home; _awayScore = away; return this; }
    public MatchBuilder WithStatus(MatchStatus s) { _status = s; return this; }
    public MatchBuilder WithSource(string s) { _source = s; return this; }
    public MatchBuilder WithGoal(int minute, string player, string team = "home")
    {
        _events.Add(new MatchEvent(EventType.Goal, minute, player, team));
        return this;
    }
    public MatchBuilder WithYellowCard(int minute, string player, string team = "home")
    {
        _events.Add(new MatchEvent(EventType.YellowCard, minute, player, team));
        return this;
    }

    public NormalizedMatch Build() => new(
        _matchId, _homeTeam, _awayTeam, _competition,
        _kickOff, _homeScore, _awayScore, _status,
        _events.AsReadOnly(), _source, DateTime.UtcNow
    );
}
