using System.Diagnostics;

namespace LoadBalancer.Core.Channel;

public enum CircuitState
{
    Closed,
    Open,
    HalfOpen
}

public sealed class CircuitBreaker
{
    private int _failures;
    private long _openedAtTicks;
    private int _state;

    private readonly int _failureThreshold;
    private readonly TimeSpan _openDuration;

    public CircuitBreaker(int failureThreshold, TimeSpan openDuration)
    {
        _failureThreshold = failureThreshold;
        _openDuration = openDuration;
        _state = (int)CircuitState.Closed;
    }

    public bool AllowRequest()
    {
        var state = (CircuitState)Volatile.Read(ref _state);

        if (state == CircuitState.Closed)
            return true;

        if (state == CircuitState.Open)
        {
            var elapsedMs =
                (Stopwatch.GetTimestamp() - _openedAtTicks) * 1000
                / Stopwatch.Frequency;

            if (elapsedMs >= _openDuration.TotalMilliseconds)
            {
                Interlocked.Exchange(ref _state, (int)CircuitState.HalfOpen);
                return true;
            }

            return false;
        }

        return true; // HalfOpen
    }

    public void OnSuccess()
    {
        Interlocked.Exchange(ref _failures, 0);
        Interlocked.Exchange(ref _state, (int)CircuitState.Closed);
    }

    public void OnFailure()
    {
        if (Interlocked.Increment(ref _failures) < _failureThreshold)
            return;

        _openedAtTicks = Stopwatch.GetTimestamp();
        Interlocked.Exchange(ref _state, (int)CircuitState.Open);
    }
}


