using LoadBalancer.Core.RateLimiter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoadBalancer.Core.Backpressure;

public sealed class RateLimitedBackpressure : IAdaptiveBackpressure
{
    private readonly IAdaptiveBackpressure _inner;
    private readonly AcquirerRateLimiter _limiter;

    public RateLimitedBackpressure(
        IAdaptiveBackpressure inner,
        AcquirerRateLimiter limiter)
    {
        _inner = inner;
        _limiter = limiter;
    }

    public Task<T> EnqueueAsync<T>(
        Func<CancellationToken, Task<T>> work,
        CancellationToken ct)
    {
        return _limiter.ExecuteAsync(
            innerCt => _inner.EnqueueAsync(work, innerCt),
            ct);
    }
}
