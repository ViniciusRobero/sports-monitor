using FluentAssertions;
using SportsMonitor.Domain.Models;
using SportsMonitor.Infrastructure.Stores;
using SportsMonitor.Tests.Helpers;

namespace SportsMonitor.Tests.Stores;

public class InMemorySnapshotStoreTests
{
    private readonly InMemorySnapshotStore _store = new();

    [Fact]
    public void Upsert_WhenMatchAdded_EventFires()
    {
        NormalizedMatch? received = null;
        _store.SnapshotUpdated += m => received = m;

        var match = MatchBuilder.Create().WithSource("sofascore").Build();
        _store.Upsert(match);

        received.Should().NotBeNull();
        received!.Source.Should().Be("sofascore");
    }

    [Fact]
    public void Upsert_WhenSameMatchFromTwoSources_BothAreStored()
    {
        var a = MatchBuilder.Create().WithMatchId("m1").WithSource("sofascore").Build();
        var b = MatchBuilder.Create().WithMatchId("m1").WithSource("365scores").Build();

        _store.Upsert(a);
        _store.Upsert(b);

        var all = _store.GetAllForMatch("m1");
        all.Should().HaveCount(2);
        all.Select(m => m.Source).Should().BeEquivalentTo(["sofascore", "365scores"]);
    }

    [Fact]
    public void Upsert_WhenSameSourceUpdates_ReplacesExisting()
    {
        var original = MatchBuilder.Create().WithMatchId("m1").WithSource("sofascore").WithScore(0, 0).Build();
        var updated  = MatchBuilder.Create().WithMatchId("m1").WithSource("sofascore").WithScore(1, 0).Build();

        _store.Upsert(original);
        _store.Upsert(updated);

        var all = _store.GetAllForMatch("m1");
        all.Should().HaveCount(1);
        all[0].HomeScore.Should().Be(1);
    }

    [Fact]
    public void GetAllForMatch_WhenMatchDoesNotExist_ReturnsEmpty()
    {
        _store.GetAllForMatch("nonexistent").Should().BeEmpty();
    }

    [Fact]
    public void GetLiveMatchIds_ReturnsAllMatchIds()
    {
        _store.Upsert(MatchBuilder.Create().WithMatchId("m1").WithSource("sofascore").Build());
        _store.Upsert(MatchBuilder.Create().WithMatchId("m2").WithSource("sofascore").Build());

        _store.GetLiveMatchIds().Should().BeEquivalentTo(["m1", "m2"]);
    }

    [Fact]
    public void RemoveMatch_RemovesMatchFromStore()
    {
        _store.Upsert(MatchBuilder.Create().WithMatchId("m1").WithSource("sofascore").Build());
        _store.RemoveMatch("m1");

        _store.GetAllForMatch("m1").Should().BeEmpty();
        _store.GetLiveMatchIds().Should().NotContain("m1");
    }

    [Fact]
    public void Upsert_EventFiresImmediately_NotAsync()
    {
        var fired = false;
        _store.SnapshotUpdated += _ => fired = true;

        _store.Upsert(MatchBuilder.Create().Build());

        // Deve ter disparado de forma síncrona antes desta linha
        fired.Should().BeTrue();
    }
}
