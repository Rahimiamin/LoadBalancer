using LoadBalancer.Core.Transport;
using System.Diagnostics;

namespace LoadBalancer.Core.Channel;

public sealed class ManagedChannel
{
    public TcpChannel Transport { get; }
    public ChannelState State { get; private set; } = ChannelState.Healthy;
    public ChannelMetrics Metrics { get; } = new();

    private double _healthScore = 100.0; // 0..100
    private const double Alpha = 0.2;

    public double HealthScore => _healthScore;

    public CircuitBreaker Circuit { get; }

    public double Weight { get; private set; } = 1.0;
    public void SetHalfOpenWeight() => Weight = 0.1;
    public void SetHealthyWeight() => Weight = 1.0;
    public void SetDownWeight() => Weight = 0.0;

    private int _consecutiveFailures;
    private DateTime _cooldownUntil;

    public bool CanRoute => Circuit.State != CircuitState.Open;

    public bool IsRoutable =>
        State == ChannelState.Healthy ||
        (State == ChannelState.Cooldown && DateTime.UtcNow >= _cooldownUntil);

    public double EffectiveScore =>
        (CanRoute && IsRoutable)
            ? Weight * (_healthScore / 100.0)
            : 0;

    public ManagedChannel(TcpChannel transport)
    {
        Transport = transport;

        Circuit = new CircuitBreaker(
            failureThreshold: 3,
            openDuration: TimeSpan.FromSeconds(10));
    }

    public async Task<byte[]> SendAsync(byte[] payload, CancellationToken ct)
    {
        if (!Circuit.AllowRequest())
            throw new InvalidOperationException("Circuit Open");

        Interlocked.Increment(ref Metrics.InFlight);
        var sw = Stopwatch.StartNew();

        try
        {
            var res = await Transport.SendAsync(payload, ct);

            sw.Stop();
            Metrics.LastLatencyMs = sw.ElapsedMilliseconds;
            Metrics.Success++;

            UpdateScore(success: true, sw.ElapsedMilliseconds);

            Circuit.OnSuccess();
            _consecutiveFailures = 0;

            return res;
        }
        catch
        {
            sw.Stop();
            Metrics.Failure++;

            UpdateScore(success: false, latencyMs: 1000);
            Circuit.OnFailure();

            _consecutiveFailures++;

            if (_consecutiveFailures >= 3)
                EnterCooldown(TimeSpan.FromSeconds(5));

            throw;
        }
        finally
        {
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

    public void MarkHealthy()
    {
        State = ChannelState.Healthy;
        SetHealthyWeight();
    }

    public void MarkUnhealthy()
    {
        State = ChannelState.Unhealthy;
        SetHalfOpenWeight();
    }

    public void EnterCooldown(TimeSpan cooldown)
    {
        State = ChannelState.Cooldown;
        _cooldownUntil = DateTime.UtcNow.Add(cooldown);
        SetHalfOpenWeight();
        _consecutiveFailures = 0;
    }
}



