using LoadBalancer.Core.Transport;

namespace LoadBalancer.Core.LoadBalancing;

public interface ILoadBalancingStrategy
{
    TcpChannel Select(IReadOnlyList<TcpChannel> channels);
}


