using LoadBalancer.Core.Channel;
using LoadBalancer.Core.Factory;
using LoadBalancer.Infrastructure.Config;
using System.Collections.Immutable;

namespace LoadBalancer.Core.Pool;

public sealed class ChannelPool
{
    private ImmutableArray<ManagedChannel> _channels
        = ImmutableArray<ManagedChannel>.Empty;

    public IReadOnlyList<ManagedChannel> Channels => _channels;

    public void Reload(IEnumerable<ChannelOptions> options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        _channels = options
            .Select(opt =>
                new ManagedChannel(
                    ChannelFactory.Create(opt)))
            .ToImmutableArray();
    }

    // 👇 چیزی که Routing لازم دارد
    public IEnumerable<ManagedChannel> Routable()
        => _channels.Where(c => c.State == ChannelState.Healthy);
}


