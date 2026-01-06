using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace LoadBalancer.Core.Backpressure;

public interface IAdaptiveBackpressure
{
    Task<T> EnqueueAsync<T>(
        Func<CancellationToken, Task<T>> work,
        CancellationToken ct);
}
public sealed class AdaptiveBackpressureQueue : IAdaptiveBackpressure
{
    private readonly int _baseConcurrency;
    private readonly Func<BackpressureSnapshot> _snapshot;

    private readonly SemaphoreSlim _semaphore;
    private volatile int _currentLimit;

    public AdaptiveBackpressureQueue(
        int baseConcurrency,
        Func<BackpressureSnapshot> snapshot)
    {
        _baseConcurrency = baseConcurrency;
        _snapshot = snapshot;

        _currentLimit = baseConcurrency;
        _semaphore = new SemaphoreSlim(baseConcurrency, baseConcurrency);
    }

    public async Task<T> EnqueueAsync<T>(
        Func<CancellationToken, Task<T>> work,
        CancellationToken ct)
    {
        AdjustLimit();

        await _semaphore.WaitAsync(ct);

        try
        {
            return await work(ct);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private void AdjustLimit()
    {
        var snap = _snapshot();

        var pressure = snap.Pressure;

        int newLimit;

        if (pressure > 0.85)
            newLimit = Math.Max(1, _baseConcurrency / 4);
        else if (pressure > 0.65)
            newLimit = Math.Max(1, _baseConcurrency / 2);
        else if (pressure < 0.3)
            newLimit = _baseConcurrency;
        else
            return;

        if (newLimit != _currentLimit)
        {
            var delta = newLimit - _currentLimit;

            if (delta > 0)
                _semaphore.Release(delta);
            else
                for (int i = 0; i < -delta; i++)
                    _semaphore.Wait();

            _currentLimit = newLimit;

            Console.WriteLine(
                $"⚖️ Backpressure adjusted → Concurrency = {_currentLimit} | Pressure={pressure:F2}");
        }
    }
}
public sealed record BackpressureSnapshot(
    double Pressure,
    int InFlight,
    double AvgLatency,
    double FailureRate
);

