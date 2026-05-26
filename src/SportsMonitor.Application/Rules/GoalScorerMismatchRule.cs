using SportsMonitor.Domain.Interfaces;
using SportsMonitor.Domain.Models;

namespace SportsMonitor.Application.Rules;

public class GoalScorerMismatchRule : IDivergenceRule
{
    private const int MinuteTolerance = 2;

    public IEnumerable<Divergence> Check(NormalizedMatch a, NormalizedMatch b)
    {
        var goalsA = a.Events.Where(e => e.Type == EventType.Goal).ToList();
        var goalsB = b.Events.Where(e => e.Type == EventType.Goal).ToList();

        foreach (var goalA in goalsA)
        {
            var match = goalsB.FirstOrDefault(g => Math.Abs(g.Minute - goalA.Minute) <= MinuteTolerance);
            if (match is null) continue;

            if (!NamesOverlap(goalA.PlayerName, match.PlayerName))
                yield return new Divergence(
                    Guid.NewGuid(),
                    a.MatchId, a.HomeTeam, a.AwayTeam,
                    DivergenceType.GoalScorerMismatch,
                    Severity.Critical,
                    a.Source, $"{goalA.Minute}' {goalA.PlayerName}",
                    b.Source, $"{match.Minute}' {match.PlayerName}",
                    OfficialSourceValue: null,
                    DateTime.UtcNow
                );
        }
    }

    private static bool NamesOverlap(string a, string b)
    {
        var partsA = Parts(a);
        var partsB = Parts(b);
        return partsA.Any(p => partsB.Contains(p, StringComparer.OrdinalIgnoreCase));
    }

    private static string[] Parts(string name) =>
        name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
}
