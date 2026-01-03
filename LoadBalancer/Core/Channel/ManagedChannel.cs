using LoadBalancer.Core.Transport;
using System.Diagnostics;

namespace LoadBalancer.Core.Channel;

public sealed class ManagedChannel
{
    public TcpChannel Transport { get; }
    public ChannelState State { get; private set; } = ChannelState.Healthy;
    public ChannelMetrics Metrics { get; } = new();

    private double _healthScore = 100.0; // 0..100
    private const double Alpha = 0.2;    // حافظه سیستم

    public double HealthScore => _healthScore;


    public async Task<byte[]> SendAsync(byte[] payload, CancellationToken ct)
    {
        Interlocked.Increment(ref Metrics.InFlight);
        var sw = Stopwatch.StartNew();

        try
        {
            var res = await Transport.SendAsync(payload, ct);

            Metrics.Success++;
            UpdateScore(success: true, sw.ElapsedMilliseconds);
            MarkHealthy();

            return res;
        }
        catch
        {
            Metrics.Failure++;
            UpdateScore(success: false, sw.ElapsedMilliseconds);
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

    private void UpdateScore(bool success, long latencyMs)
    {
        var penalty =
            (!success ? 30 : 0) +
            Math.Min(latencyMs / 10.0, 20) +
            Metrics.InFlight * 2;

        var target = Math.Max(0, 100 - penalty);

        _healthScore = _healthScore * (1 - Alpha) + target * Alpha;
    }

    public void MarkHealthy() => State = ChannelState.Healthy;
    public void MarkUnhealthy() => State = ChannelState.Unhealthy;

    public ManagedChannel(TcpChannel transport)
    {
        Transport = transport;
    }
    public void EnterCooldown(TimeSpan cooldown)
    {
        State = ChannelState.Cooldown;
        _cooldownUntil = DateTime.UtcNow.Add(cooldown);
        _consecutiveFailures = 0;
    }
    private int _consecutiveFailures;
    private DateTime _cooldownUntil;



    public bool IsRoutable =>
        State == ChannelState.Healthy ||
        (State == ChannelState.Cooldown && DateTime.UtcNow >= _cooldownUntil);
}


