using LoadBalancer.Core.Channel;
using LoadBalancer.Core.Pool;
using LoadBalancer.Infrastructure.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoadBalancer.Core.LoadBalancing;

public sealed class LoadBalancerService
{
    public Dictionary<string, AcquirerRuntime> Runtimes { get; } = new();
    public Dictionary<string, BackpressureQueue> Queues { get; } = new();

    public LoadBalancerService(LoadBalancerSettings settings, CancellationToken ct)
    {
        foreach (var acq in settings.Acquirers)
        {
            var pool = new ChannelPool();
            pool.Reload(acq.Channels);
            pool.StartHeartbeat(TimeSpan.FromMilliseconds(settings.HeartbeatIntervalMs), ct);

            ILoadBalancingStrategy strategy = acq.Strategy switch
            {
                "LeastConnections" => new LeastConnectionsStrategy(pool.Routable),
                _ => new RoundRobinStrategy(pool.Routable)
            };

            var runtime = new AcquirerRuntime(acq.AcquirerId, pool, strategy);
            var queue = new BackpressureQueue(settings.MaxConcurrent);

            Runtimes[acq.AcquirerId] = runtime;
            Queues[acq.AcquirerId] = queue;
        }
    }
}
