namespace SportsMonitor.Domain.Models;

public record Divergence(
    Guid Id,
    string MatchId,
    string HomeTeam,
    string AwayTeam,
    DivergenceType Type,
    Severity Severity,
    string SourceA,
    string SourceAValue,
    string SourceB,
    string SourceBValue,
    string? OfficialSourceValue,
    DateTime DetectedAt,
    VerificationStatus VerificationStatus = VerificationStatus.Pending,
    string? ReplayLink = null,
    string? AnalystNotes = null,
    // Human-readable explanation in PT, e.g. "Placar diferente — SofaScore: 1-0, 365Scores: 0-0".
    // Shown in the dashboard card and written to the engine logs.
    string Description = ""
);
