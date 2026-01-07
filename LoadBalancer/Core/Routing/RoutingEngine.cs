using LoadBalancer.Core.LoadBalancing;
using LoadBalancer.Core.Pool;
using LoadBalancer.Core.Retry;

namespace LoadBalancer.Core.Routing;

public sealed class RoutingEngine
{
    private readonly ChannelPool _pool;
    private readonly ILoadBalancingStrategy _strategy;
    private readonly RetryBudget _retryBudget;

    public RoutingEngine(
        ChannelPool pool,
        ILoadBalancingStrategy strategy,
        RetryBudget retryBudget)
    {
        _pool = pool;
        _strategy = strategy;
        _retryBudget = retryBudget;
    }

    public async Task<byte[]> RouteAsync(
        byte[] payload,
        CancellationToken ct)
    {
        var channels = _pool.Routable().ToList();
        if (!channels.Any())
            throw new Exception("No healthy channels");

        var primary = _strategy.Select();

        try
        {
            return await primary.SendAsync(payload, ct);
        }
        catch
        {
            if (!_retryBudget.TryConsume())
            {
                Console.WriteLine("⛔ Retry budget exhausted");
                throw;
            }

            var fallback = channels
                .Where(c => c != primary && c.CanRoute)
                .OrderByDescending(c => c.HealthScore)
                .FirstOrDefault();

            if (fallback == null)
                throw;

            Console.WriteLine(
                $"🔁 Retry → {fallback.Transport.Name} | " +
                $"Budget: {_retryBudget.Used}/{_retryBudget.Max}");

            return await fallback.SendAsync(payload, ct);
        }
    }
}

