using FluentAssertions;
using SportsMonitor.Application.Rules;
using SportsMonitor.Domain.Models;
using SportsMonitor.Tests.Helpers;

namespace SportsMonitor.Tests.Rules;

public class MissingGoalRuleTests
{
    private readonly MissingGoalRule _rule = new();

    [Fact]
    public void Check_WhenBothSourcesHaveSameNumberOfGoals_ReturnsNoDivergences()
    {
        var a = MatchBuilder.Create().WithSource("sofascore").WithGoal(32, "Pedro").Build();
        var b = MatchBuilder.Create().WithSource("365scores").WithGoal(32, "Pedro").Build();

        _rule.Check(a, b).Should().BeEmpty();
    }

    [Fact]
    public void Check_WhenSourceAHasGoalMissingInSourceB_ReturnsHighDivergence()
    {
        var a = MatchBuilder.Create().WithSource("sofascore")
            .WithGoal(32, "Pedro").WithGoal(67, "Gabi").Build();
        var b = MatchBuilder.Create().WithSource("365scores")
            .WithGoal(32, "Pedro").Build();  // falta o gol do Gabi

        var result = _rule.Check(a, b).ToList();

        result.Should().HaveCount(1);
        result[0].Type.Should().Be(DivergenceType.MissingGoalEvent);
        result[0].Severity.Should().Be(Severity.High);
        result[0].SourceAValue.Should().Contain("67");
    }

    [Fact]
    public void Check_WhenSourceBHasGoalMissingInSourceA_ReturnsHighDivergence()
    {
        var a = MatchBuilder.Create().WithSource("sofascore").Build(); // sem gols
        var b = MatchBuilder.Create().WithSource("365scores").WithGoal(10, "Endrick").Build();

        var result = _rule.Check(a, b).ToList();

        result.Should().HaveCount(1);
        result[0].Type.Should().Be(DivergenceType.MissingGoalEvent);
    }

    [Fact]
    public void Check_WhenBothHaveNoGoals_ReturnsNoDivergences()
    {
        var a = MatchBuilder.Create().WithSource("sofascore").Build();
        var b = MatchBuilder.Create().WithSource("365scores").Build();

        _rule.Check(a, b).Should().BeEmpty();
    }
}
