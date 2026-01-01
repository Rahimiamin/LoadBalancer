//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Collections.Immutable;
//using System.Diagnostics;
//using System.Linq;
//using System.Net;
//using System.Net.Sockets;
//using System.Text;
//using System.Threading.Tasks;

//namespace ConsoleApp1;


//// -------------------------- Config Models --------------------------
//public class ChannelOptions { public string Name; public string Ip; public int Port; }
//public class AcquirerOptions { public string AcquirerId; public string Strategy; public List<ChannelOptions> Channels = new(); }
//public class LoadBalancerSettings
//{
//    public int MaxConcurrent = 50;
//    public int HeartbeatIntervalMs = 2000;
//    public List<AcquirerOptions> Acquirers = new();
//}

//// -------------------------- TCP / ManagedChannel --------------------------
//public sealed class TcpChannel
//{
//    private readonly IPEndPoint _endpoint;
//    public string Name { get; }
//    public TcpChannel(string name, IPEndPoint ep) { Name = name; _endpoint = ep; }
//    public async Task<byte[]> SendAsync(byte[] payload, CancellationToken ct)
//    {
//        using var client = new TcpClient();
//        await client.ConnectAsync(_endpoint.Address, _endpoint.Port, ct);
//        using var stream = client.GetStream();
//        await stream.WriteAsync(payload, 0, payload.Length, ct);
//        var buffer = new byte[payload.Length];
//        int read = 0;
//        while (read < buffer.Length) read += await stream.ReadAsync(buffer, read, buffer.Length - read, ct);
//        return buffer;
//    }
//    public void TestConnection() { /* همیشه موفق */ }
//}

//public enum ChannelState { Healthy, Unhealthy, Down }
//public sealed class ChannelMetrics { public long InFlight, Success, Failure, LastLatencyMs; }

//public sealed class ManagedChannel
//{
//    public TcpChannel Transport { get; }
//    public ChannelState State { get; private set; } = ChannelState.Healthy;
//    public ChannelMetrics Metrics { get; } = new();
//    public ManagedChannel(TcpChannel t) => Transport = t;

//    public async Task<byte[]> SendAsync(byte[] payload, CancellationToken ct)
//    {
//        if (State != ChannelState.Healthy) throw new Exception("Channel not healthy");
//        Interlocked.Increment(ref Metrics.InFlight);
//        var sw = Stopwatch.StartNew();
//        try
//        {
//            var res = await Transport.SendAsync(payload, ct);
//            Interlocked.Increment(ref Metrics.Success);
//            return res;
//        }
//        catch
//        {
//            Interlocked.Increment(ref Metrics.Failure);
//            MarkUnhealthy();
//            throw;
//        }
//        finally
//        {
//            sw.Stop();
//            Metrics.LastLatencyMs = sw.ElapsedMilliseconds;
//            Interlocked.Decrement(ref Metrics.InFlight);
//        }
//    }

//    public void MarkHealthy() => State = ChannelState.Healthy;
//    public void MarkUnhealthy() => State = ChannelState.Unhealthy;
//    public void MarkDown() => State = ChannelState.Down;
//}

//// -------------------------- ChannelFactory --------------------------
//public static class ChannelFactory { public static TcpChannel Create(ChannelOptions opt) => new(opt.Name, new IPEndPoint(IPAddress.Parse(opt.Ip), opt.Port)); }

//// -------------------------- ChannelPool --------------------------
//public sealed class ChannelPool
//{
//    private ImmutableArray<ManagedChannel> _channels = ImmutableArray<ManagedChannel>.Empty;
//    public IEnumerable<ManagedChannel> Routable() => _channels.Where(c => c.State == ChannelState.Healthy);

//    public void Reload(IEnumerable<ChannelOptions> options) => _channels = options.Select(opt => new ManagedChannel(ChannelFactory.Create(opt))).ToImmutableArray();

//    public void StartHeartbeat(TimeSpan interval, CancellationToken ct)
//    {
//        Task.Run(async () =>
//        {
//            while (!ct.IsCancellationRequested)
//            {
//                foreach (var ch in _channels)
//                {
//                    if (ch.State != ChannelState.Healthy)
//                    {
//                        try { ch.Transport.TestConnection(); ch.MarkHealthy(); }
//                        catch { ch.MarkDown(); }
//                    }
//                }
//                await Task.Delay(interval, ct);
//            }
//        }, ct);
//    }
//}

//// -------------------------- Strategies --------------------------
//public interface ILoadBalancingStrategy { ManagedChannel Select(); }

//public sealed class RoundRobinStrategy : ILoadBalancingStrategy
//{
//    private readonly Func<IEnumerable<ManagedChannel>> _source; private int _index = -1;
//    public RoundRobinStrategy(Func<IEnumerable<ManagedChannel>> s) => _source = s;
//    public ManagedChannel Select()
//    {
//        var list = _source().ToList();
//        if (!list.Any()) throw new Exception("No healthy channels");
//        var next = Interlocked.Increment(ref _index);
//        return list[next % list.Count];
//    }
//}

//public sealed class LeastConnectionsStrategy : ILoadBalancingStrategy
//{
//    private readonly Func<IEnumerable<ManagedChannel>> _source;
//    public LeastConnectionsStrategy(Func<IEnumerable<ManagedChannel>> s) => _source = s;
//    public ManagedChannel Select()
//    {
//        var list = _source().ToList();
//        if (!list.Any()) throw new Exception("No healthy channels");
//        return list.OrderBy(c => c.Metrics.InFlight).First();
//    }
//}

//// -------------------------- RoutingEngine --------------------------
//public sealed class RoutingEngine
//{
//    private readonly ILoadBalancingStrategy _strategy;
//    public RoutingEngine(ILoadBalancingStrategy s) => _strategy = s;
//    public async Task<byte[]> RouteAsync(byte[] payload, CancellationToken ct) => await _strategy.Select().SendAsync(payload, ct);
//}

//// -------------------------- BackpressureQueue --------------------------
//public sealed class BackpressureQueue
//{
//    private readonly SemaphoreSlim _sem; private readonly ConcurrentQueue<(byte[] payload, TaskCompletionSource<byte[]> tcs)> _queue = new();
//    public BackpressureQueue(int maxConcurrent) => _sem = new SemaphoreSlim(maxConcurrent, maxConcurrent);

//    public async Task<byte[]> EnqueueAsync(byte[] payload, Func<byte[], Task<byte[]>> sendFunc)
//    {
//        var tcs = new TaskCompletionSource<byte[]>();
//        _queue.Enqueue((payload, tcs));
//        await _sem.WaitAsync();

//        if (_queue.TryDequeue(out var item))
//        {
//            try { var result = await sendFunc(item.payload); item.tcs.SetResult(result); return result; }
//            catch (Exception ex) { item.tcs.SetException(ex); throw; }
//            finally { _sem.Release(); }
//        }
//        return await tcs.Task;
//    }
//}

//// -------------------------- AcquirerRuntime --------------------------
//public sealed class AcquirerRuntime
//{
//    public string Id; public ChannelPool Pool; public ILoadBalancingStrategy Strategy;
//    public AcquirerRuntime(string id, ChannelPool pool, ILoadBalancingStrategy s) { Id = id; Pool = pool; Strategy = s; }
//}

//// -------------------------- TCP Test Server --------------------------
//public static class TcpTestServer
//{
//    public static void Start(string ip, int port, CancellationToken ct)
//    {
//        Task.Run(async () =>
//        {
//            var listener = new TcpListener(IPAddress.Parse(ip), port);
//            listener.Start();
//            Console.WriteLine($"Test TCP Server started on {ip}:{port}");

//            while (!ct.IsCancellationRequested)
//            {
//                if (!listener.Pending()) { await Task.Delay(50, ct); continue; }
//                var client = await listener.AcceptTcpClientAsync(ct);
//                _ = HandleClient(client, ct);
//            }
//            listener.Stop();
//        }, ct);
//    }

//    private static async Task HandleClient(TcpClient client, CancellationToken ct)
//    {
//        using (client)
//        using (var stream = client.GetStream())
//        {
//            var buffer = new byte[1024];
//            while (!ct.IsCancellationRequested && client.Connected)
//            {
//                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, ct);
//                if (bytesRead == 0) break;
//                await stream.WriteAsync(buffer, 0, bytesRead, ct); // echo
//            }
//        }
//    }
//}
