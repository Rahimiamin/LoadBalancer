using LoadBalancer.Core.Channel;

namespace LoadBalancer.Core.LoadBalancing;

public sealed class AdaptiveStrategy : ILoadBalancingStrategy
{
    private readonly Func<IEnumerable<ManagedChannel>> _channels;

    public AdaptiveStrategy(Func<IEnumerable<ManagedChannel>> channels)
    {
        _channels = channels;
    }

    public ManagedChannel Select()
    {
        var candidates = _channels()
            .Where(c => c.IsRoutable)
            .OrderByDescending(c => c.EffectiveScore)
            .ToList();

        if (!candidates.Any())
            throw new Exception("No healthy channels available");

        return candidates[0];
    }
}

