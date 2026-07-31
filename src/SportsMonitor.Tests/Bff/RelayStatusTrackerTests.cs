using FluentAssertions;
using SportsMonitor.Bff.Services;
using SportsMonitor.Domain.Models;

namespace SportsMonitor.Tests.Bff;

public class RelayStatusTrackerTests
{
    private readonly MutableTimeProvider _time = new(
        new DateTimeOffset(2026, 7, 28, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void ReportsDownWhenRelayKeyIsNotConfigured()
    {
        var status = new RelayStatusTracker(_time).GetStatus(isConfigured: false);

        status.Health.Should().Be(ProviderHealth.Down);
        status.LastError.Should().Contain("configure");
    }

    [Fact]
    public void ReportsDownBeforeFirstLocalAgentRelay()
    {
        var status = new RelayStatusTracker(_time).GetStatus(isConfigured: true);

        status.Health.Should().Be(ProviderHealth.Down);
        status.LastSuccessUtc.Should().BeNull();
    }

    [Fact]
    public void TracksSuccessfulRelayAndBecomesStale()
    {
        var tracker = new RelayStatusTracker(_time);
        tracker.RecordSuccess(4);

        var healthy = tracker.GetStatus(isConfigured: true);
        healthy.Health.Should().Be(ProviderHealth.Healthy);
        healthy.TotalCollected.Should().Be(4);

        _time.Advance(TimeSpan.FromMinutes(3));
        tracker.GetStatus(isConfigured: true).Health.Should().Be(ProviderHealth.Degraded);

        _time.Advance(TimeSpan.FromMinutes(3));
        tracker.GetStatus(isConfigured: true).Health.Should().Be(ProviderHealth.Down);
    }

    private sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;

        public void Advance(TimeSpan elapsed) => utcNow += elapsed;
    }
}
