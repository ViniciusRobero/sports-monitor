using SportsMonitor.Domain.Models;

namespace SportsMonitor.Domain.Interfaces;

public interface IDivergenceRule
{
    IEnumerable<Divergence> Check(NormalizedMatch a, NormalizedMatch b);
}
