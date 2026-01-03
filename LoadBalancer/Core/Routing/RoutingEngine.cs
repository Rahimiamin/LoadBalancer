using LoadBalancer.Core.LoadBalancing;
using LoadBalancer.Core.Pool;

namespace LoadBalancer.Core.Routing;

public sealed class RoutingEngine
{
    private readonly ChannelPool _pool;
    private readonly ILoadBalancingStrategy _strategy;

    public RoutingEngine(ChannelPool pool, ILoadBalancingStrategy strategy)
    {
        _pool = pool;
        _strategy = strategy;
    }

    public async Task<byte[]> RouteAsync(byte[] payload, CancellationToken ct)
    {
        var channels = _pool.Routable().ToList();
        if (channels.Count == 0)
            throw new Exception("No healthy channels");

        var channel = _strategy.Select();
        return await channel.SendAsync(payload, ct);
    }
}

