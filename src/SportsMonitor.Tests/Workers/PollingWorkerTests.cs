using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SportsMonitor.Domain.Interfaces;
using SportsMonitor.Domain.Models;
using SportsMonitor.Infrastructure.Stores;
using SportsMonitor.Tests.Helpers;
using SportsMonitor.Workers.Base;

namespace SportsMonitor.Tests.Workers;

public class PollingWorkerTests
{
    [Fact]
    public async Task CollectOnceAsync_SavesSnapshotsAndUpdatesStore()
    {
        var store = new InMemorySnapshotStore();
        var history = new RecordingHistoryRepository();
        var match = MatchBuilder.Create()
            .WithMatchId("match-001")
            .WithSource("api_football")
            .Build();
        var worker = new TestPollingWorker([match], store, history);

        var count = await worker.CollectOnceAsync(CancellationToken.None);

        count.Should().Be(1);
        history.Snapshots.Should().ContainSingle();
        store.GetAllForMatch("match-001").Should().ContainSingle();
    }

    private sealed class TestPollingWorker : PollingWorker
    {
        private readonly IReadOnlyList<NormalizedMatch> _matches;

        public TestPollingWorker(
            IReadOnlyList<NormalizedMatch> matches,
            ISnapshotStore store,
            IMatchHistoryRepository history)
            : base(store, history, NullLogger.Instance)
        {
            _matches = matches;
        }

        protected override string WorkerName => "TestPollingWorker";
        protected override bool IsEnabled => true;
        protected override int PollingIntervalSeconds => 60;

        protected override Task<IReadOnlyList<NormalizedMatch>> CollectAsync(CancellationToken ct) =>
            Task.FromResult(_matches);
    }

    private sealed class RecordingHistoryRepository : IMatchHistoryRepository
    {
        public List<NormalizedMatch> Snapshots { get; } = [];

        public Task SaveSnapshotAsync(NormalizedMatch match, CancellationToken ct)
        {
            Snapshots.Add(match);
            return Task.CompletedTask;
        }

        public Task SaveDivergenceAsync(Divergence divergence, CancellationToken ct) =>
            Task.CompletedTask;

        public Task UpdateVerificationAsync(Guid divergenceId, VerificationUpdate update, CancellationToken ct) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<Divergence>> GetRecentDivergencesAsync(int limit, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Divergence>>([]);
    }
}
