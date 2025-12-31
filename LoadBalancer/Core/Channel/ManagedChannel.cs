using LoadBalancer.Core.Transport;
using System.Diagnostics;

namespace LoadBalancer.Core.Channel;

public sealed class ManagedChannel
{
    public TcpChannel Transport { get; }
    public ChannelState State { get; private set; } = ChannelState.Healthy;
    public ChannelMetrics Metrics { get; } = new();

    public ManagedChannel(TcpChannel transport)
    {
        Transport = transport;
    }

    public async Task<byte[]> SendAsync(
        byte[] payload,
        CancellationToken ct)
    {
        if (State != ChannelState.Healthy)
            throw new Exception("Channel is not healthy");

        var sw = Stopwatch.StartNew();
        Interlocked.Increment(ref Metrics.InFlight);

        try
        {
            var res = await Transport.SendAsync(payload, ct);
            Metrics.Success++;
            return res;
        }
        catch
        {
            Metrics.Failure++;
            State = ChannelState.Unhealthy;
            throw;
        }
        finally
        {
            sw.Stop();
            Metrics.LastLatencyMs = sw.ElapsedMilliseconds;
            Interlocked.Decrement(ref Metrics.InFlight);
        }
    }

    public void MarkDown() => State = ChannelState.Down;
    public void MarkHealthy() => State = ChannelState.Healthy;
}
