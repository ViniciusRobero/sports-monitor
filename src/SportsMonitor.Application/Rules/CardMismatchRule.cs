using SportsMonitor.Domain.Interfaces;
using SportsMonitor.Domain.Models;

namespace SportsMonitor.Application.Rules;

public class CardMismatchRule : IDivergenceRule
{
    private const int MinuteTolerance = 2;

    public IEnumerable<Divergence> Check(NormalizedMatch a, NormalizedMatch b)
    {
        foreach (var cardType in new[] { EventType.YellowCard, EventType.RedCard })
        {
            var cardsA = a.Events.Where(e => e.Type == cardType).ToList();
            var cardsB = b.Events.Where(e => e.Type == cardType).ToList();

            foreach (var cardA in cardsA)
            {
                var match = cardsB.FirstOrDefault(c => Math.Abs(c.Minute - cardA.Minute) <= MinuteTolerance);
                if (match is null) continue;

                if (!NamesOverlap(cardA.PlayerName, match.PlayerName))
                {
                    var divType = cardType == EventType.YellowCard
                        ? DivergenceType.YellowCardMismatch
                        : DivergenceType.RedCardMismatch;

                    yield return new Divergence(
                        Guid.NewGuid(),
                        a.MatchId, a.HomeTeam, a.AwayTeam,
                        divType,
                        Severity.High,
                        a.Source, $"{cardA.Minute}' {cardA.PlayerName}",
                        b.Source, $"{match.Minute}' {match.PlayerName}",
                        OfficialSourceValue: null,
                        DateTime.UtcNow
                    );
                }
            }
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
