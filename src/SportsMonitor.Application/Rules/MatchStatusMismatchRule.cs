using SportsMonitor.Domain.Interfaces;
using SportsMonitor.Domain.Models;

namespace SportsMonitor.Application.Rules;

public class MatchStatusMismatchRule : IDivergenceRule
{
    // Only alert when one source says the match is still live and another says it ended.
    // Ignore minor transitions (NotStarted↔Live) which happen naturally during kickoff.
    public IEnumerable<Divergence> Check(NormalizedMatch a, NormalizedMatch b)
    {
        if (a.Status == b.Status)
            yield break;

        if (!IsSignificantMismatch(a.Status, b.Status))
            yield break;

        yield return new Divergence(
            Guid.NewGuid(),
            a.MatchId, a.HomeTeam, a.AwayTeam,
            DivergenceType.MatchStatusMismatch,
            Severity.Medium,
            a.Source, a.Status.ToString(),
            b.Source, b.Status.ToString(),
            OfficialSourceValue: null,
            DateTime.UtcNow
        );
    }

    // Significant = one source thinks it's live/halftime, the other thinks it's over/postponed
    private static bool IsSignificantMismatch(MatchStatus a, MatchStatus b)
    {
        var live = new[] { MatchStatus.Live, MatchStatus.HalfTime };
        var terminal = new[] { MatchStatus.Finished, MatchStatus.Postponed, MatchStatus.Cancelled };

        return (live.Contains(a) && terminal.Contains(b)) ||
               (terminal.Contains(a) && live.Contains(b));
    }
}
