using System.Net;
using LoadBalancer.Core.Transport;
using LoadBalancer.Infrastructure.Config;

namespace LoadBalancer.Core.Factory;

public static class ChannelFactory
{
    public static TcpChannel Create(ChannelOptions opt)
    {
        return new TcpChannel(
            opt.Name,
            new IPEndPoint(
                IPAddress.Parse(opt.Ip),
                opt.Port));
    }
}
