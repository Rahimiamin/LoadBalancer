using LoadBalancer.Core.Channel;

namespace LoadBalancer.Core.LoadBalancing;

public sealed class LeastConnectionsStrategy : ILoadBalancingStrategy
{
    private readonly Func<IEnumerable<ManagedChannel>> _source;

    public LeastConnectionsStrategy(Func<IEnumerable<ManagedChannel>> source)
    {
        _source = source;
    }

    public ManagedChannel Select()
    {
        var channels = _source()
            .Where(c => c.IsRoutable)
            .OrderBy(c => c.Metrics.InFlight)
            .ToList();

        if (!channels.Any())
            throw new Exception("No routable channels");



        return channels[0];
    }
}



