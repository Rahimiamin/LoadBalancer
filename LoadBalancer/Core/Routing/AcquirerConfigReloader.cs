using LoadBalancer.Core.LoadBalancing;
using LoadBalancer.Infrastructure.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LoadBalancer.Core.Routing;

public sealed class AcquirerConfigReloader
{
    public AcquirerConfigReloader(
        IOptionsMonitor<RoutingOptions> monitor,
        AcquirerRegistry registry,
        IServiceProvider sp)
    {
        monitor.OnChange(opt =>
        {
            registry.Reload(
                opt.Acquirers,
                acqId => ResolveStrategy(sp, acqId));
        });
    }

    private static ILoadBalancingStrategy ResolveStrategy(
        IServiceProvider sp,
        string acqId)
    {
        // الان ساده، بعداً per-ACQ config می‌گیریم
        return sp.GetRequiredService<ILoadBalancingStrategy>();
    }
}
