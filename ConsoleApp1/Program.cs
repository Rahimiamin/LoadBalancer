using System.Diagnostics;
using System.Net;
using System.Text;

var cts = new CancellationTokenSource();

// Simulate servers
var servers = new List<TcpChannel>
        {
            new TcpChannel("CH1", new IPEndPoint(IPAddress.Loopback, 5000)),
            new TcpChannel("CH2", new IPEndPoint(IPAddress.Loopback, 5001))
        };

var managedChannels = servers.Select(s => new ManagedChannel(s)).ToList();
var pool = new ChannelPool();
pool.Reload(managedChannels);
pool.StartHeartbeat(TimeSpan.FromMilliseconds(500), cts.Token);

var strategy = new LeastConnectionsStrategy(pool);
var router = new RoutingEngine(strategy);

// Load test
int totalTx = 100;
var rand = new Random();
var sw = Stopwatch.StartNew();

var tasks = Enumerable.Range(0, totalTx).Select(i =>
    Task.Run(async () =>
    {
        var payload = Encoding.UTF8.GetBytes($"Tx-{i}");
        try
        {
            var res = await router.RouteAsync(payload, cts.Token);
            return $"Tx-{i} OK";
        }
        catch (Exception ex)
        {
            return $"Tx-{i} FAIL: {ex.Message}";
        }
    })
).ToArray();

// Simulate one server going down in middle
_ = Task.Run(async () =>
{
    await Task.Delay(50);
    servers[0].SetDown();
    servers[1].SetDown();
    Console.WriteLine($"⚠️ Server {servers[0].Name} forcibly closed!");
});

var results = await Task.WhenAll(tasks);
sw.Stop();

foreach (var r in results) Console.WriteLine(r);
Console.WriteLine($"\nProcessed {totalTx} transactions in {sw.ElapsedMilliseconds} ms");
Console.WriteLine($"TPS ~ {totalTx / (sw.ElapsedMilliseconds / 1000.0):F2}");

// Metrics
foreach (var ch in managedChannels)
{
    var m = ch.Metrics;
    Console.WriteLine($"Channel {ch.Transport.Name} | State: {ch.State} | Success: {m.Success} | Failure: {m.Failure} | LastLatencyMs: {m.LastLatencyMs}");
}