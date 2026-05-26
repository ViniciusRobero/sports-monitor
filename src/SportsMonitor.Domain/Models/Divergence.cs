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
    string? AnalystNotes = null
);
