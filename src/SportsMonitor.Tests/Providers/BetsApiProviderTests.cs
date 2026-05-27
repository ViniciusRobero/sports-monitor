using System.Net;
using FluentAssertions;
using SportsMonitor.Domain.Configuration;
using SportsMonitor.Domain.Models;
using SportsMonitor.Infrastructure.Providers;
using SportsMonitor.Infrastructure.Resolvers;

namespace SportsMonitor.Tests.Providers;

public class BetsApiProviderTests
{
    // Sample payload based on BetsAPI public documentation structure.
    // TODO: replace ST nodes with real payload once API key is confirmed.
    private const string InplayFilterJson = """
        {
          "success": 1,
          "results": [
            { "id": "12345", "league": { "name": "Brasileirao" } }
          ]
        }
        """;

    private static string EventJson(string score = "1-0", string goalLabel = "Pedro - Goal 32'") => $$"""
        {
          "success": 1,
          "results": [
            { "type": "EV", "NA": "Flamengo v Palmeiras", "CT": "Brasileirao Serie A", "SS": "{{score}}", "LM": "32" },
            { "type": "ST", "ET": "IGoal",       "TM": "32", "TI": "1", "LA": "{{goalLabel}}" },
            { "type": "ST", "ET": "IYellowCard", "TM": "45", "TI": "2", "LA": "Zé Rafael - Yellow Card 45'" }
          ]
        }
        """;

    [Fact]
    public async Task GetLiveMatchesAsync_MapsEventToNormalizedMatch()
    {
        var provider = BuildProvider(InplayFilterJson, EventJson());

        var result = await provider.GetLiveMatchesAsync(CancellationToken.None);

        result.Should().ContainSingle();
        var match = result[0];
        match.Source.Should().Be("bet365");
        match.HomeTeam.Should().Be("Flamengo");
        match.AwayTeam.Should().Be("Palmeiras");
        match.HomeScore.Should().Be(1);
        match.AwayScore.Should().Be(0);
    }

    [Fact]
    public async Task GetLiveMatchesAsync_MapsGoalEventWithPlayerName()
    {
        var provider = BuildProvider(InplayFilterJson, EventJson(goalLabel: "Pedro - Goal 32'"));

        var result = await provider.GetLiveMatchesAsync(CancellationToken.None);

        var match = result[0];
        match.Events.Should().Contain(e => e.Type == EventType.Goal && e.PlayerName == "Pedro" && e.Minute == 32);
    }

    [Fact]
    public async Task GetLiveMatchesAsync_MapsYellowCard()
    {
        var provider = BuildProvider(InplayFilterJson, EventJson());

        var result = await provider.GetLiveMatchesAsync(CancellationToken.None);

        var match = result[0];
        match.Events.Should().Contain(e => e.Type == EventType.YellowCard && e.PlayerName == "Zé Rafael");
    }

    [Theory]
    [InlineData("Pedro - Goal 32'", "Pedro")]
    [InlineData("Pedro (Arrascaeta) - Goal 32'", "Pedro (Arrascaeta)")]
    [InlineData("Zé Rafael - Yellow Card 45'", "Zé Rafael")]
    [InlineData("Pedro", "Pedro")]
    [InlineData("", "")]
    public void ExtractPlayerName_ParsesLabelFormats(string label, string expected)
    {
        BetsApiProvider.ExtractPlayerName(label).Should().Be(expected);
    }

    private static BetsApiProvider BuildProvider(string inplayJson, string eventJson)
    {
        var handler = new SequentialStubHandler(new[]
        {
            (HttpStatusCode.OK, inplayJson),
            (HttpStatusCode.OK, eventJson)
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://example.test") };
        return new BetsApiProvider(http, new BetsApiOptions(), new FuzzyMatchResolver());
    }

    private sealed class SequentialStubHandler : HttpMessageHandler
    {
        private readonly (HttpStatusCode status, string body)[] _responses;
        private int _index;

        public SequentialStubHandler((HttpStatusCode, string)[] responses) => _responses = responses;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var (status, body) = _responses[_index++ % _responses.Length];
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body)
            });
        }
    }
}
