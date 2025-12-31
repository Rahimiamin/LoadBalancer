// See https://aka.ms/new-console-template for more information

using LoadBalancer.Core.Channel;
using LoadBalancer.Core.LoadBalancing;
using LoadBalancer.Core.Pool;
using LoadBalancer.Core.Routing;
using LoadBalancer.Infrastructure.Health;

var channels = new List<TcpChannel>
{
    new TcpChannel("10.0.0.1", 5000),
    new TcpChannel("10.0.0.2", 5000)
};

var pool = new ChannelPool(channels);
var router = new RoutingEngine(pool, new LeastConnectionStrategy());

var cts = new CancellationTokenSource();

var health = new HealthMonitor(
    channels,
    TimeSpan.FromSeconds(5),
    TimeSpan.FromSeconds(1));

var reconnect = new ReconnectWorker(
    channels,
    TimeSpan.FromSeconds(2));

_ = Task.Run(() => health.RunAsync(cts.Token));
_ = Task.Run(() => reconnect.RunAsync(cts.Token));



Console.WriteLine("Hello, World!");
