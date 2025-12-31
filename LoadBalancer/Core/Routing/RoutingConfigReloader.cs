using LoadBalancer.Core.Factory;
using LoadBalancer.Core.Pool;
using LoadBalancer.Infrastructure.Config;
using Microsoft.Extensions.Options;

public sealed class RoutingConfigReloader
{
    public RoutingConfigReloader(
        IOptionsMonitor<RoutingOptions> monitor,
        ChannelPool pool)
    {
        monitor.OnChange(opt =>
        {
            var tcpChannels = opt.Acquirers
                .SelectMany(a => a.Channels)
                .Where(c => c.Enabled)
                .Select(ChannelFactory.Create);

        });
    }
}
