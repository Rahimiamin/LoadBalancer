using LoadBalancer.Core.Pool;
using LoadBalancer.Core.Transport;
using System.Threading.Channels;

namespace LoadBalancer.Core.Routing;

public sealed class RoutingEngine
{
    private readonly ChannelPool _pool;

    public RoutingEngine(ChannelPool pool)
    {
        _pool = pool;
    }

    public async Task<byte[]> RouteAsync(
        byte[] payload,
        CancellationToken ct)
    {
        var routable = _pool.Routable();
        using var e = routable.GetEnumerator();

        if (!e.MoveNext())
            throw new Exception("No healthy channels");

        var channel = e.Current;


        return await channel.SendAsync(payload, ct);
    }
}
