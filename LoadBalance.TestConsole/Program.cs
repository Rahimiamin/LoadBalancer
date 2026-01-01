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


// کمی صبر کن تا سرورها بالا بیایند
//await Task.Delay(500);


var runtimes = new Dictionary<string, (AcquirerRuntime runtime, BackpressureQueue queue)>();

var settings = configuration.GetSection("LoadBalancerSettings")
                                    .Get<LoadBalancerSettings>();

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

var ff = Task.Run(async () =>
{
    await Task.Delay(500);   // وسط Load
    server5000.Stop();        // 💥 Fail واقعی
});


// ---------------- Load Test ----------------
var totalTx = 100; // تعداد تراکنش برای تست
var rand = new Random();
var swTotal = Stopwatch.StartNew();

var tasks = Enumerable.Range(0, totalTx).Select(i =>
{
    int txNumber = i;
    var acqId = rand.Next(2) == 0 ? "ACQ1" : "ACQ2";
    var payload = Encoding.UTF8.GetBytes($"Tx-{txNumber}");
    var (runtime, queue) = runtimes[acqId];

    return queue.EnqueueAsync(payload, p => new RoutingEngine(runtime.Strategy).RouteAsync(p, cts.Token))
                .ContinueWith(t =>
                {
                    if (t.IsCompletedSuccessfully)
                        return $"{acqId} Tx-{txNumber} OK";
                    else
                        return $"{acqId} Tx-{txNumber} FAIL: {t.Exception?.GetBaseException().Message}";
                });
}).ToArray();


var results = await Task.WhenAll(tasks);
swTotal.Stop();

// ---------------- Print Results ----------------
foreach (var r in results) Console.WriteLine(r);

Console.WriteLine($"\nProcessed {totalTx} transactions in {swTotal.ElapsedMilliseconds} ms");
Console.WriteLine($"TPS ~ {totalTx / (swTotal.ElapsedMilliseconds / 1000.0):F2}");

// ---------------- Print Channel Metrics ----------------
Console.WriteLine("\n========== Channel Metrics ==========\n");

foreach (var acq in runtimes)
{
    var runtime = acq.Value.runtime;
    Console.WriteLine($"Acquirer: {acq.Key}");

    foreach (var ch in runtime.Pool.Routable())
    {
        var m = ch.Metrics;
        Console.WriteLine($"  Channel {ch.Transport.Name} | State: {ch.State} | Success: {m.Success} | Failure: {m.Failure} | LastLatencyMs: {m.LastLatencyMs}");
    }
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

    var elapsedSec = swTotal.ElapsedMilliseconds / 1000.0;
    double tps = elapsedSec == 0 ? 0 : success / elapsedSec;

    totalSuccess += success;
    totalFailure += failure;

    Console.WriteLine(
        $"ACQ: {acq.Key,-6} | " +
        $"Total: {success + failure,5} | " +
        $"OK: {success,5} | " +
        $"FAIL: {failure,3} | " +
        $"SuccessRate: {(success * 100.0 / Math.Max(1, success + failure)):F2}% | " +
        $"AvgLatency: {avgLatency,6:F1} ms | " +
        $"MaxLatency: {maxLatency,5} ms | " +
        $"TPS: {tps,6:F1}"
    );
}


Console.WriteLine("\n========== SYSTEM SUMMARY ==========\n");


var totalTps = totalSuccess / (swTotal.ElapsedMilliseconds / 1000.0);

Console.WriteLine($"Total TX       : {totalTx}");
Console.WriteLine($"Total Success  : {totalSuccess}");
Console.WriteLine($"Total Failure  : {totalFailure}");
Console.WriteLine($"System TPS     : {totalTps:F2}");
Console.WriteLine($"Elapsed Time   : {swTotal.ElapsedMilliseconds} ms");

Console.WriteLine("END!");
Console.ReadLine();
