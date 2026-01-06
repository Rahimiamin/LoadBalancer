using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoadBalancer.Core.Retry;

public sealed class RetryPolicy
{
    public int MaxRetries { get; }
    public TimeSpan BaseDelay { get; }

    public RetryPolicy(int maxRetries, TimeSpan baseDelay)
    {
        MaxRetries = maxRetries;
        BaseDelay = baseDelay;
    }

    public async Task<T> ExecuteAsync<T>(
        Func<Task<T>> action,
        Func<Exception, bool> shouldRetry,
        CancellationToken ct)
    {
        Exception? last = null;

        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                return await action();
            }
            catch (Exception ex) when (shouldRetry(ex))
            {
                last = ex;

                var delay = TimeSpan.FromMilliseconds(
                    BaseDelay.TotalMilliseconds * attempt);

                await Task.Delay(delay, ct);
            }
        }

        throw last!;
    }
}
