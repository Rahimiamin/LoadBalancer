namespace LoadBalancer.Core.Channel;

//public sealed class ReconnectWorker
//{
//    private readonly IReadOnlyList<TcpChannel> _channels;
//    private readonly TimeSpan _baseDelay;

//    public ReconnectWorker(
//        IReadOnlyList<TcpChannel> channels,
//        TimeSpan baseDelay)
//    {
//        _channels = channels;
//        _baseDelay = baseDelay;
//    }

//    public async Task RunAsync(CancellationToken ct)
//    {
//        while (!ct.IsCancellationRequested)
//        {
//            foreach (var ch in _channels)
//            {
//                if (ch.State != ChannelState.Down)
//                    continue;

//                try
//                {
//                    await ch.TryReconnectAsync();
//                }
//                catch
//                {
//                    await Task.Delay(_baseDelay, ct);
//                }
//            }

//            await Task.Delay(500, ct);
//        }
//    }
//}
