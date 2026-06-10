using SportsMonitor.Domain.Interfaces;
using SportsMonitor.Domain.Models;

namespace SportsMonitor.Application.Rules;

public class ScoreMismatchRule : IDivergenceRule
{
    public IEnumerable<Divergence> Check(NormalizedMatch a, NormalizedMatch b)
    {
        if (a.HomeScore == b.HomeScore && a.AwayScore == b.AwayScore)
            yield break;

        var valA = $"{a.HomeScore}-{a.AwayScore}";
        var valB = $"{b.HomeScore}-{b.AwayScore}";

        yield return new Divergence(
            Guid.NewGuid(),
            a.MatchId, a.HomeTeam, a.AwayTeam,
            DivergenceType.ScoreMismatch,
            Severity.Critical,
            a.Source, valA,
            b.Source, valB,
            OfficialSourceValue: null,
            DateTime.UtcNow,
            Description: $"Placar diferente — {Sources.Label(a.Source)}: {valA}, {Sources.Label(b.Source)}: {valB} " +
                        "(uma fonte registrou um gol que a outra ainda não)"
        );
    }
}
