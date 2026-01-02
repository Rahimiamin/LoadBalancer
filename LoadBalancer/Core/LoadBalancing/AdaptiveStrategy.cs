using LoadBalancer.Core.Channel;

namespace LoadBalancer.Core.LoadBalancing;

public sealed class AdaptiveStrategy : ILoadBalancingStrategy
{
    private readonly Func<IEnumerable<ManagedChannel>> _channels;

    public AdaptiveStrategy(Func<IEnumerable<ManagedChannel>> channels)
    {
        _channels = channels;
    }

    private static double Score(ManagedChannel c)
    {
        var latency = c.Metrics.AvgLatency > 0
            ? c.Metrics.AvgLatency
            : c.Metrics.LastLatencyMs;

        return latency * (c.Metrics.InFlight + 1);
    }

    public ManagedChannel Select()
    {
        var candidates = _channels()
            .Where(c => c.State == ChannelState.Healthy)
            .ToList();

        if (candidates.Count == 0)
            throw new Exception("No healthy channels available");

        return candidates
            .OrderBy(c => Score(c))
            .First();
    }
}
