using LoadBalancer.Core.Pool;

namespace LoadBalancer.Core.LoadBalancing;

public sealed class AcquirerRuntime
{
    public string AcquirerId { get; }
    public ChannelPool Pool { get; }
    public ILoadBalancingStrategy Strategy { get; }

    public AcquirerRuntime(
        string acquirerId,
        ChannelPool pool,
        ILoadBalancingStrategy strategy)
    {
        AcquirerId = acquirerId;
        Pool = pool;
        Strategy = strategy;
    }
}
