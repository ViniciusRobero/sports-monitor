using SportsMonitor.Domain.Interfaces;
using SportsMonitor.Domain.Models;

namespace SportsMonitor.Application.Rules;

public class MissingGoalRule : IDivergenceRule
{
    private const int MinuteTolerance = 2;

    public IEnumerable<Divergence> Check(NormalizedMatch a, NormalizedMatch b)
    {
        var goalsA = a.Events.Where(e => e.Type == EventType.Goal).ToList();
        var goalsB = b.Events.Where(e => e.Type == EventType.Goal).ToList();

        // Gols em A sem par em B
        foreach (var goalA in goalsA.Where(ga => !goalsB.Any(gb => Math.Abs(gb.Minute - ga.Minute) <= MinuteTolerance)))
            yield return MissingDivergence(a, b, goalA, missingIn: b.Source);

        // Gols em B sem par em A
        foreach (var goalB in goalsB.Where(gb => !goalsA.Any(ga => Math.Abs(ga.Minute - gb.Minute) <= MinuteTolerance)))
            yield return MissingDivergence(b, a, goalB, missingIn: a.Source);
    }

    private static Divergence MissingDivergence(NormalizedMatch has, NormalizedMatch missing,
        MatchEvent goal, string missingIn) =>
        new(
            Guid.NewGuid(),
            has.MatchId, has.HomeTeam, has.AwayTeam,
            DivergenceType.MissingGoalEvent,
            Severity.High,
            has.Source, $"{goal.Minute}' {goal.PlayerName}",
            missingIn, "—",
            OfficialSourceValue: null,
            DateTime.UtcNow
        );
}
