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
    private readonly int _failureThreshold;
    private readonly TimeSpan _openDuration;
    private readonly int _halfOpenSuccessNeeded = 2;

    private int _failureCount;
    private int _halfOpenSuccess;
    private DateTime _openedAt;

    public CircuitState State { get; private set; } = CircuitState.Closed;

    public CircuitBreaker(int failureThreshold, TimeSpan openDuration)
    {
        _failureThreshold = failureThreshold;
        _openDuration = openDuration;
    }

    public bool AllowRequest()
    {
        if (State == CircuitState.Closed)
            return true;

        if (State == CircuitState.Open)
        {
            if (DateTime.UtcNow - _openedAt >= _openDuration)
            {
                State = CircuitState.HalfOpen;
                _halfOpenSuccess = 0;
                return true; // اجازه یک probe
            }
            return false;
        }

        // HalfOpen
        return true;
    }

    public void OnSuccess()
    {
        if (State == CircuitState.HalfOpen)
        {
            _halfOpenSuccess++;
            if (_halfOpenSuccess >= _halfOpenSuccessNeeded)
            {
                Reset();
            }
        }
        else
        {
            _failureCount = 0;
        }
    }

    public void OnFailure()
    {
        if (State == CircuitState.HalfOpen)
        {
            Trip();
            return;
        }

        _failureCount++;
        if (_failureCount >= _failureThreshold)
        {
            Trip();
        }
    }

    private void Trip()
    {
        State = CircuitState.Open;
        _openedAt = DateTime.UtcNow;
    }

    private void Reset()
    {
        State = CircuitState.Closed;
        _failureCount = 0;
        _halfOpenSuccess = 0;
    }
}


