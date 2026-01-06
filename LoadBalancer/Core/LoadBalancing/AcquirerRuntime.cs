using LoadBalancer.Core.Pool;
using LoadBalancer.Core.RateLimiter;

namespace LoadBalancer.Core.LoadBalancing;

public sealed class AcquirerRuntime
{
    public AcquirerRateLimiter Limiter { get; }
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
