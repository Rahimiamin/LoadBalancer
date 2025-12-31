using LoadBalancer.Core.LoadBalancing;

namespace LoadBalancer.Core.Routing;

public sealed class RoutingEngine
{
    private readonly ILoadBalancingStrategy _strategy;

    public RoutingEngine(ILoadBalancingStrategy strategy)
    {
        _strategy = strategy;
    }

    public async Task<byte[]> RouteAsync(byte[] payload, CancellationToken ct)
    {
        var channel = _strategy.Select();
        return await channel.SendAsync(payload, ct);
    }
}

