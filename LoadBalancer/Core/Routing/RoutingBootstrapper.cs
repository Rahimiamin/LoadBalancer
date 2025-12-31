using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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