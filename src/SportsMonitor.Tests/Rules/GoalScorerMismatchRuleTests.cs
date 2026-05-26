using FluentAssertions;
using SportsMonitor.Application.Rules;
using SportsMonitor.Domain.Models;
using SportsMonitor.Tests.Helpers;

namespace SportsMonitor.Tests.Rules;

public class GoalScorerMismatchRuleTests
{
    private readonly GoalScorerMismatchRule _rule = new();

    [Fact]
    public void Check_WhenGoalScorersMatch_ReturnsNoDivergences()
    {
        var a = MatchBuilder.Create().WithSource("sofascore").WithGoal(32, "Pedro").Build();
        var b = MatchBuilder.Create().WithSource("365scores").WithGoal(32, "Pedro").Build();

        _rule.Check(a, b).Should().BeEmpty();
    }

    [Fact]
    public void Check_WhenGoalScorersAreDifferent_ReturnsCriticalDivergence()
    {
        var a = MatchBuilder.Create().WithSource("sofascore").WithGoal(32, "Pedro").Build();
        var b = MatchBuilder.Create().WithSource("365scores").WithGoal(32, "Arrascaeta").Build();

        var result = _rule.Check(a, b).ToList();

        result.Should().HaveCount(1);
        result[0].Type.Should().Be(DivergenceType.GoalScorerMismatch);
        result[0].Severity.Should().Be(Severity.Critical);
        result[0].SourceAValue.Should().Contain("Pedro");
        result[0].SourceBValue.Should().Contain("Arrascaeta");
    }

    [Fact]
    public void Check_WhenGoalMinuteIsWithinTolerance_StillCompares()
    {
        // Minuto 32 vs 33 — dentro da tolerância de ±2
        var a = MatchBuilder.Create().WithSource("sofascore").WithGoal(32, "Pedro").Build();
        var b = MatchBuilder.Create().WithSource("365scores").WithGoal(33, "Arrascaeta").Build();

        var result = _rule.Check(a, b).ToList();

        result.Should().HaveCount(1);
        result[0].Type.Should().Be(DivergenceType.GoalScorerMismatch);
    }

    [Fact]
    public void Check_WhenGoalMinutesAreFarApart_DoesNotCompare()
    {
        // Minuto 32 vs 40 — provavelmente gols diferentes, não compara
        var a = MatchBuilder.Create().WithSource("sofascore").WithGoal(32, "Pedro").Build();
        var b = MatchBuilder.Create().WithSource("365scores").WithGoal(40, "Arrascaeta").Build();

        _rule.Check(a, b).Should().BeEmpty();
    }

    [Fact]
    public void Check_WhenNoGoalsInEitherMatch_ReturnsNoDivergences()
    {
        var a = MatchBuilder.Create().WithSource("sofascore").WithScore(0, 0).Build();
        var b = MatchBuilder.Create().WithSource("365scores").WithScore(0, 0).Build();

        _rule.Check(a, b).Should().BeEmpty();
    }

    [Fact]
    public void Check_WithLastNameNormalization_SamePlayerDifferentFormat()
    {
        // "Pedro Guilherme" vs "Pedro" — mesmo sobrenome → sem divergência
        var a = MatchBuilder.Create().WithSource("sofascore").WithGoal(32, "Pedro Guilherme").Build();
        var b = MatchBuilder.Create().WithSource("365scores").WithGoal(32, "Pedro").Build();

        _rule.Check(a, b).Should().BeEmpty();
    }
}
