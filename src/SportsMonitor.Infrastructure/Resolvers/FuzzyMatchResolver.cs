using SportsMonitor.Domain.Interfaces;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace SportsMonitor.Infrastructure.Resolvers;

public class FuzzyMatchResolver : IMatchResolver
{
    public string ResolveMatchId(
        string sourceMatchId,
        string source,
        string homeTeam,
        string awayTeam,
        DateTime kickOff,
        string competition)
    {
        var key = $"{Normalize(homeTeam)}|{Normalize(awayTeam)}|{kickOff.ToUniversalTime():yyyyMMddHH}|{Normalize(competition)}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }

    private static string Normalize(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().ToLowerInvariant().Trim();
    }
}
