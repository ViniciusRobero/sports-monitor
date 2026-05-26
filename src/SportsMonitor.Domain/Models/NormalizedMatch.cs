namespace SportsMonitor.Domain.Models;

public record NormalizedMatch(
    string MatchId,
    string HomeTeam,
    string AwayTeam,
    string Competition,
    DateTime KickOff,
    int HomeScore,
    int AwayScore,
    MatchStatus Status,
    IReadOnlyList<MatchEvent> Events,
    string Source,
    DateTime CollectedAt,
    string RawJson = ""
);
