using System.Collections.Concurrent;

namespace LoadBalancer.Core.Channel;

public sealed class StickyRouter
{
    private readonly ConcurrentDictionary<string, TcpChannel> _map
        = new();

    public TcpChannel Resolve(
        string terminalId,
        IReadOnlyList<TcpChannel> candidates,
        Func<IReadOnlyList<TcpChannel>, TcpChannel> selector)
    {
        if (_map.TryGetValue(terminalId, out var channel))
        {
            if (channel.State == ChannelState.Healthy)
                return channel;

            _map.TryRemove(terminalId, out _);
        }

        var selected = selector(candidates);
        _map[terminalId] = selected;
        return selected;
    }
}
