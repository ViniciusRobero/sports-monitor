using FluentAssertions;
using SportsMonitor.Domain.Models;
using SportsMonitor.Infrastructure.Repositories;
using SportsMonitor.Tests.Helpers;

namespace SportsMonitor.Tests.Repositories;

public class JsonlMatchHistoryRepositoryTests : IDisposable
{
    private readonly string _basePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly JsonlMatchHistoryRepository _repo;

    public JsonlMatchHistoryRepositoryTests()
    {
        _repo = new JsonlMatchHistoryRepository(_basePath);
    }

    public void Dispose() => Directory.Delete(_basePath, recursive: true);

    [Fact]
    public async Task SaveSnapshotAsync_CreatesFileAtExpectedPath()
    {
        var match = MatchBuilder.Create()
            .WithSource("sofascore")
            .WithCollectedAt(new DateTime(2026, 5, 26, 21, 0, 0, DateTimeKind.Utc))
            .Build();

        await _repo.SaveSnapshotAsync(match, CancellationToken.None);

        var expectedPath = Path.Combine(_basePath, "2026-05-26", "snapshots", "sofascore.jsonl");
        File.Exists(expectedPath).Should().BeTrue();
    }

    [Fact]
    public async Task SaveSnapshotAsync_AppendsOneLinePerCall()
    {
        var match = MatchBuilder.Create().WithSource("api_football").Build();

        await _repo.SaveSnapshotAsync(match, CancellationToken.None);
        await _repo.SaveSnapshotAsync(match, CancellationToken.None);

        var file = Path.Combine(_basePath,
            match.CollectedAt.ToString("yyyy-MM-dd"), "snapshots", "api_football.jsonl");
        var lines = await File.ReadAllLinesAsync(file);
        lines.Where(l => !string.IsNullOrWhiteSpace(l)).Should().HaveCount(2);
    }

    [Fact]
    public async Task SaveDivergenceAsync_CreatesFileAtExpectedPath()
    {
        var divergence = DivergenceBuilder.Create()
            .WithDetectedAt(new DateTime(2026, 5, 26, 22, 0, 0, DateTimeKind.Utc))
            .Build();

        await _repo.SaveDivergenceAsync(divergence, CancellationToken.None);

        var expectedPath = Path.Combine(_basePath, "2026-05-26", "divergences.jsonl");
        File.Exists(expectedPath).Should().BeTrue();
    }

    [Fact]
    public async Task GetRecentDivergencesAsync_ReturnsLastNDivergences()
    {
        for (var i = 0; i < 5; i++)
            await _repo.SaveDivergenceAsync(DivergenceBuilder.Create().Build(), CancellationToken.None);

        var result = await _repo.GetRecentDivergencesAsync(3, CancellationToken.None);

        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetRecentDivergencesAsync_WhenNoFile_ReturnsEmpty()
    {
        var result = await _repo.GetRecentDivergencesAsync(10, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateVerificationAsync_UpdatesStatusAndNotes()
    {
        var divergence = DivergenceBuilder.Create().Build();
        await _repo.SaveDivergenceAsync(divergence, CancellationToken.None);

        var update = new VerificationUpdate(
            VerificationStatus.Confirmed, "https://youtube.com/clip", "Goal confirmed", null);

        await _repo.UpdateVerificationAsync(divergence.Id, update, CancellationToken.None);

        var result = await _repo.GetRecentDivergencesAsync(10, CancellationToken.None);
        var updated = result.Single(d => d.Id == divergence.Id);
        updated.VerificationStatus.Should().Be(VerificationStatus.Confirmed);
        updated.ReplayLink.Should().Be("https://youtube.com/clip");
        updated.AnalystNotes.Should().Be("Goal confirmed");
    }

    [Fact]
    public async Task UpdateVerificationAsync_WhenIdNotFound_DoesNotThrow()
    {
        var act = async () => await _repo.UpdateVerificationAsync(
            Guid.NewGuid(),
            new VerificationUpdate(VerificationStatus.Ignored, null, null, null),
            CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
