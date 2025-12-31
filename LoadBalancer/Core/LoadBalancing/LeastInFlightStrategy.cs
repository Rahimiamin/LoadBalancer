using LoadBalancer.Core.Channel;

namespace LoadBalancer.Core.LoadBalancing;

public sealed class LeastInFlightStrategy : ILoadBalancingStrategy
{
    private readonly Func<IEnumerable<ManagedChannel>> _source;

    public LeastInFlightStrategy(Func<IEnumerable<ManagedChannel>> source)
    {
        _source = source;
    }

    public ManagedChannel Select()
    {
        var list = _source().ToList();
        if (!list.Any())
            throw new Exception("No healthy channels");

        return list.OrderBy(c => c.Metrics.InFlight).First();
    }
}
