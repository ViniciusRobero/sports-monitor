namespace SportsMonitor.Domain.Interfaces;

public interface IMatchResolver
{
    string ResolveMatchId(
        string sourceMatchId,
        string source,
        string homeTeam,
        string awayTeam,
        DateTime kickOff,
        string competition);
}
