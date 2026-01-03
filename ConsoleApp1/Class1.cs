using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// -------------------- ENUMS --------------------
public enum ChannelState
{
    Healthy,
    Unhealthy,
    Cooldown,
    Down
}

// -------------------- METRICS --------------------
public class ChannelMetrics
{
    public long Success;
    public long Failure;
    public long InFlight;
    public long LastLatencyMs;
    public double AvgLatency => Success > 0 ? (double)LastLatencyMs / Success : 0;
}

// -------------------- TCP CHANNEL SIMULATION --------------------
public class TcpChannel
{
    public string Name { get; }
    private readonly IPEndPoint _endpoint;
    private bool _isUp = true;
    private readonly Random _rand = new();

    public TcpChannel(string name, IPEndPoint ep)
    {
        Name = name;
        _endpoint = ep;
    }

    public void TestConnection()
    {
        if (!_isUp) throw new Exception("Server DOWN");
    }

    public void SetDown() => _isUp = false;
    public void SetUp() => _isUp = true;

    public async Task<byte[]> SendAsync(byte[] payload, CancellationToken ct)
    {
        if (!_isUp)
            throw new Exception("Server forcibly closed connection");

        await Task.Delay(_rand.Next(50, 150), ct); // simulate latency
        return payload; // echo
    }
}

// -------------------- MANAGED CHANNEL --------------------
public sealed class ManagedChannel
{
    public TcpChannel Transport { get; }
    public ChannelState State { get; private set; } = ChannelState.Healthy;
    public ChannelMetrics Metrics { get; } = new();

    private int _consecutiveFailures;
    private DateTime _cooldownUntil;

    public ManagedChannel(TcpChannel transport)
    {
        Transport = transport;
    }

    public bool IsRoutable =>
        State == ChannelState.Healthy ||
        (State == ChannelState.Cooldown && DateTime.UtcNow >= _cooldownUntil);

    public async Task<byte[]> SendAsync(byte[] payload, CancellationToken ct)
    {
        if (!IsRoutable)
            throw new InvalidOperationException("Channel not routable");

        Interlocked.Increment(ref Metrics.InFlight);
        var sw = Stopwatch.StartNew();

        try
        {
            var res = await Transport.SendAsync(payload, ct);

            Metrics.Success++;
            _consecutiveFailures = 0;

            if (State == ChannelState.Cooldown)
                MarkHealthy();

            return res;
        }
        catch
        {
            Metrics.Failure++;
            _consecutiveFailures++;

            if (_consecutiveFailures >= 3)
                MarkDown();
            else
                MarkUnhealthy();

            throw;
        }
        finally
        {
            sw.Stop();
            Metrics.LastLatencyMs = sw.ElapsedMilliseconds;
            Interlocked.Decrement(ref Metrics.InFlight);
        }
    }

    public void MarkHealthy()
    {
        State = ChannelState.Healthy;
    }

    public void MarkUnhealthy()
    {
        State = ChannelState.Unhealthy;
    }

    public void MarkDown()
    {
        State = ChannelState.Down;
    }

    public void EnterCooldown(TimeSpan cooldown)
    {
        State = ChannelState.Cooldown;
        _cooldownUntil = DateTime.UtcNow.Add(cooldown);
        _consecutiveFailures = 0;
    }
}


// -------------------- CHANNEL POOL --------------------
public class ChannelPool
{
    private ImmutableArray<ManagedChannel> _channels = ImmutableArray<ManagedChannel>.Empty;

    public IReadOnlyList<ManagedChannel> Routable()
        => _channels.Where(c => c.State == ChannelState.Healthy).ToList();

    public void Reload(IEnumerable<ManagedChannel> channels)
        => _channels = channels.ToImmutableArray();

    public void StartHeartbeat(TimeSpan interval, CancellationToken ct)
    {
        Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested)
            {
                foreach (var ch in _channels)
                {
                    _ = Task.Run(() =>
                    {
                        try
                        {
                            ch.Transport.TestConnection();
                            ch.MarkHealthy();
                        }
                        catch
                        {
                            ch.MarkDown();
                            Console.WriteLine($"🔴 Channel {ch.Transport.Name} DOWN");
                        }
                    });
                }
                await Task.Delay(interval, ct);
            }
        }, ct);
    }
}

// -------------------- LEAST CONNECTION STRATEGY --------------------
public interface ILoadBalancingStrategy
{
    ManagedChannel Pick();
}

public class LeastConnectionsStrategy : ILoadBalancingStrategy
{
    private readonly ChannelPool _pool;
    public LeastConnectionsStrategy(ChannelPool pool) => _pool = pool;

    public ManagedChannel Pick()
    {
        var channels = _pool.Routable();
        if (!channels.Any())
            throw new Exception("No healthy channels available");

        return channels.OrderBy(c => c.Metrics.InFlight + Math.Max(c.Metrics.LastLatencyMs, 1)).First();
    }
}

// -------------------- ROUTING ENGINE --------------------
public class RoutingEngine
{
    private readonly ILoadBalancingStrategy _strategy;
    public RoutingEngine(ILoadBalancingStrategy strategy) => _strategy = strategy;

    public async Task<byte[]> RouteAsync(byte[] payload, CancellationToken ct)
    {
        var ch = _strategy.Pick();

        return await ch.SendAsync(payload, ct);
    }
}


public sealed class LoadBalancerOptions
{
    public string Algorithm { get; set; } = "RoundRobin";
    public int BackpressureCapacity { get; set; } = 10000;
}

public sealed class RoutingOptions
{
    public List<AcquirerOptions> Acquirers { get; set; } = new();
}
public class AcquirerOptions
{
    public string AcquirerId { get; set; }
    public List<ChannelOptions> Channels { get; set; } = new();
    public string Strategy { get; set; } = "RoundRobin"; // یا "LeastConnections"
}

public class ChannelOptions
{
    public string Name { get; set; }
    public string Ip { get; set; }
    public int Port { get; set; }
    public bool Enabled { get; set; } = true;
}

public class LoadBalancerSettings
{
    public int MaxConcurrent { get; set; } = 100;
    public int HeartbeatIntervalMs { get; set; } = 5000;
    public List<AcquirerOptions> Acquirers { get; set; } = new();
}

public sealed class AcquirerRuntime
{
    public string AcquirerId { get; }
    public ChannelPool Pool { get; }
    public ILoadBalancingStrategy Strategy { get; }

    public AcquirerRuntime(
        string acquirerId,
        ChannelPool pool,
        ILoadBalancingStrategy strategy)
    {
        AcquirerId = acquirerId;
        Pool = pool;
        Strategy = strategy;
    }
}




public sealed class BackpressureQueue
{
    private readonly SemaphoreSlim _semaphore;
    private readonly ConcurrentQueue<(byte[] payload, TaskCompletionSource<byte[]> tcs)> _queue
        = new();

    public BackpressureQueue(int maxConcurrent)
    {
        _semaphore = new SemaphoreSlim(maxConcurrent, maxConcurrent);
    }

    public async Task<byte[]> EnqueueAsync(byte[] payload, Func<byte[], Task<byte[]>> sendFunc)
    {
        var tcs = new TaskCompletionSource<byte[]>();
        _queue.Enqueue((payload, tcs));

        await _semaphore.WaitAsync();
        if (_queue.TryDequeue(out var item))
        {
            try
            {
                var result = await sendFunc(item.payload);
                item.tcs.SetResult(result);
                return result;
            }
            catch (Exception ex)
            {
                item.tcs.SetException(ex);
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        return await tcs.Task;
    }
}


//// -------------------- TEST MAIN --------------------
//public class Program
//{
//    public static async Task Main()
//    {

//    }
//}
