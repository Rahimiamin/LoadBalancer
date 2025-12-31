namespace LoadBalancer.Infrastructure.Config;

public sealed class LoadBalancerOptions
{
    public string Algorithm { get; set; } = "RoundRobin";
    public int BackpressureCapacity { get; set; } = 10000;
}
public sealed class ChannelOptions
{
    public string Name { get; set; } = default!;
    public string Ip { get; set; } = default!;
    public int Port { get; set; }
    public string Protocol { get; set; } = "ISO8583";
    public bool Enabled { get; set; } = true;
}
public sealed class AcquirerOptions
{
    public string AcquirerId { get; set; } = default!;
    public IEnumerable<ChannelOptions> Channels { get; set; }
}
public sealed class RoutingOptions
{
    public List<AcquirerOptions> Acquirers { get; set; } = new();
}
