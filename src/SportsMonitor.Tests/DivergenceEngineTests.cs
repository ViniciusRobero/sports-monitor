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

    [Fact]
    public async Task EvaluateAsync_WhenCalledMultipleTimesWithSameDivergence_EmitsOnlyOnce()
    {
        var store = new InMemorySnapshotStore();
        var history = new RecordingHistoryRepository();
        var channel = Channel.CreateUnbounded<Divergence>();
        var engine = new DivergenceEngine(store, [new ScoreMismatchRule()], history, channel.Writer);

        var a = MatchBuilder.Create().WithMatchId("m1").WithSource("sofascore").WithScore(1, 0).Build();
        var b = MatchBuilder.Create().WithMatchId("m1").WithSource("bet365").WithScore(0, 0).Build();
        store.Upsert(a);
        store.Upsert(b);

        await engine.EvaluateAsync(b);
        await engine.EvaluateAsync(b);
        await engine.EvaluateAsync(b);

        history.Divergences.Should().HaveCount(1);
        channel.Reader.Count.Should().Be(1);
    }

    [Fact]
    public async Task EvaluateAsync_AfterDedupWindow_EmitsDivergenceAgain()
    {
        var fakeClock = new FakeClock(DateTimeOffset.UtcNow);
        var store = new InMemorySnapshotStore();
        var history = new RecordingHistoryRepository();
        var channel = Channel.CreateUnbounded<Divergence>();
        var engine = new DivergenceEngine(
            store, [new ScoreMismatchRule()], history, channel.Writer, () => fakeClock.Now);

        var a = MatchBuilder.Create().WithMatchId("m1").WithSource("sofascore").WithScore(1, 0).Build();
        var b = MatchBuilder.Create().WithMatchId("m1").WithSource("bet365").WithScore(0, 0).Build();
        store.Upsert(a);
        store.Upsert(b);

        await engine.EvaluateAsync(b);
        fakeClock.Advance(TimeSpan.FromMinutes(6));
        await engine.EvaluateAsync(b);

        history.Divergences.Should().HaveCount(2);
    }

    [Fact]
    public async Task EvaluateAsync_BidirectionalUpdates_EmitsSingleDivergence()
    {
        var store = new InMemorySnapshotStore();
        var history = new RecordingHistoryRepository();
        var channel = Channel.CreateUnbounded<Divergence>();
        var engine = new DivergenceEngine(store, [new ScoreMismatchRule()], history, channel.Writer);

        var bet365 = MatchBuilder.Create().WithMatchId("m1").WithSource("bet365").WithScore(1, 0).Build();
        var sofascore = MatchBuilder.Create().WithMatchId("m1").WithSource("sofascore").WithScore(0, 0).Build();
        store.Upsert(bet365);
        store.Upsert(sofascore);

        // bet365 atualiza → compara contra sofascore
        await engine.EvaluateAsync(bet365);
        // sofascore atualiza → compara contra bet365 (direção inversa)
        await engine.EvaluateAsync(sofascore);

        history.Divergences.Should().HaveCount(1);
    }

    [Fact]
    public async Task EvaluateAsync_DifferentDivergenceTypes_AreNotDeduplicated()
    {
        var store = new InMemorySnapshotStore();
        var history = new RecordingHistoryRepository();
        var channel = Channel.CreateUnbounded<Divergence>();
        var engine = new DivergenceEngine(
            store,
            [new ScoreMismatchRule(), new MatchStatusMismatchRule()],
            history,
            channel.Writer);

        var live = MatchBuilder.Create().WithMatchId("m1").WithSource("sofascore")
            .WithScore(1, 0).WithStatus(MatchStatus.Live).Build();
        var finished = MatchBuilder.Create().WithMatchId("m1").WithSource("bet365")
            .WithScore(0, 0).WithStatus(MatchStatus.Finished).Build();
        store.Upsert(live);
        store.Upsert(finished);

        await engine.EvaluateAsync(finished);

        history.Divergences.Should().HaveCount(2);
        history.Divergences.Select(d => d.Type).Should()
            .Contain(DivergenceType.ScoreMismatch)
            .And.Contain(DivergenceType.MatchStatusMismatch);
    }

    private sealed class FakeClock(DateTimeOffset initial)
    {
        public DateTimeOffset Now { get; private set; } = initial;
        public void Advance(TimeSpan span) => Now = Now.Add(span);
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
