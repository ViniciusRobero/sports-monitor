using System.Text.Json;
using SportsMonitor.Domain.Interfaces;
using SportsMonitor.Domain.Models;

namespace SportsMonitor.Infrastructure.Repositories;

public class JsonlMatchHistoryRepository : IMatchHistoryRepository
{
    private static readonly SemaphoreSlim FileLock = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _basePath;

    public JsonlMatchHistoryRepository(string basePath)
    {
        _basePath = basePath;
        Directory.CreateDirectory(_basePath);
    }

    public async Task SaveSnapshotAsync(NormalizedMatch match, CancellationToken ct)
    {
        var path = SnapshotPath(match.CollectedAt, match.Source);
        var line = JsonSerializer.Serialize(match, _jsonOptions);

        await AppendLineAsync(path, line, ct);
    }

    public async Task SaveDivergenceAsync(Divergence divergence, CancellationToken ct)
    {
        var path = DivergencePath(divergence.DetectedAt);
        var line = JsonSerializer.Serialize(divergence, _jsonOptions);

        await AppendLineAsync(path, line, ct);
    }

    public async Task UpdateVerificationAsync(Guid divergenceId, VerificationUpdate update, CancellationToken ct)
    {
        var path = DivergencePath(DateTime.UtcNow);
        if (!File.Exists(path))
            return;

        await FileLock.WaitAsync(ct);
        try
        {
            var lines = await File.ReadAllLinesAsync(path, ct);
            var changed = false;

            for (var i = 0; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;

                var divergence = JsonSerializer.Deserialize<Divergence>(lines[i], _jsonOptions);
                if (divergence is null || divergence.Id != divergenceId)
                    continue;

                var updated = divergence with
                {
                    VerificationStatus = update.Status,
                    ReplayLink = update.ReplayLink,
                    AnalystNotes = update.AnalystNotes
                };

                lines[i] = JsonSerializer.Serialize(updated, _jsonOptions);
                changed = true;
                break;
            }

            if (changed)
                await File.WriteAllLinesAsync(path, lines, ct);
        }
        finally
        {
            FileLock.Release();
        }
    }

    public async Task<IReadOnlyList<Divergence>> GetRecentDivergencesAsync(int limit, CancellationToken ct)
    {
        if (limit <= 0)
            return [];

        var path = DivergencePath(DateTime.UtcNow);
        if (!File.Exists(path))
            return [];

        var lines = await File.ReadAllLinesAsync(path, ct);

        return lines
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<Divergence>(line, _jsonOptions))
            .Where(divergence => divergence is not null)
            .Cast<Divergence>()
            .TakeLast(limit)
            .ToList();
    }

    private string SnapshotPath(DateTime collectedAt, string source) =>
        Path.Combine(_basePath, collectedAt.ToUniversalTime().ToString("yyyy-MM-dd"), "snapshots", $"{source}.jsonl");

    private string DivergencePath(DateTime detectedAt) =>
        Path.Combine(_basePath, detectedAt.ToUniversalTime().ToString("yyyy-MM-dd"), "divergences.jsonl");

    private static async Task AppendLineAsync(string path, string line, CancellationToken ct)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await FileLock.WaitAsync(ct);
        try
        {
            await File.AppendAllTextAsync(path, line + Environment.NewLine, ct);
        }
        finally
        {
            FileLock.Release();
        }
    }
}
