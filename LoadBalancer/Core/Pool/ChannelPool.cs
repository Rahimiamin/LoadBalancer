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
        => _channels.Where(c => c.IsRoutable);

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
        var channels = _channels;

        if (channels.Length == 0)
            return new BackpressureSnapshot(1, 0, 0, 0);

        var inflight = channels.Sum(c => c.Metrics.InFlight);
        var latency = channels
            .Where(c => c.Metrics.LastLatencyMs > 0)
            .Select(c => c.Metrics.LastLatencyMs)
            .DefaultIfEmpty(0)
            .Average();

        var success = channels.Sum(c => c.Metrics.Success);
        var failure = channels.Sum(c => c.Metrics.Failure);

        var failureRate = success + failure == 0
            ? 0
            : (double)failure / (success + failure);

        var inflightRatio = inflight / (double)(channels.Length * 10);
        var latencyRatio = latency / 1000.0;

        var pressure = Math.Clamp(
            inflightRatio + latencyRatio + failureRate,
            0, 1);

        return new BackpressureSnapshot(
            pressure,
            (int)inflight,
            latency,
            failureRate);
    }
}


