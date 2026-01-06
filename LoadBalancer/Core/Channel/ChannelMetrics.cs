using System.Collections.Concurrent;
using System.Diagnostics;

namespace LoadBalancer.Core.Channel;

public class ChannelMetrics
{
    private readonly ConcurrentQueue<long> _latencies = new();

    public int InFlight;
    public long Success;
    public long Failure;
    public long LastLatencyMs;

    public double AvgLatency;

    public void RecordLatency(long ms)
    {
        _latencies.Enqueue(ms);

        while (_latencies.Count > 200)
            _latencies.TryDequeue(out _);
    }

    public double P95Latency()
    {
        var arr = _latencies.ToArray();
        if (arr.Length == 0) return 0;

        Array.Sort(arr);
        var index = (int)(arr.Length * 0.95);
        return arr[Math.Min(index, arr.Length - 1)];
    }
}

