using LoadBalancer.Core.Channel;
using LoadBalancer.Core.Transport;

namespace LoadBalancer.Core.LoadBalancing;

public interface ILoadBalancingStrategy
{
    ManagedChannel Select();
}
