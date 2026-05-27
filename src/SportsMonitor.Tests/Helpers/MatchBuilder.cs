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
    private DateTime _collectedAt = DateTime.UtcNow;

    public static MatchBuilder Create() => new();

    public MatchBuilder WithMatchId(string id) { _matchId = id; return this; }
    public MatchBuilder WithTeams(string home, string away) { _homeTeam = home; _awayTeam = away; return this; }
    public MatchBuilder WithScore(int home, int away) { _homeScore = home; _awayScore = away; return this; }
    public MatchBuilder WithStatus(MatchStatus s) { _status = s; return this; }
    public MatchBuilder WithSource(string s) { _source = s; return this; }
    public MatchBuilder WithCollectedAt(DateTime dt) { _collectedAt = dt; return this; }
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
    public MatchBuilder WithRedCard(int minute, string player, string team = "home")
    {
        _events.Add(new MatchEvent(EventType.RedCard, minute, player, team));
        return this;
    }

    public NormalizedMatch Build() => new(
        _matchId, _homeTeam, _awayTeam, _competition,
        _kickOff, _homeScore, _awayScore, _status,
        _events.AsReadOnly(), _source, _collectedAt
    );
}

public class DivergenceBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _matchId = "match-001";
    private string _homeTeam = "Flamengo";
    private string _awayTeam = "Palmeiras";
    private DivergenceType _type = DivergenceType.ScoreMismatch;
    private Severity _severity = Severity.Critical;
    private string _sourceA = "sofascore";
    private string _sourceAValue = "1-0";
    private string _sourceB = "365scores";
    private string _sourceBValue = "0-0";
    private DateTime _detectedAt = DateTime.UtcNow;
    private VerificationStatus _verificationStatus = VerificationStatus.Pending;

    public static DivergenceBuilder Create() => new();

    public DivergenceBuilder WithId(Guid id) { _id = id; return this; }
    public DivergenceBuilder WithDetectedAt(DateTime dt) { _detectedAt = dt; return this; }
    public DivergenceBuilder WithStatus(VerificationStatus status) { _verificationStatus = status; return this; }

    public Divergence Build() => new(
        _id, _matchId, _homeTeam, _awayTeam,
        _type, _severity,
        _sourceA, _sourceAValue,
        _sourceB, _sourceBValue,
        OfficialSourceValue: null,
        _detectedAt,
        _verificationStatus,
        ReplayLink: null,
        AnalystNotes: null
    );
}
