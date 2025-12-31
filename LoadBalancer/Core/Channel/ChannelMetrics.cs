using System.Diagnostics;

namespace LoadBalancer.Core.Channel;

public sealed class ChannelMetrics
{
    public int InFlight;
    public long LastSuccessTicks;

    public void MarkSuccess()
    {
        LastSuccessTicks = Stopwatch.GetTimestamp();
    }

    public bool IsAlive(long timeoutMs)
    {
        if (LastSuccessTicks == 0) return false;

        var elapsedMs =
            (Stopwatch.GetTimestamp() - LastSuccessTicks) * 1000
            / Stopwatch.Frequency;

        return elapsedMs < timeoutMs;
    }
}

