using LoadBalancer.Core.Channel;
using LoadBalancer.Core.Transport;
using System.Diagnostics;

namespace LoadBalancer.Infrastructure.Health;

public enum HealthStatus
{
    Healthy,
    Unresponsive
}

public sealed class HealthMonitor
{
    private readonly IReadOnlyList<TcpChannel> _channels;
    private readonly TimeSpan _timeout;
    private readonly TimeSpan _interval;

    public HealthMonitor(
        IReadOnlyList<TcpChannel> channels,
        TimeSpan timeout,
        TimeSpan interval)
    {
        _channels = channels;
        _timeout = timeout;
        _interval = interval;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var now = Stopwatch.GetTimestamp();

            foreach (var ch in _channels)
            {
                //if (ch.State == ChannelState.Down)
                //    continue;

                //if (!IsResponsive(ch, now))
                //    ch.MarkDown();
            }

            await Task.Delay(_interval, ct);
        }
    }

    //private bool IsResponsive(TcpChannel ch, long nowTicks)
    //{
    //    var last = Volatile.Read(ref ch.Metrics.LastSuccessTicks);
    //    if (last == 0)
    //        return false;

    //    var elapsedMs =
    //        (nowTicks - last) * 1000 / Stopwatch.Frequency;

    //    return elapsedMs <= _timeout.TotalMilliseconds;
    //}
}
