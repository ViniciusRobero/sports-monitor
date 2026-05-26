namespace SportsMonitor.Domain.Models;

public record MatchEvent(
    EventType Type,
    int Minute,
    string PlayerName,
    string Team   // "home" | "away"
);
