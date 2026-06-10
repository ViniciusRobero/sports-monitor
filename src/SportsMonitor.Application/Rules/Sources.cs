using SportsMonitor.Domain.Models;

namespace SportsMonitor.Application.Rules;

// Display labels shared by the divergence rules so every Description reads consistently.
internal static class Sources
{
    private static readonly Dictionary<string, string> Labels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["sofascore"] = "SofaScore",
        ["365scores"] = "365Scores",
        ["bet365"] = "Bet365",
        ["api_football"] = "API Football",
        ["google"] = "Google",
    };

    public static string Label(string source) =>
        Labels.TryGetValue(source, out var label) ? label : source;

    public static string Status(MatchStatus status) => status switch
    {
        MatchStatus.Live => "Ao Vivo",
        MatchStatus.HalfTime => "Intervalo",
        MatchStatus.Finished => "Encerrada",
        MatchStatus.NotStarted => "Não iniciada",
        MatchStatus.Postponed => "Adiada",
        MatchStatus.Cancelled => "Cancelada",
        _ => status.ToString()
    };
}
