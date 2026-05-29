using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SportsMonitor.Domain.Configuration;
using SportsMonitor.Domain.Interfaces;
using SportsMonitor.Domain.Models;

namespace SportsMonitor.Workers;

/// <summary>
/// Injects fake live matches and divergences so the dashboard works without real API keys.
/// Disable by setting Demo.Enabled = false in appsettings.json once real tokens are configured.
/// </summary>
public class DemoWorker : BackgroundService
{
    private readonly ISnapshotStore _store;
    private readonly IOptionsMonitor<DemoOptions> _options;
    private readonly ILogger<DemoWorker> _logger;

    private static readonly DateTime Today = DateTime.UtcNow.Date;

    public DemoWorker(
        ISnapshotStore store,
        IOptionsMonitor<DemoOptions> options,
        ILogger<DemoWorker> logger)
    {
        _store = store;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!_options.CurrentValue.Enabled)
        {
            _logger.LogInformation("DemoWorker disabled — using real providers.");
            return;
        }

        _logger.LogInformation("DemoWorker started — injecting demo data every {Interval}s",
            _options.CurrentValue.TickIntervalSeconds);

        // Seed base state immediately so dashboard isn't empty on first load
        SeedBaseMatches();

        int tick = 0;
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(_options.CurrentValue.TickIntervalSeconds), ct);
            InjectScenario(tick++ % 3);
        }
    }

    // ------------------------------------------------------------------
    // Base state — all sources agree, no divergences yet
    // ------------------------------------------------------------------
    private void SeedBaseMatches()
    {
        UpsertMatch("demo-fla-pal", "Flamengo", "Palmeiras", "Brasileirão Serie A",
            homeScore: 0, awayScore: 0, minute: 15, source: "sofascore");
        UpsertMatch("demo-fla-pal", "Flamengo", "Palmeiras", "Brasileirão Serie A",
            homeScore: 0, awayScore: 0, minute: 15, source: "365scores");

        UpsertMatch("demo-bra-arg", "Brasil", "Argentina", "Copa do Mundo 2026",
            homeScore: 0, awayScore: 0, minute: 28, source: "sofascore");
        UpsertMatch("demo-bra-arg", "Brasil", "Argentina", "Copa do Mundo 2026",
            homeScore: 0, awayScore: 0, minute: 28, source: "365scores");

        UpsertMatch("demo-rma-bar", "Real Madrid", "Barcelona", "Champions League",
            homeScore: 1, awayScore: 1, minute: 62, source: "sofascore");
        UpsertMatch("demo-rma-bar", "Real Madrid", "Barcelona", "Champions League",
            homeScore: 1, awayScore: 1, minute: 62, source: "api_football");
    }

    // ------------------------------------------------------------------
    // Divergence scenarios — each one injects conflicting snapshots
    // ------------------------------------------------------------------
    private void InjectScenario(int index)
    {
        switch (index)
        {
            case 0: InjectGoalScorerMismatch(); break;
            case 1: InjectScoreMismatch(); break;
            case 2: InjectCardMismatch(); break;
        }
    }

    private void InjectGoalScorerMismatch()
    {
        _logger.LogInformation("[Demo] Scenario: GoalScorerMismatch — Flamengo x Palmeiras");

        // SofaScore: Pedro scored at 32'
        UpsertMatch("demo-fla-pal", "Flamengo", "Palmeiras", "Brasileirão Serie A",
            homeScore: 1, awayScore: 0, minute: 32, source: "sofascore",
            events:
            [
                new MatchEvent(EventType.Goal, 32, "Pedro", "home")
            ]);

        // Bet365: Arrascaeta scored at 32'
        UpsertMatch("demo-fla-pal", "Flamengo", "Palmeiras", "Brasileirão Serie A",
            homeScore: 1, awayScore: 0, minute: 32, source: "bet365",
            events:
            [
                new MatchEvent(EventType.Goal, 32, "Arrascaeta", "home")
            ]);
    }

    private void InjectScoreMismatch()
    {
        _logger.LogInformation("[Demo] Scenario: ScoreMismatch — Brasil x Argentina");

        // SofaScore: 1-0
        UpsertMatch("demo-bra-arg", "Brasil", "Argentina", "Copa do Mundo 2026",
            homeScore: 1, awayScore: 0, minute: 55, source: "sofascore");

        // 365Scores: still 0-0 (delayed update)
        UpsertMatch("demo-bra-arg", "Brasil", "Argentina", "Copa do Mundo 2026",
            homeScore: 0, awayScore: 0, minute: 55, source: "365scores");
    }

    private void InjectCardMismatch()
    {
        _logger.LogInformation("[Demo] Scenario: CardMismatch — Real Madrid x Barcelona");

        // SofaScore: Bellingham yellow card at 67'
        UpsertMatch("demo-rma-bar", "Real Madrid", "Barcelona", "Champions League",
            homeScore: 1, awayScore: 1, minute: 67, source: "sofascore",
            events:
            [
                new MatchEvent(EventType.YellowCard, 67, "Bellingham", "home")
            ]);

        // API-Football: Vinicius yellow card at 67'
        UpsertMatch("demo-rma-bar", "Real Madrid", "Barcelona", "Champions League",
            homeScore: 1, awayScore: 1, minute: 67, source: "api_football",
            events:
            [
                new MatchEvent(EventType.YellowCard, 67, "Vinicius Jr.", "home")
            ]);
    }

    // ------------------------------------------------------------------
    private void UpsertMatch(
        string matchId, string home, string away, string competition,
        int homeScore, int awayScore, int minute, string source,
        IReadOnlyList<MatchEvent>? events = null)
    {
        _store.Upsert(new NormalizedMatch(
            matchId, home, away, competition,
            Today, homeScore, awayScore,
            MatchStatus.Live,
            events ?? [],
            source,
            DateTime.UtcNow,
            RawJson: "{\"demo\":true}"
        ));
    }
}
