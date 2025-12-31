using LoadBalancer.Core.Channel;
using LoadBalancer.Core.Transport;

namespace LoadBalancer.Core.LoadBalancing;

public sealed class LeastConnectionsStrategy : ILoadBalancingStrategy
{
    public TcpChannel Select(IReadOnlyList<TcpChannel> channels)
        => channels.OrderBy(c => c.ActiveConnections).First();
}


