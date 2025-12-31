using LoadBalancer.Core.Channel;

namespace LoadBalancer.Core.LoadBalancing;

public sealed class LeastConnectionsStrategy : ILoadBalancingStrategy
{
    private readonly Func<IEnumerable<ManagedChannel>> _source;

    public LeastConnectionsStrategy(Func<IEnumerable<ManagedChannel>> source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
    }

    public ManagedChannel Select()
    {
        // گرفتن snapshot کانال‌های سالم
        var channels = _source().ToList();

        if (!channels.Any())
            throw new Exception("No healthy channels available");

        // انتخاب کانال با کمترین تراکنش در حال اجرا
        return channels.OrderBy(c => c.Metrics.InFlight).First();
    }
}


