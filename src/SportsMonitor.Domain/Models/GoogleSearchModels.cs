namespace SportsMonitor.Domain.Models;

public record GoogleSearchResult(string Title, string Snippet, string Url);

public record GoogleSearchSnapshot(
    string MatchId,
    string HomeTeam,
    string AwayTeam,
    string Query,
    IReadOnlyList<GoogleSearchResult> Results,
    DateTime FetchedAt
);
