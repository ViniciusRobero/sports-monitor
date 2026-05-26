using SportsMonitor.Domain.Interfaces;
using SportsMonitor.Domain.Models;

namespace SportsMonitor.Application.Rules;

public class ScoreMismatchRule : IDivergenceRule
{
    public IEnumerable<Divergence> Check(NormalizedMatch a, NormalizedMatch b)
    {
        if (a.HomeScore == b.HomeScore && a.AwayScore == b.AwayScore)
            yield break;

        yield return new Divergence(
            Guid.NewGuid(),
            a.MatchId, a.HomeTeam, a.AwayTeam,
            DivergenceType.ScoreMismatch,
            Severity.Critical,
            a.Source, $"{a.HomeScore}-{a.AwayScore}",
            b.Source, $"{b.HomeScore}-{b.AwayScore}",
            OfficialSourceValue: null,
            DateTime.UtcNow
        );
    }
}
