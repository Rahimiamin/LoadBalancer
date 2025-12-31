using System.Threading.Channels;

namespace LoadBalancer.Core.Backpressure;

public sealed class BackpressureQueue
{
    private readonly Channel<Func<Task>> _queue;

    public BackpressureQueue(int capacity)
    {
        _queue = System.Threading.Channels.Channel.CreateBounded<Func<Task>>(
            new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.DropWrite
            });
    }

    public bool TryEnqueue(Func<Task> work)
        => _queue.Writer.TryWrite(work);

    public async Task RunAsync(CancellationToken ct)
    {
        await foreach (var work in _queue.Reader.ReadAllAsync(ct))
            await work();
    }
}
