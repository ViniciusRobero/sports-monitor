using System.Threading.Channels;
using FluentAssertions;
using SportsMonitor.Application;
using SportsMonitor.Application.Rules;
using SportsMonitor.Domain.Interfaces;
using SportsMonitor.Domain.Models;
using SportsMonitor.Infrastructure.Stores;
using SportsMonitor.Tests.Helpers;

namespace SportsMonitor.Tests;

public class DivergenceEngineTests
{
    [Fact]
    public async Task EvaluateAsync_WhenScoresDiffer_SavesAndQueuesDivergence()
    {
        var store = new InMemorySnapshotStore();
        var history = new RecordingHistoryRepository();
        var channel = Channel.CreateUnbounded<Divergence>();
        var engine = new DivergenceEngine(
            store,
            [new ScoreMismatchRule()],
            history,
            channel.Writer);

        var first = MatchBuilder.Create()
            .WithMatchId("match-001")
            .WithSource("sofascore")
            .WithScore(1, 0)
            .Build();
        var updated = MatchBuilder.Create()
            .WithMatchId("match-001")
            .WithSource("365scores")
            .WithScore(0, 0)
            .Build();

        store.Upsert(first);
        store.Upsert(updated);
        await engine.EvaluateAsync(updated);

        history.Divergences.Should().ContainSingle();
        history.Divergences[0].Type.Should().Be(DivergenceType.ScoreMismatch);

        channel.Reader.TryRead(out var queued).Should().BeTrue();
        queued!.Type.Should().Be(DivergenceType.ScoreMismatch);
    }

    [Fact]
    public async Task EvaluateAsync_WhenOnlyOneSourceExists_DoesNothing()
    {
        var store = new InMemorySnapshotStore();
        var history = new RecordingHistoryRepository();
        var channel = Channel.CreateUnbounded<Divergence>();
        var engine = new DivergenceEngine(
            store,
            [new ScoreMismatchRule()],
            history,
            channel.Writer);

        var match = MatchBuilder.Create().WithSource("sofascore").Build();

        store.Upsert(match);
        await engine.EvaluateAsync(match);

        history.Divergences.Should().BeEmpty();
        channel.Reader.TryRead(out _).Should().BeFalse();
    }

    private sealed class RecordingHistoryRepository : IMatchHistoryRepository
    {
        public List<Divergence> Divergences { get; } = [];

        public Task SaveSnapshotAsync(NormalizedMatch match, CancellationToken ct) =>
            Task.CompletedTask;

        public Task SaveDivergenceAsync(Divergence divergence, CancellationToken ct)
        {
            Divergences.Add(divergence);
            return Task.CompletedTask;
        }

        public Task UpdateVerificationAsync(Guid divergenceId, VerificationUpdate update, CancellationToken ct) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<Divergence>> GetRecentDivergencesAsync(int limit, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Divergence>>(Divergences.TakeLast(limit).ToList());
    }
}
