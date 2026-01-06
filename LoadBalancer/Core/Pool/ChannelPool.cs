using LoadBalancer.Core.Backpressure;
using LoadBalancer.Core.Channel;
using LoadBalancer.Core.Factory;
using LoadBalancer.Infrastructure.Config;
using System.Collections.Immutable;

namespace LoadBalancer.Core.Pool;

public sealed class ChannelPool
{
    private ImmutableArray<ManagedChannel> _channels =
        ImmutableArray<ManagedChannel>.Empty;

    public IEnumerable<ManagedChannel> Routable()
        => _channels.Where(c => c.CanRoute && c.State == ChannelState.Healthy );

    public void Reload(IEnumerable<ChannelOptions> options)
    {
        _channels = options
            .Select(opt => new ManagedChannel(ChannelFactory.Create(opt)))
            .ToImmutableArray();
    }

    public void StartHeartbeat(
        TimeSpan interval,
        TimeSpan cooldown,
        CancellationToken ct)
    {
        Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested)
            {
                foreach (var ch in _channels)
                {
                    if (ch.State == ChannelState.Down)
                    {
                        try
                        {
                            await ch.Transport.TestConnectionAsync();
                            ch.EnterCooldown(cooldown);
                            Console.WriteLine($"🟡 COOLDOWN {ch.Transport.Name}");
                        }
                        catch
                        {
                            // still dead
                        }
                    }
                }

                await Task.Delay(interval, ct);
            }
        }, ct);
    }
    public BackpressureSnapshot CreateSnapshot()
    {
        var channels = Routable().ToList();

        var inFlight = channels.Sum(c => c.Metrics.InFlight);
        var avgLatency = channels.Any()
            ? channels.Average(c => c.Metrics.AvgLatency)
            : 0;

        var total = channels.Sum(c => c.Metrics.Success + c.Metrics.Failure);
        var failureRate = total > 0
            ? channels.Sum(c => c.Metrics.Failure) / (double)total
            : 0;

        var pressure =
            Math.Min(1.0,
                (avgLatency / 500.0) * 0.4 +
                failureRate * 0.4 +
                Math.Min(1.0, inFlight / 50.0) * 0.2);

        return new BackpressureSnapshot(
            pressure,
            inFlight,
            avgLatency,
            failureRate);
    }

    public IEnumerable<ManagedChannel> All()
    {
        return _channels;
    }
}


