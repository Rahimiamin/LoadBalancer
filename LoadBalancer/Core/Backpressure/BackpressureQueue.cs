using System.Collections.Concurrent;

namespace LoadBalancer.Core.Channel;

public sealed class BackpressureQueue
{
    private readonly SemaphoreSlim _semaphore;
    private readonly ConcurrentQueue<(byte[] payload, TaskCompletionSource<byte[]> tcs)> _queue
        = new();

    public BackpressureQueue(int maxConcurrent)
    {
        _semaphore = new SemaphoreSlim(maxConcurrent, maxConcurrent);
    }

    public async Task<byte[]> EnqueueAsync(byte[] payload, Func<byte[], Task<byte[]>> sendFunc)
    {
        var tcs = new TaskCompletionSource<byte[]>();
        _queue.Enqueue((payload, tcs));

        await _semaphore.WaitAsync();
        if (_queue.TryDequeue(out var item))
        {
            try
            {
                var result = await sendFunc(item.payload);
                item.tcs.SetResult(result);
                return result;
            }
            catch (Exception ex)
            {
                item.tcs.SetException(ex);
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        return await tcs.Task;
    }
}
