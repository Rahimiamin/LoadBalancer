using LoadBalancer.Core.Backpressure;
using LoadBalancer.Core.Channel;
using LoadBalancer.Core.LoadBalancing;
using LoadBalancer.Core.Pool;
using LoadBalancer.Core.Routing;
using LoadBalancer.Infrastructure.Config;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Text;



var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory) // مسیر فایل appsettings.json
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var cts = new CancellationTokenSource();

// ایجاد سرور شبیه‌سازی برای همه کانال‌ها
var allChannels = new List<(string ip, int port)>
{
    ("127.0.0.1", 5000), ("127.0.0.1", 5001),
    ("127.0.0.1", 6000), ("127.0.0.1", 6001)
};

var server5000 = new TcpTestServer("127.0.0.1", 5000);
var server5001 = new TcpTestServer("127.0.0.1", 5001);

server5000.Start();
server5001.Start();

var server6000 = new TcpTestServer("127.0.0.1", 6000);
var server6001 = new TcpTestServer("127.0.0.1", 6001);

server6000.Start();
server6001.Start();


var runtimes = new Dictionary<string, (AcquirerRuntime runtime, AdaptiveBackpressureQueue queue)>();

var settings = configuration.GetSection("LoadBalancerSettings")
                                    .Get<LoadBalancerSettings>();
    var pool = new ChannelPool();
foreach (var acq in settings.Acquirers)
{

    pool.Reload(acq.Channels);
    pool.StartHeartbeat(TimeSpan.FromMilliseconds(settings.HeartbeatIntervalMs), TimeSpan.FromMilliseconds(settings.HeartbeatIntervalMs), cts.Token);

    ILoadBalancingStrategy strategy = acq.Strategy switch
    {
        "Adaptive" => new AdaptiveStrategy(pool.Routable),
        "LeastConnections" => new LeastConnectionsStrategy(pool.Routable),
        _ => new RoundRobinStrategy(pool.Routable)
    };

    var runtime = new AcquirerRuntime(acq.AcquirerId, pool, strategy);
    var queue = new AdaptiveBackpressureQueue(
       baseConcurrency: settings.MaxConcurrent,
       snapshot: runtime.Pool.CreateSnapshot);

    runtimes[acq.AcquirerId] = (runtime, queue);

    var router = new RoutingEngine(runtime.Pool, runtime.Strategy);

    int total = 200;
    _ = Task.Delay(200).ContinueWith(_ =>
    {
        Console.WriteLine("### SERVER 6001 DOWN ###");
        server6001.Stop();
    });
    var sw = Stopwatch.StartNew();

    var tasks = Enumerable.Range(0, total)
        .Select(i =>

            queue.EnqueueAsync(
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

}


Console.WriteLine("\n========== ACQUIRER SUMMARY ==========\n");

double totalSuccess = 0;
double totalFailure = 0;

foreach (var acq in runtimes)
{
    var runtime = acq.Value.runtime;
    var channels = runtime.Pool.Routable().ToList();

    long success = channels.Sum(c => c.Metrics.Success);
    long failure = channels.Sum(c => c.Metrics.Failure);

    var latencies = channels
        .Select(c => c.Metrics.AvgLatency)
        .Where(x => x > 0)
        .ToList();

    double avgLatency = latencies.Any() ? latencies.Average() : 0;
    double maxLatency = channels.Any()
        ? channels.Max(c => c.Metrics.LastLatencyMs)
        : 0;




    totalSuccess += success;
    totalFailure += failure;

    Console.WriteLine(
        $"ACQ: {acq.Key,-6} | " +
        $"Total: {success + failure,5} | " +
        $"OK: {success,5} | " +
        $"FAIL: {failure,3} | " +
        $"SuccessRate: {(success * 100.0 / Math.Max(1, success + failure)):F2}% | " +
        $"AvgLatency: {avgLatency,6:F1} ms | " +
        $"MaxLatency: {maxLatency,5} ms | " 
    );
}


Console.WriteLine("\n========== SYSTEM SUMMARY ==========\n");



Console.WriteLine("END!");
Console.ReadLine();
