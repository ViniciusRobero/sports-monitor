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
    string RawJson = "",
    // Whether this source exposes individual events (goals, cards, scorers).
    // Score-only sources like 365Scores set this false so event-level rules skip them.
    bool ProvidesEvents = true
);
