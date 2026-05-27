namespace SportsMonitor.Domain.Models;

public record VerificationUpdate(
    VerificationStatus Status,
    string? ReplayLink,
    string? AnalystNotes,
    string? ManualActionStatus
);
