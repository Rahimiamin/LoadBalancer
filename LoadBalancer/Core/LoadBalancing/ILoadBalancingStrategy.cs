using LoadBalancer.Core.Channel;
using LoadBalancer.Core.Pool;

namespace LoadBalancer.Core.LoadBalancing;

public interface ILoadBalancingStrategy
{
    TcpChannel Select(IReadOnlyList<TcpChannel> channels);
}

public sealed class LeastConnectionStrategy
    : ILoadBalancingStrategy
{
    public TcpChannel Select(IReadOnlyList<TcpChannel> channels)
    {
        return channels
            .OrderBy(c => c.Metrics.InFlight)
            .First();
    }
}

