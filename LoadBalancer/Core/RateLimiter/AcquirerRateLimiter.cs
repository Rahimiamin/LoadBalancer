namespace LoadBalancer.Core.RateLimiter;

public sealed class AcquirerRateLimiter
{
    private readonly SemaphoreSlim _semaphore;

    public AcquirerRateLimiter(int maxConcurrent)
    {
        _semaphore = new SemaphoreSlim(maxConcurrent, maxConcurrent);
    }

    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> work,
        CancellationToken ct)
    {
        if (!await _semaphore.WaitAsync(0, ct))
            throw new InvalidOperationException("Rate limit exceeded");

        try
        {
            return await work(ct);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
