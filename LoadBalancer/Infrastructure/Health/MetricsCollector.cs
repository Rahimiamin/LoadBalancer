using System.Collections.Concurrent;

namespace LoadBalancer.Infrastructure.Health;

public sealed class MetricsCollector
{
    private long _total;
    private long _failed;

    private readonly ConcurrentQueue<long> _latencies
        = new();

    private readonly int _windowSize;

    public MetricsCollector(int windowSize = 5000)
    {
        _windowSize = windowSize;
    }

    public void RecordSuccess(long latencyMs)
    {
        Interlocked.Increment(ref _total);
        _latencies.Enqueue(latencyMs);
        Trim();
    }

    public void RecordFailure()
    {
        Interlocked.Increment(ref _total);
        Interlocked.Increment(ref _failed);
    }

    private void Trim()
    {
        while (_latencies.Count > _windowSize)
            _latencies.TryDequeue(out _);
    }

    public MetricsSnapshot Snapshot(TimeSpan interval)
    {
        var arr = _latencies.ToArray();
        Array.Sort(arr);

        return new MetricsSnapshot
        {
            TotalRequests = Volatile.Read(ref _total),
            FailedRequests = Volatile.Read(ref _failed),
            Tps = arr.Length / interval.TotalSeconds,
            P95LatencyMs = Percentile(arr, 0.95),
            P99LatencyMs = Percentile(arr, 0.99)
        };
    }

    private static double Percentile(long[] data, double p)
    {
        if (data.Length == 0) return 0;
        var idx = (int)(p * data.Length);
        idx = Math.Min(idx, data.Length - 1);
        return data[idx];
    }
}
