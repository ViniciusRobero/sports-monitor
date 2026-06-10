using SportsMonitor.Domain.Interfaces;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace SportsMonitor.Infrastructure.Resolvers;

public class FuzzyMatchResolver : IMatchResolver
{
    // Maps Portuguese/Italian/other-language national team names → canonical code.
    // This allows cross-source grouping when sources use different languages.
    private static readonly Dictionary<string, string> _teamCanonical = new(StringComparer.OrdinalIgnoreCase)
    {
        ["england"] = "ENG", ["inglaterra"] = "ENG",
        ["france"] = "FRA", ["franca"] = "FRA", ["france (f)"] = "FRA",
        ["netherlands"] = "NED", ["holanda"] = "NED", ["holland"] = "NED",
        ["germany"] = "GER", ["alemanha"] = "GER", ["deutschland"] = "GER",
        ["spain"] = "ESP", ["espanha"] = "ESP", ["espana"] = "ESP",
        ["iceland"] = "ISL", ["islandia"] = "ISL",
        ["poland"] = "POL", ["polonia"] = "POL",
        ["ukraine"] = "UKR", ["ucrania"] = "UKR",
        ["ireland"] = "IRL", ["irlanda"] = "IRL",
        ["italy"] = "ITA", ["italia"] = "ITA",
        ["portugal"] = "POR",
        ["brazil"] = "BRA", ["brasil"] = "BRA",
        ["argentina"] = "ARG",
        ["colombia"] = "COL",
        ["mexico"] = "MEX", ["mexic"] = "MEX", ["messico"] = "MEX",
        ["usa"] = "USA", ["united states"] = "USA", ["estados unidos"] = "USA",
        ["scotland"] = "SCO", ["escocia"] = "SCO",
        ["wales"] = "WAL", ["pais de gales"] = "WAL",
        ["belgium"] = "BEL", ["belgica"] = "BEL",
        ["austria"] = "AUT",
        ["switzerland"] = "SUI", ["suica"] = "SUI", ["suiza"] = "SUI",
        ["sweden"] = "SWE", ["suecia"] = "SWE",
        ["norway"] = "NOR", ["noruega"] = "NOR",
        ["denmark"] = "DEN", ["dinamarca"] = "DEN",
        ["finland"] = "FIN", ["finlandia"] = "FIN",
        ["turkey"] = "TUR", ["turquia"] = "TUR",
        ["russia"] = "RUS", ["russia"] = "RUS",
        ["hungary"] = "HUN", ["hungria"] = "HUN",
        ["croatia"] = "CRO", ["croacia"] = "CRO",
        ["serbia"] = "SRB", ["servia"] = "SRB",
        ["slovakia"] = "SVK", ["eslovaquia"] = "SVK",
        ["slovenia"] = "SVN", ["eslovenia"] = "SVN",
        ["czech republic"] = "CZE", ["republica tcheca"] = "CZE",
        ["romania"] = "ROU", ["romenia"] = "ROU",
        ["greece"] = "GRE", ["grecia"] = "GRE",
        ["albania"] = "ALB",
        ["north macedonia"] = "MKD", ["macedonia do norte"] = "MKD",
        ["bosnia"] = "BIH", ["bosnia e herzegovina"] = "BIH",
        ["israel"] = "ISR",
        ["georgia"] = "GEO",
        ["moldova"] = "MDA",
        ["kazakhstan"] = "KAZ",
        ["japan"] = "JPN", ["japao"] = "JPN",
        ["south korea"] = "KOR", ["coreia do sul"] = "KOR",
        ["china"] = "CHN",
        ["australia"] = "AUS",
        ["new zealand"] = "NZL", ["nova zelandia"] = "NZL",
        ["south africa"] = "RSA", ["africa do sul"] = "RSA",
        ["nigeria"] = "NGA",
        ["ghana"] = "GHA",
        ["senegal"] = "SEN",
        ["ivory coast"] = "CIV", ["costa do marfim"] = "CIV",
        ["cameroon"] = "CMR", ["camaroes"] = "CMR",
        ["morocco"] = "MAR", ["marrocos"] = "MAR",
        ["egypt"] = "EGY", ["egito"] = "EGY",
        ["iran"] = "IRN",
        ["saudi arabia"] = "KSA", ["arabia saudita"] = "KSA",
        ["qatar"] = "QAT",
        ["uae"] = "UAE", ["emirados arabes"] = "UAE",
        ["venezuela"] = "VEN",
        ["chile"] = "CHI",
        ["ecuador"] = "ECU",
        ["peru"] = "PER",
        ["uruguay"] = "URU",
        ["paraguay"] = "PAR",
        ["bolivia"] = "BOL",
        ["costa rica"] = "CRC",
        ["honduras"] = "HON",
        ["panama"] = "PAN",
        ["el salvador"] = "SLV",
        ["guatemala"] = "GUA",
        ["canada"] = "CAN",
    };

    public string ResolveMatchId(
        string sourceMatchId,
        string source,
        string homeTeam,
        string awayTeam,
        DateTime kickOff,
        string competition)
    {
        var key = $"{CanonicalizeTeam(homeTeam)}|{CanonicalizeTeam(awayTeam)}|{kickOff.ToUniversalTime():yyyyMMddHH}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }

    private static string CanonicalizeTeam(string name)
    {
        var normalized = Normalize(name);
        // Strip gender/age suffixes: (f), (w), (m), sub-XX, u20, u21, etc.
        var stripped = Regex.Replace(normalized, @"\s*\([fw]\)\s*$", "").Trim();
        stripped = Regex.Replace(stripped, @"\s+(sub[-\s]?\d{2}|u\d{2})\s*$", "").Trim();

        // Try canonical national team code
        if (_teamCanonical.TryGetValue(stripped, out var code))
            return code;
        // Also try the non-stripped version
        if (_teamCanonical.TryGetValue(normalized, out code))
            return code;

        return stripped.Length > 0 ? stripped : normalized;
    }

    private static string Normalize(string value)
    {
        var nfd = value.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(nfd.Length);
        foreach (var c in nfd)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().ToLowerInvariant().Trim();
    }
}
