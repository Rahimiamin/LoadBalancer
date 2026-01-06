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

    public async Task<byte[]> RouteAsync(
        byte[] payload,
        CancellationToken ct)
    {
        var snapshot = _pool.Routable().ToList();

        if (!snapshot.Any())
            throw new Exception("No healthy channels");

        var primary = _strategy.Select();

        try
        {
            return await primary.SendAsync(payload, ct);
        }
        catch
        {
            Console.WriteLine($"⚠️ Primary {primary.Transport.Name} failed");

            var fallback = snapshot
                .Where(c => c != primary && c.CanRoute)
                .OrderByDescending(c => c.HealthScore)
                .FirstOrDefault();

            if (fallback == null)
                throw;

            Console.WriteLine(
                $"🔁 Failover retry → {fallback.Transport.Name}");

            return await fallback.SendAsync(payload, ct);
        }
    }
}

