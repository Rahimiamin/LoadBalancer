using LoadBalancer.Core.Transport;
using System.Diagnostics;

namespace LoadBalancer.Core.Channel;

public sealed class ManagedChannel
{
    public TcpChannel Transport { get; }
    public ChannelState State { get; private set; } = ChannelState.Healthy;
    public CircuitState CircuitState { get; private set; } = CircuitState.Closed;

    public ChannelMetrics Metrics { get; } = new();

    private const int FailureThreshold = 3;                 // چند خطا → Open
    private static readonly TimeSpan OpenTimeout = TimeSpan.FromSeconds(5);

    private DateTime _lastFailureUtc;
    private int _consecutiveFailures;

    public ManagedChannel(TcpChannel transport)
    {
        Transport = transport;
    }

    public async Task<byte[]> SendAsync(byte[] payload, CancellationToken ct)
    {
        if (!CanSend())
            throw new Exception("Channel is not routable");

        Interlocked.Increment(ref Metrics.InFlight);
        var sw = Stopwatch.StartNew();

        try
        {
            var res = await Transport.SendAsync(payload, ct);

            OnSuccess();
            return res;
        }
        catch
        {
            OnFailure();
            throw;
        }
        finally
        {
            sw.Stop();
            Metrics.LastLatencyMs = sw.ElapsedMilliseconds;
            Interlocked.Decrement(ref Metrics.InFlight);
        }
    }

    // ---------------- Routing Logic ----------------

    public bool CanSend()
    {
        if (State != ChannelState.Healthy)
            return false;

        if (CircuitState == CircuitState.Open)
        {
            if (DateTime.UtcNow - _lastFailureUtc > OpenTimeout)
            {
                CircuitState = CircuitState.HalfOpen;
                return true; // اجازه تست
            }

            return false;
        }

        return true;
    }

    // ---------------- Circuit Transitions ----------------

    private void OnSuccess()
    {
        Metrics.Success++;
        _consecutiveFailures = 0;

        if (CircuitState == CircuitState.HalfOpen)
        {
            CircuitState = CircuitState.Closed;
        }
    }

    private void OnFailure()
    {
        Metrics.Failure++;
        _consecutiveFailures++;
        _lastFailureUtc = DateTime.UtcNow;

        if (_consecutiveFailures >= FailureThreshold)
        {
            CircuitState = CircuitState.Open;
        }
    }

    // ---------------- Heartbeat Hooks ----------------

    public void MarkTransportHealthy()
    {
        State = ChannelState.Healthy;
    }

    public void MarkDown()
    {
        State = ChannelState.Down;
    }
}

