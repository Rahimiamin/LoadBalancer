using LoadBalancer.Core.Factory;
using LoadBalancer.Core.Pool;
using LoadBalancer.Infrastructure.Config;
using System.Collections.Immutable;

namespace LoadBalancer.Core.LoadBalancing;

public sealed class AcquirerRegistry
{
    private volatile ImmutableDictionary<string, AcquirerRuntime> _map
        = ImmutableDictionary<string, AcquirerRuntime>.Empty;

    public AcquirerRuntime Resolve(string acquirerId)
    {
        if (_map.TryGetValue(acquirerId, out var runtime))
            return runtime;

        throw new Exception($"ACQ '{acquirerId}' not found");
    }

    public void Reload(
    IEnumerable<AcquirerOptions> acquirers,
    Func<string, ILoadBalancingStrategy> strategyFactory)
    {
        var builder = ImmutableDictionary.CreateBuilder<string, AcquirerRuntime>();

        foreach (var acq in acquirers)
        {
            var pool = new ChannelPool();
            pool.Reload(acq.Channels); // 👈 فقط ChannelOptions پاس داده می‌شود

            builder[acq.AcquirerId] = new AcquirerRuntime(
                acq.AcquirerId,
                pool,
                strategyFactory(acq.AcquirerId));
        }

        _map = builder.ToImmutable();
    }

}

