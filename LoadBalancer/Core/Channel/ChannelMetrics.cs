using System.Diagnostics;

namespace LoadBalancer.Core.Channel;

public sealed class ChannelMetrics
{
    public long InFlight;
    public long Success;
    public long Failure;
    public long LastLatencyMs;
}
