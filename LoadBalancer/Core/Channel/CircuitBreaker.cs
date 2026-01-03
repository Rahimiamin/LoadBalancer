using System.Diagnostics;

namespace LoadBalancer.Core.Channel;

public enum CircuitState
{
    Closed,
    Open,
    HalfOpen
}

public class CircuitBreaker
{
    private readonly int _failureThreshold;
    private readonly TimeSpan _openDuration;

    private int _failures;
    private DateTime _openedAt;

    public CircuitState State { get; private set; } = CircuitState.Closed;

    public CircuitBreaker(int failureThreshold, TimeSpan openDuration)
    {
        _failureThreshold = failureThreshold;
        _openDuration = openDuration;
    }

    public bool AllowRequest()
    {
        if (State == CircuitState.Open)
        {
            if (DateTime.UtcNow - _openedAt > _openDuration)
            {
                State = CircuitState.HalfOpen;
                return true;
            }
            return false;
        }

        return true;
    }

    public void OnSuccess()
    {
        _failures = 0;
        State = CircuitState.Closed;
    }

    public void OnFailure()
    {
        _failures++;

        if (_failures >= _failureThreshold)
        {
            State = CircuitState.Open;
            _openedAt = DateTime.UtcNow;

            Console.WriteLine("⚡ Circuit OPENED");
        }
    }
}


