using LoadBalancer.Core.Channel;
using LoadBalancer.Core.Factory;
using LoadBalancer.Infrastructure.Config;
using System.Collections.Immutable;

namespace LoadBalancer.Core.Pool;

public sealed class ChannelPool
{
    private ImmutableArray<ManagedChannel> _channels = ImmutableArray<ManagedChannel>.Empty;

    public IEnumerable<ManagedChannel> Routable()
        => _channels.Where(c => c.CanSend());

    public void Reload(IEnumerable<ChannelOptions> options)
    {
        _channels = options.Select(opt => new ManagedChannel(ChannelFactory.Create(opt)))
                           .ToImmutableArray();
    }

    // Heartbeat ساده
    public void StartHeartbeat(TimeSpan interval, CancellationToken ct)
    {
        Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested)
            {
                foreach (var ch in _channels)
                {
                    if (ch.State != ChannelState.Healthy)
                    {
                        try
                        {
                            ch.Transport.TestConnection();
                            ch.MarkTransportHealthy();
                            Console.WriteLine($"🟢 Channel {ch.Transport.Name} recovered");
                        }
                        catch
                        {
                            ch.MarkDown();
                        }
                    }
                }

                await Task.Delay(interval, ct);
            }
        }, ct);
    }

}


//Auto Recovery کانال‌ها

//می‌توانیم یک Timer ساده یا BackgroundTask برای Pool یا Engine بسازیم که کانال‌های Down یا Unhealthy را دوباره بررسی و MarkHealthy کند.
//public void RecoverChannels()
//    {
//        foreach (var ch in _channels)
//        {
//            if (ch.State == ChannelState.Unhealthy)
//            {
//                // Ping ساده یا Heartbeat
//                try
//                {
//                    // فرضاً متد ساده تست اتصال
//                    ch.Transport.TestConnection();
//                    ch.MarkHealthy();
//                }
//                catch
//                {
//                    ch.MarkDown();
//                }
//            }
//        }
//    }
