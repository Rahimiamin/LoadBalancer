using System.Diagnostics;

namespace LoadBalancer.Core.Channel;

public sealed class ChannelMetrics
{
    public long InFlight;
    public long Success;
    public long Failure;
    public long LastLatencyMs;

    private long _totalLatency;
    private long _count;

    public void RecordLatency(long ms)
    {
        Interlocked.Add(ref _totalLatency, ms);
        Interlocked.Increment(ref _count);
        LastLatencyMs = ms;
    }

    public double AvgLatency =>
        _count == 0 ? 0 : _totalLatency / (double)_count;
}
