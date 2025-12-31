using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoadBalancer.Core.Idempotency;

public readonly struct IdempotencyKey
{
    public readonly string TerminalId;
    public readonly string TraceNumber;

    public IdempotencyKey(string terminalId, string traceNumber)
    {
        TerminalId = terminalId;
        TraceNumber = traceNumber;
    }

    public override int GetHashCode()
        => HashCode.Combine(TerminalId, TraceNumber);
}
public sealed class IdempotencyStore
{
    private readonly ConcurrentDictionary<IdempotencyKey, byte[]> _cache
        = new();

    private readonly TimeSpan _ttl = TimeSpan.FromMinutes(2);

    public bool TryGet(
        IdempotencyKey key,
        out byte[] response)
    {
        return _cache.TryGetValue(key, out response);
    }

    public void Store(
        IdempotencyKey key,
        byte[] response)
    {
        _cache[key] = response;
    }
}
