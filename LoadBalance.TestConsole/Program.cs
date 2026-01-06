using LoadBalancer.Core.Backpressure;
using LoadBalancer.Core.Channel;
using LoadBalancer.Core.LoadBalancing;
using LoadBalancer.Core.Pool;
using LoadBalancer.Core.Routing;
using LoadBalancer.Infrastructure.Config;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Text;
using static System.Collections.Specialized.BitVector32;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var settings = configuration
    .GetSection("LoadBalancerSettings")
    .Get<Loadbalancersettings>()!;


var section = configuration.GetSection("LoadBalancerSettings");
foreach (var child in section.GetChildren())
{
    Console.WriteLine(child.Key);
}

var cts = new CancellationTokenSource();

/* ================= TCP TEST SERVERS ================= */

var server5000 = new TcpTestServer("127.0.0.1", 5000);
var server5001 = new TcpTestServer("127.0.0.1", 5001);
var server6000 = new TcpTestServer("127.0.0.1", 6000);
var server6001 = new TcpTestServer("127.0.0.1", 6001);

server5000.Start();
server5001.Start();
server6000.Start();
server6001.Start();

/* ================= RUNTIMES ================= */

var runtimes = new Dictionary<string, (AcquirerRuntime runtime, AdaptiveBackpressureQueue queue)>();

foreach (var acq in settings.Acquirers)
{
    var pool = new ChannelPool();
    pool.Reload(acq.Channels);
    pool.StartHeartbeat(
        TimeSpan.FromMilliseconds(settings.HeartbeatIntervalMs),
        TimeSpan.FromMilliseconds(settings.HeartbeatIntervalMs),
        cts.Token);

    ILoadBalancingStrategy strategy = acq.Strategy switch
    {
        "LeastConnections" => new LeastConnectionsStrategy(pool.Routable),
        "RoundRobin" => new RoundRobinStrategy(pool.Routable),
        _ => new AdaptiveStrategy(pool.Routable)
    };

    var runtime = new AcquirerRuntime(acq.AcquirerId, pool, strategy);

    var queue = new AdaptiveBackpressureQueue(
        baseConcurrency: settings.MaxConcurrent,
        snapshot: runtime.Pool.CreateSnapshot);

    runtimes[acq.AcquirerId] = (runtime, queue);
}

/* ================= METRICS MONITOR ================= */

var monitorTask = Task.Run(async () =>
{
    while (!cts.Token.IsCancellationRequested)
    {
        Console.WriteLine("\n--- METRICS SNAPSHOT ---");

        foreach (var acq in runtimes)
        {
            foreach (var ch in acq.Value.runtime.Pool.All())
            {
                var m = ch.Metrics;
                Console.WriteLine(
                    $"[{acq.Key}] {ch.Transport.Name,-10} | " +
                    $"State={ch.Circuit.State,-8} | " +
                    $"InFlight={m.InFlight,3} | " +
                    $"OK={m.Success,4} | FAIL={m.Failure,3} | " +
                    $"LastLat={m.LastLatencyMs,4}ms");
            }
        }

        await Task.Delay(200, cts.Token);
    }
});

/* ================= LOAD TEST ================= */

var (runtime1, queue1) = runtimes["ACQ1"];
var router = new RoutingEngine(runtime1.Pool, runtime1.Strategy);

int total = 300;
var sw = Stopwatch.StartNew();

/* سرور 5000 وسط لود DOWN شود */
_ = Task.Delay(500).ContinueWith(_ =>
{
    Console.WriteLine("\n🔥 SERVER 5000 DOWN 🔥\n");
    server5000.Stop();
});

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
        }))
    .ToArray();

await Task.WhenAll(tasks);
sw.Stop();

await Task.Delay(500);
cts.Cancel();

Console.WriteLine($"\nElapsed: {sw.ElapsedMilliseconds} ms");
Console.WriteLine($"TPS ≈ {total / (sw.ElapsedMilliseconds / 1000.0):F2}");

Console.WriteLine("\nEND");
Console.ReadLine();
