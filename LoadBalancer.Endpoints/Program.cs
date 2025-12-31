using LoadBalancer.Core.Channel;
using LoadBalancer.Core.LoadBalancing;
using LoadBalancer.Core.Pool;
using LoadBalancer.Core.Routing;
using LoadBalancer.Infrastructure.Config;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var settings = builder.Configuration.GetSection("LoadBalancerSettings")
                                    .Get<LoadBalancerSettings>();

var cts = new CancellationTokenSource();
var runtimes = new Dictionary<string, (AcquirerRuntime runtime, BackpressureQueue queue)>();

foreach (var acq in settings.Acquirers)
{
    var pool = new ChannelPool();
    pool.Reload(acq.Channels);
    pool.StartHeartbeat(TimeSpan.FromMilliseconds(settings.HeartbeatIntervalMs), cts.Token);

    ILoadBalancingStrategy strategy = acq.Strategy switch
    {
        "LeastConnections" => new LeastConnectionsStrategy(pool.Routable),
        _ => new RoundRobinStrategy(pool.Routable)
    };

    var runtime = new AcquirerRuntime(acq.AcquirerId, pool, strategy);
    var queue = new BackpressureQueue(settings.MaxConcurrent);

    runtimes[acq.AcquirerId] = (runtime, queue);
}

// نمونه ارسال تراکنش
var payload = Encoding.UTF8.GetBytes("Hello ACQ1");
var (runtime1, queue1) = runtimes["ACQ1"];
var result = await queue1.EnqueueAsync(payload, p => new RoutingEngine(runtime1.Strategy).RouteAsync(p, cts.Token));

Console.WriteLine("Response: " + Encoding.UTF8.GetString(result));
