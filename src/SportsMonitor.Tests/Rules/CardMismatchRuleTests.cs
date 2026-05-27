using FluentAssertions;
using SportsMonitor.Application.Rules;
using SportsMonitor.Domain.Models;
using SportsMonitor.Tests.Helpers;

namespace SportsMonitor.Tests.Rules;

public class CardMismatchRuleTests
{
    private readonly CardMismatchRule _rule = new();

    [Fact]
    public void Check_WhenYellowCardPlayersMatch_ReturnsNoDivergences()
    {
        var a = MatchBuilder.Create().WithSource("sofascore").WithYellowCard(45, "Zé Rafael").Build();
        var b = MatchBuilder.Create().WithSource("bet365").WithYellowCard(45, "Zé Rafael").Build();

        _rule.Check(a, b).Should().BeEmpty();
    }

    [Fact]
    public void Check_WhenYellowCardPlayersDiffer_ReturnsHighDivergence()
    {
        var a = MatchBuilder.Create().WithSource("sofascore").WithYellowCard(45, "Zé Rafael").Build();
        var b = MatchBuilder.Create().WithSource("bet365").WithYellowCard(45, "Gerson").Build();

        var result = _rule.Check(a, b).ToList();

        result.Should().ContainSingle();
        result[0].Type.Should().Be(DivergenceType.YellowCardMismatch);
        result[0].Severity.Should().Be(Severity.High);
    }

    [Fact]
    public void Check_WhenRedCardPlayersDiffer_ReturnsRedCardMismatch()
    {
        var a = MatchBuilder.Create().WithSource("sofascore").WithRedCard(60, "Fabrício Bruno").Build();
        var b = MatchBuilder.Create().WithSource("bet365").WithRedCard(60, "Murilo").Build();

        var result = _rule.Check(a, b).ToList();

        result.Should().ContainSingle();
        result[0].Type.Should().Be(DivergenceType.RedCardMismatch);
    }

    [Fact]
    public void Check_WhenCardMinutesAreFarApart_DoesNotCompare()
    {
        var a = MatchBuilder.Create().WithSource("sofascore").WithYellowCard(30, "Zé Rafael").Build();
        var b = MatchBuilder.Create().WithSource("bet365").WithYellowCard(60, "Gerson").Build();

        _rule.Check(a, b).Should().BeEmpty();
    }

    [Fact]
    public void Check_WithLastNameOverlap_TreatsAsSamePlayer()
    {
        var a = MatchBuilder.Create().WithSource("sofascore").WithYellowCard(45, "Rafael").Build();
        var b = MatchBuilder.Create().WithSource("bet365").WithYellowCard(45, "Zé Rafael").Build();

        _rule.Check(a, b).Should().BeEmpty();
    }
}
