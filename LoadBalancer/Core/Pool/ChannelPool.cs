using LoadBalancer.Core.Channel;

namespace LoadBalancer.Core.Pool;

public sealed class ChannelPool
{
    private readonly List<TcpChannel> _channels;

    public ChannelPool(IEnumerable<TcpChannel> channels)
    {
        _channels = channels.ToList();
    }

    // فقط کانال‌هایی که واقعاً قابل ارسال‌اند
    public IReadOnlyList<TcpChannel> Routable =>
        _channels
            .Where(c => c.State == ChannelState.Healthy)
            .ToList();
}
