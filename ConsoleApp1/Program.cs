//using System;
//using System.Collections.Concurrent;
//using System.Collections.Immutable;
//using System.Diagnostics;
//using System.Net;
//using System.Net.Sockets;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using System.Linq;
//using ConsoleApp1;

//// -------------------------- Main --------------------------
//var settings = new LoadBalancerSettings
//{
//    MaxConcurrent = 50,
//    HeartbeatIntervalMs = 2000,
//    Acquirers = new List<AcquirerOptions>
//    {
//        new() { AcquirerId="ACQ1", Strategy="LeastConnections", Channels=new List<ChannelOptions>
//            { new() {Name="CH1",Ip="127.0.0.1",Port=5000}, new(){Name="CH2",Ip="127.0.0.1",Port=5001} } },
//        new() { AcquirerId="ACQ2", Strategy="RoundRobin", Channels=new List<ChannelOptions>
//            { new() {Name="CH1",Ip="127.0.0.1",Port=6000}, new(){Name="CH2",Ip="127.0.0.1",Port=6001} } },
//    }
//};

//var cts = new CancellationTokenSource();

//// ---------------- TCP Servers ----------------
//var allChannels = new List<(string ip, int port)>
//{
//    ("127.0.0.1",5000),("127.0.0.1",5001),
//    ("127.0.0.1",6000),("127.0.0.1",6001)
//};

//foreach (var (ip, port) in allChannels) TcpTestServer.Start(ip, port, cts.Token);
//await Task.Delay(500); // صبر برای بالا آمدن سرورها

//// ---------------- LoadBalancer ----------------
//var runtimes = new Dictionary<string, (AcquirerRuntime runtime, BackpressureQueue queue)>();

//foreach (var acq in settings.Acquirers)
//{
//    var pool = new ChannelPool(); pool.Reload(acq.Channels);
//    pool.StartHeartbeat(TimeSpan.FromMilliseconds(settings.HeartbeatIntervalMs), cts.Token);

//    ILoadBalancingStrategy strategy = acq.Strategy switch
//    {
//        "LeastConnections" => new LeastConnectionsStrategy(pool.Routable),
//        _ => new RoundRobinStrategy(pool.Routable)
//    };

//    var runtime = new AcquirerRuntime(acq.AcquirerId, pool, strategy);
//    var queue = new BackpressureQueue(settings.MaxConcurrent);

//    runtimes[acq.AcquirerId] = (runtime, queue);
//}

//// ---------------- Load Test ----------------
//var totalTx = 4000; // تعداد تراکنش برای تست
//var rand = new Random();
//var swTotal = Stopwatch.StartNew();

//var tasks = Enumerable.Range(0, totalTx).Select(i =>
//{
//    int txNumber = i;
//    var acqId = rand.Next(2) == 0 ? "ACQ1" : "ACQ2";
//    var payload = Encoding.UTF8.GetBytes($"Tx-{txNumber}");
//    var (runtime, queue) = runtimes[acqId];

//    return queue.EnqueueAsync(payload, p => new RoutingEngine(runtime.Strategy).RouteAsync(p, cts.Token))
//                .ContinueWith(t =>
//                {
//                    if (t.IsCompletedSuccessfully)
//                        return $"{acqId} Tx-{txNumber} OK";
//                    else
//                        return $"{acqId} Tx-{txNumber} FAIL: {t.Exception?.GetBaseException().Message}";
//                });
//}).ToArray();

//var results = await Task.WhenAll(tasks);
//swTotal.Stop();

//// ---------------- Print Results ----------------
//foreach (var r in results) Console.WriteLine(r);

//Console.WriteLine($"\nProcessed {totalTx} transactions in {swTotal.ElapsedMilliseconds} ms");
//Console.WriteLine($"TPS ~ {totalTx / (swTotal.ElapsedMilliseconds / 1000.0):F2}");

//// ---------------- Print Channel Metrics ----------------
//Console.WriteLine("\n========== Channel Metrics ==========\n");

//foreach (var acq in runtimes)
//{
//    var runtime = acq.Value.runtime;
//    Console.WriteLine($"Acquirer: {acq.Key}");

//    foreach (var ch in runtime.Pool.Routable())
//    {
//        var m = ch.Metrics;
//        Console.WriteLine($"  Channel {ch.Transport.Name} | State: {ch.State} | Success: {m.Success} | Failure: {m.Failure} | LastLatencyMs: {m.LastLatencyMs}");
//    }
//}

Console.WriteLine("");
