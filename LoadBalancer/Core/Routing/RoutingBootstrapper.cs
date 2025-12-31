using Microsoft.Extensions.Hosting;

namespace LoadBalancer.Core.Routing;

public sealed class RoutingBootstrapper : IHostedService
{
    private readonly RoutingConfigReloader _reloader;

    public RoutingBootstrapper(RoutingConfigReloader reloader)
    {
        _reloader = reloader;
    }

    public Task StartAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}