using FluentAssertions;
using SportsMonitor.Application.Rules;
using SportsMonitor.Domain.Models;
using SportsMonitor.Tests.Helpers;

namespace SportsMonitor.Tests.Rules;

public class ScoreMismatchRuleTests
{
    private readonly ScoreMismatchRule _rule = new();

    [Fact]
    public void Check_WhenScoresAreEqual_ReturnsNoDivergences()
    {
        var a = MatchBuilder.Create().WithSource("sofascore").WithScore(1, 0).Build();
        var b = MatchBuilder.Create().WithSource("365scores").WithScore(1, 0).Build();

        var result = _rule.Check(a, b);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Check_WhenHomeScoresDiffer_ReturnsCriticalDivergence()
    {
        var a = MatchBuilder.Create().WithSource("sofascore").WithScore(2, 0).Build();
        var b = MatchBuilder.Create().WithSource("365scores").WithScore(1, 0).Build();

        var result = _rule.Check(a, b).ToList();

        result.Should().HaveCount(1);
        result[0].Type.Should().Be(DivergenceType.ScoreMismatch);
        result[0].Severity.Should().Be(Severity.Critical);
        result[0].SourceA.Should().Be("sofascore");
        result[0].SourceAValue.Should().Be("2-0");
        result[0].SourceB.Should().Be("365scores");
        result[0].SourceBValue.Should().Be("1-0");
    }

    [Fact]
    public void Check_WhenAwayScoresDiffer_ReturnsCriticalDivergence()
    {
        var a = MatchBuilder.Create().WithSource("api_football").WithScore(0, 1).Build();
        var b = MatchBuilder.Create().WithSource("sofascore").WithScore(0, 2).Build();

        var result = _rule.Check(a, b).ToList();

        result.Should().HaveCount(1);
        result[0].Type.Should().Be(DivergenceType.ScoreMismatch);
        result[0].SourceAValue.Should().Be("0-1");
        result[0].SourceBValue.Should().Be("0-2");
    }

    [Fact]
    public void Check_WhenBothScoresDiffer_ReturnsSingleDivergence()
    {
        var a = MatchBuilder.Create().WithSource("sofascore").WithScore(2, 1).Build();
        var b = MatchBuilder.Create().WithSource("365scores").WithScore(1, 2).Build();

        var result = _rule.Check(a, b).ToList();

        // Um único ScoreMismatch — não dois
        result.Should().HaveCount(1);
        result[0].Type.Should().Be(DivergenceType.ScoreMismatch);
    }

    [Fact]
    public void Check_DivergenceContainsCorrectMatchInfo()
    {
        var a = MatchBuilder.Create()
            .WithMatchId("match-abc")
            .WithTeams("Flamengo", "Palmeiras")
            .WithSource("sofascore")
            .WithScore(1, 0)
            .Build();
        var b = MatchBuilder.Create()
            .WithMatchId("match-abc")
            .WithTeams("Flamengo", "Palmeiras")
            .WithSource("365scores")
            .WithScore(0, 0)
            .Build();

        var result = _rule.Check(a, b).ToList();

        result[0].MatchId.Should().Be("match-abc");
        result[0].HomeTeam.Should().Be("Flamengo");
        result[0].AwayTeam.Should().Be("Palmeiras");
        result[0].Id.Should().NotBe(Guid.Empty);
        result[0].DetectedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
