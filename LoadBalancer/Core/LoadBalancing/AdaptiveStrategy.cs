using LoadBalancer.Core.Channel;

namespace LoadBalancer.Core.LoadBalancing;

public class AdaptiveStrategy : ILoadBalancingStrategy
{
    private readonly Func<IEnumerable<ManagedChannel>> _channels;

    public AdaptiveStrategy(Func<IEnumerable<ManagedChannel>> channels)
    {
        _channels = channels;
    }

    public ManagedChannel Select()
    {
        return _channels()
            .Where(c => c.Score > 0)
            .OrderByDescending(c => c.Score)
            .FirstOrDefault();
    }
}


