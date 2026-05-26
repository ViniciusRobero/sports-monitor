using FluentAssertions;
using SportsMonitor.Infrastructure.Resolvers;

namespace SportsMonitor.Tests.Resolvers;

public class FuzzyMatchResolverTests
{
    private readonly FuzzyMatchResolver _resolver = new();

    private readonly DateTime _kickOff = new(2026, 5, 26, 21, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ResolveMatchId_SameMatchFromDifferentSources_ReturnsSameId()
    {
        var idFromSofaScore  = _resolver.ResolveMatchId("ss:123",  "sofascore",  "Flamengo", "Palmeiras", _kickOff, "Brasileirão");
        var idFrom365Scores  = _resolver.ResolveMatchId("365:456", "365scores",  "Flamengo", "Palmeiras", _kickOff, "Brasileirão");
        var idFromApiFootball = _resolver.ResolveMatchId("af:789", "api_football","Flamengo", "Palmeiras", _kickOff, "Brasileirão");

        idFromSofaScore.Should().Be(idFrom365Scores);
        idFrom365Scores.Should().Be(idFromApiFootball);
    }

    [Fact]
    public void ResolveMatchId_DifferentMatch_ReturnsDifferentId()
    {
        var id1 = _resolver.ResolveMatchId("ss:1", "sofascore", "Flamengo", "Palmeiras", _kickOff, "Brasileirão");
        var id2 = _resolver.ResolveMatchId("ss:2", "sofascore", "Corinthians", "São Paulo", _kickOff, "Brasileirão");

        id1.Should().NotBe(id2);
    }

    [Fact]
    public void ResolveMatchId_TeamNameWithAccents_NormalizesCorrectly()
    {
        var id1 = _resolver.ResolveMatchId("ss:1", "sofascore", "São Paulo", "Atlético-MG", _kickOff, "Brasileirão");
        var id2 = _resolver.ResolveMatchId("ss:2", "365scores", "Sao Paulo", "Atletico-MG", _kickOff, "Brasileirão");

        id1.Should().Be(id2);
    }

    [Fact]
    public void ResolveMatchId_TeamNameCaseInsensitive_ReturnsSameId()
    {
        var id1 = _resolver.ResolveMatchId("ss:1", "sofascore", "flamengo", "palmeiras", _kickOff, "Brasileirão");
        var id2 = _resolver.ResolveMatchId("ss:2", "365scores", "FLAMENGO", "PALMEIRAS", _kickOff, "Brasileirão");

        id1.Should().Be(id2);
    }

    [Fact]
    public void ResolveMatchId_IsIdempotent_SameInputReturnsSameId()
    {
        var id1 = _resolver.ResolveMatchId("ss:1", "sofascore", "Flamengo", "Palmeiras", _kickOff, "Brasileirão");
        var id2 = _resolver.ResolveMatchId("ss:1", "sofascore", "Flamengo", "Palmeiras", _kickOff, "Brasileirão");

        id1.Should().Be(id2);
    }
}
