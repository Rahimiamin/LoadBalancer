using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoadBalancer.Core.Retry;

public sealed class RetryBudget
{
    private readonly int _maxRetries;
    private int _used;

    public RetryBudget(int maxRetries)
    {
        _maxRetries = maxRetries;
    }

    public bool TryConsume()
    {
        while (true)
        {
            var current = _used;
            if (current >= _maxRetries)
                return false;

            if (Interlocked.CompareExchange(
                ref _used, current + 1, current) == current)
                return true;
        }
    }

    public void Reset()
    {
        Interlocked.Exchange(ref _used, 0);
    }

    public int Used => _used;
    public int Max => _maxRetries;
}
