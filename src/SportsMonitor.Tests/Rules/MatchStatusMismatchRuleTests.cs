using FluentAssertions;
using SportsMonitor.Application.Rules;
using SportsMonitor.Domain.Models;
using SportsMonitor.Tests.Helpers;

namespace SportsMonitor.Tests.Rules;

public class MatchStatusMismatchRuleTests
{
    private readonly MatchStatusMismatchRule _rule = new();

    [Fact]
    public void Check_WhenBothLive_ReturnsNoDivergence()
    {
        var a = MatchBuilder.Create().WithSource("sofascore").WithStatus(MatchStatus.Live).Build();
        var b = MatchBuilder.Create().WithSource("bet365").WithStatus(MatchStatus.Live).Build();

        _rule.Check(a, b).Should().BeEmpty();
    }

    [Fact]
    public void Check_WhenOneFinishedOtherLive_ReturnsMediumDivergence()
    {
        var a = MatchBuilder.Create().WithSource("sofascore").WithStatus(MatchStatus.Finished).Build();
        var b = MatchBuilder.Create().WithSource("bet365").WithStatus(MatchStatus.Live).Build();

        var result = _rule.Check(a, b).ToList();

        result.Should().ContainSingle();
        result[0].Type.Should().Be(DivergenceType.MatchStatusMismatch);
        result[0].Severity.Should().Be(Severity.Medium);
    }

    [Fact]
    public void Check_WhenHalfTimeVsFinished_ReturnsDivergence()
    {
        var a = MatchBuilder.Create().WithSource("sofascore").WithStatus(MatchStatus.HalfTime).Build();
        var b = MatchBuilder.Create().WithSource("bet365").WithStatus(MatchStatus.Finished).Build();

        _rule.Check(a, b).Should().ContainSingle();
    }

    [Fact]
    public void Check_WhenLiveVsHalfTime_ReturnsNoDivergence()
    {
        var a = MatchBuilder.Create().WithSource("sofascore").WithStatus(MatchStatus.Live).Build();
        var b = MatchBuilder.Create().WithSource("bet365").WithStatus(MatchStatus.HalfTime).Build();

        _rule.Check(a, b).Should().BeEmpty();
    }

    [Fact]
    public void Check_WhenPostponedVsLive_ReturnsDivergence()
    {
        var a = MatchBuilder.Create().WithSource("sofascore").WithStatus(MatchStatus.Postponed).Build();
        var b = MatchBuilder.Create().WithSource("bet365").WithStatus(MatchStatus.Live).Build();

        _rule.Check(a, b).Should().ContainSingle();
    }
}
