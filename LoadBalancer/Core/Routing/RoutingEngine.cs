using LoadBalancer.Core.Channel;
using LoadBalancer.Core.Idempotency;
using LoadBalancer.Core.LoadBalancing;
using LoadBalancer.Core.Pool;
using LoadBalancer.Core.Protocol;

namespace LoadBalancer.Core.Routing;

public sealed class RoutingEngine
{
    private readonly ChannelPool _pool;
    private readonly ILoadBalancingStrategy _strategy;
    private readonly StickyRouter _sticky;
    private readonly IdempotencyStore _idempotency;
    public RoutingEngine(
        ChannelPool pool,
        ILoadBalancingStrategy strategy,
        StickyRouter sticky,
        IdempotencyStore idempotency)
    {
        _pool = pool;
        _strategy = strategy;
        _sticky = sticky;
        _idempotency = idempotency;
    }

    public async ValueTask<byte[]> RouteAsync(
        string terminalId,
        string traceNumber,
        byte[] payload,
        CancellationToken ct)
    {
        var key = new IdempotencyKey(terminalId, traceNumber);

        if (_idempotency.TryGet(key, out var cached))
            return cached;

        var candidates = _pool.Routable;
        if (candidates.Count == 0)
            throw new Exception("No routable channels");

        var channel = _sticky.Resolve(
            terminalId,
            candidates,
            _strategy.Select);

        var response = await channel.SendAsync(payload, ct);

        _idempotency.Store(key, response);
        return response;
    }

}



