using System.Threading.Channels;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SportsMonitor.Domain.Interfaces;
using SportsMonitor.Domain.Models;
using SportsMonitor.Tests.Helpers;
using SportsMonitor.Workers;

namespace SportsMonitor.Tests.Workers;

public class AlertWorkerTests
{
    [Fact]
    public async Task DispatchAsync_SendsDivergenceToAllChannels()
    {
        var first = new RecordingAlertChannel();
        var second = new RecordingAlertChannel();
        var worker = new AlertWorker(
            Channel.CreateUnbounded<Divergence>().Reader,
            [first, second],
            NullLogger<AlertWorker>.Instance);
        var divergence = DivergenceBuilder.Create().Build();

        await worker.DispatchAsync(divergence, CancellationToken.None);

        first.Sent.Should().ContainSingle().Which.Should().Be(divergence);
        second.Sent.Should().ContainSingle().Which.Should().Be(divergence);
    }

    [Fact]
    public async Task DispatchAsync_WhenOneChannelFails_StillSendsToRemainingChannels()
    {
        var working = new RecordingAlertChannel();
        var worker = new AlertWorker(
            Channel.CreateUnbounded<Divergence>().Reader,
            [new FailingAlertChannel(), working],
            NullLogger<AlertWorker>.Instance);
        var divergence = DivergenceBuilder.Create().Build();

        await worker.DispatchAsync(divergence, CancellationToken.None);

        working.Sent.Should().ContainSingle().Which.Should().Be(divergence);
    }

    private sealed class RecordingAlertChannel : IAlertChannel
    {
        public List<Divergence> Sent { get; } = [];

        public Task SendAsync(Divergence divergence, CancellationToken ct)
        {
            Sent.Add(divergence);
            return Task.CompletedTask;
        }
    }

    private sealed class FailingAlertChannel : IAlertChannel
    {
        public Task SendAsync(Divergence divergence, CancellationToken ct) =>
            throw new InvalidOperationException("Channel failed.");
    }
}
