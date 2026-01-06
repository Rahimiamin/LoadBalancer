namespace LoadBalancer.Infrastructure.Config;

public sealed class LoadBalancerOptions
{
    public string Algorithm { get; set; } = "RoundRobin";
    public int BackpressureCapacity { get; set; } = 10000;
}

public sealed class RoutingOptions
{
    public List<AcquirerOptions> Acquirers { get; set; }
}
public class AcquirerOptions
{
    public string AcquirerId { get; set; }
    public int MaxTPS { get; set; }
    public int Weight { get; set; }
    public List<ChannelOptions> Channels { get; set; }
    public string Strategy { get; set; } = "RoundRobin"; // یا "LeastConnections"
}

public class ChannelOptions
{
    public string Name { get; set; }
    public string Ip { get; set; }
    public int Port { get; set; }
    public bool Enabled { get; set; } = true;
}




public class Loadbalancersettings
{
    public int MaxConcurrent { get; set; } = 100;
    public int HeartbeatIntervalMs { get; set; } = 5000;
    public List<Acquirer> Acquirers { get; set; }
}

public class Acquirer
{
    public string AcquirerId { get; set; }
    public int MaxTPS { get; set; }
    public float Weight { get; set; }
    public string Strategy { get; set; }
    public List<ChannelOptions> Channels { get; set; }
}

