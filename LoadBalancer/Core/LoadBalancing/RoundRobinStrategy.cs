using LoadBalancer.Core.Channel;

namespace LoadBalancer.Core.LoadBalancing;

public sealed class RoundRobinStrategy : ILoadBalancingStrategy
{
    private readonly Func<IEnumerable<ManagedChannel>> _source;
    private int _index = -1;

    public RoundRobinStrategy(Func<IEnumerable<ManagedChannel>> source)
    {
        _source = source;
    }

    public ManagedChannel Select()
    {
        var list = _source().ToList();
        if (list.Count == 0)
            throw new Exception("No healthy channels");

        var next = Interlocked.Increment(ref _index);
        return list[next % list.Count];
    }
}
