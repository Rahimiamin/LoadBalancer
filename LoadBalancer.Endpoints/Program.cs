using LoadBalancer.Core.Backpressure;
using LoadBalancer.Core.Channel;
using LoadBalancer.Core.LoadBalancing;
using LoadBalancer.Core.Pool;
using LoadBalancer.Core.Routing;
using LoadBalancer.Infrastructure.Config;
using System.Diagnostics;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var settings = builder.Configuration.GetSection("LoadBalancerSettings")
                                    .Get<LoadBalancerSettings>();

var cts = new CancellationTokenSource();
var runtimes = new Dictionary<string, (AcquirerRuntime runtime, AdaptiveBackpressureQueue queue)>();
var pool = new ChannelPool();
foreach (var acq in settings.Acquirers)
{

    pool.Reload(acq.Channels);
    pool.StartHeartbeat(TimeSpan.FromMilliseconds(settings.HeartbeatIntervalMs), TimeSpan.FromMilliseconds(settings.HeartbeatIntervalMs), cts.Token);

    ILoadBalancingStrategy strategy = acq.Strategy switch
    {
        "LeastConnections" => new LeastConnectionsStrategy(pool.Routable),
        _ => new RoundRobinStrategy(pool.Routable)
    };

    var runtime = new AcquirerRuntime(acq.AcquirerId, pool, strategy);
    //var queue = new BackpressureQueue(settings.MaxConcurrent);

    var queue = new AdaptiveBackpressureQueue(
    baseConcurrency: settings.MaxConcurrent,
    snapshot: runtime.Pool.CreateSnapshot);

    runtimes[acq.AcquirerId] = (runtime, queue);
}

// نمونه ارسال تراکنش
var payload = Encoding.UTF8.GetBytes("Hello ACQ1");
var (runtime1, queue1) = runtimes["ACQ1"];
var result = await queue1.EnqueueAsync(p => new RoutingEngine(pool, runtime1.Strategy).RouteAsync(payload, cts.Token), cts.Token);

var router = new RoutingEngine(runtime1.Pool, runtime1.Strategy);

int total = 200;
var sw = Stopwatch.StartNew();

var tasks = Enumerable.Range(0, total)
    .Select(i =>
        queue1.EnqueueAsync(
            ct => router.RouteAsync(
                Encoding.UTF8.GetBytes($"TX-{i}"), ct),
            cts.Token)
        .ContinueWith(t =>
        {
            if (t.IsCompletedSuccessfully)
                Console.WriteLine($"TX-{i} OK");
            else
                Console.WriteLine($"TX-{i} FAIL: {t.Exception?.GetBaseException().Message}");
        })
    ).ToArray();

await Task.WhenAll(tasks);
sw.Stop();

Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds} ms");
Console.WriteLine($"TPS ≈ {total / (sw.ElapsedMilliseconds / 1000.0):F2}");

