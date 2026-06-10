using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SportsMonitor.Domain.Interfaces;
using SportsMonitor.Domain.Models;

namespace SportsMonitor.Application;

public class DivergenceEngine
{
    private readonly ISnapshotStore _store;
    private readonly IEnumerable<IDivergenceRule> _rules;
    private readonly IMatchHistoryRepository _history;
    private readonly ChannelWriter<Divergence> _alertQueue;
    private readonly Func<DateTimeOffset> _clock;
    private readonly ILogger<DivergenceEngine> _logger;

    private readonly ConcurrentDictionary<string, DateTimeOffset> _recentDivergences = new();
    private static readonly TimeSpan DedupWindow = TimeSpan.FromMinutes(5);

    public DivergenceEngine(
        ISnapshotStore store,
        IEnumerable<IDivergenceRule> rules,
        IMatchHistoryRepository history,
        ChannelWriter<Divergence> alertQueue,
        Func<DateTimeOffset>? clock = null,
        ILogger<DivergenceEngine>? logger = null)
    {
        _store = store;
        _rules = rules;
        _history = history;
        _alertQueue = alertQueue;
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _logger = logger ?? NullLogger<DivergenceEngine>.Instance;
    }

    public async Task EvaluateAsync(NormalizedMatch updated, CancellationToken ct = default)
    {
        var otherSources = _store.GetAllForMatch(updated.MatchId)
            .Where(match => match.Source != updated.Source)
            .ToList();

        // Cross-source visibility: log who covers this match so single-source games are obvious.
        if (otherSources.Count == 0)
        {
            _logger.LogDebug("Match {Home} x {Away}: only 1 source ({Source}) — no comparison.",
                updated.HomeTeam, updated.AwayTeam, Label(updated.Source));
        }
        else
        {
            _logger.LogDebug("Match {Home} x {Away}: comparing {Source} vs [{Others}].",
                updated.HomeTeam, updated.AwayTeam, Label(updated.Source),
                string.Join(", ", otherSources.Select(o => Label(o.Source))));
        }

        foreach (var other in otherSources)
        {
            foreach (var rule in _rules)
            {
                foreach (var divergence in rule.Check(updated, other))
                {
                    var key = BuildDedupKey(divergence);
                    var now = _clock();

                    if (_recentDivergences.TryGetValue(key, out var lastSeen)
                        && now - lastSeen < DedupWindow)
                    {
                        _logger.LogDebug("Divergence suppressed (dedup): {Description}",
                            Describe(divergence));
                        continue;
                    }

                    _recentDivergences[key] = now;
                    _logger.LogInformation("Divergence [{Type}/{Severity}] {Home} x {Away}: {Description}",
                        divergence.Type, divergence.Severity, divergence.HomeTeam, divergence.AwayTeam,
                        Describe(divergence));
                    await _history.SaveDivergenceAsync(divergence, ct);
                    await _alertQueue.WriteAsync(divergence, ct);
                }
            }
        }
    }

    private static string Describe(Divergence d) =>
        string.IsNullOrWhiteSpace(d.Description)
            ? $"{d.SourceA}: {d.SourceAValue} != {d.SourceB}: {d.SourceBValue}"
            : d.Description;

    private static string Label(string source) => source switch
    {
        "sofascore" => "SofaScore",
        "365scores" => "365Scores",
        "bet365" => "Bet365",
        "api_football" => "API Football",
        "google" => "Google",
        _ => source
    };

    private static string BuildDedupKey(Divergence d)
    {
        string ordA, valA, ordB, valB;
        if (string.Compare(d.SourceA, d.SourceB, StringComparison.Ordinal) <= 0)
            (ordA, valA, ordB, valB) = (d.SourceA, d.SourceAValue, d.SourceB, d.SourceBValue);
        else
            (ordA, valA, ordB, valB) = (d.SourceB, d.SourceBValue, d.SourceA, d.SourceAValue);

        return $"{d.MatchId}_{d.Type}_{ordA}_{ordB}_{valA}_{valB}";
    }
}
