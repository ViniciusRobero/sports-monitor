namespace SportsMonitor.Domain.Models;

public enum DivergenceType
{
    ScoreMismatch,
    GoalScorerMismatch,
    MissingGoalEvent,
    YellowCardMismatch,
    RedCardMismatch,
    MatchStatusMismatch,
    SubstitutionMismatch,
    VARDecisionMismatch
}
