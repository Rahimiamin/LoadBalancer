namespace LoadBalancer.Core.Channel;

public enum ChannelState
{
    Healthy,
    Unhealthy,
    Cooldown,
    Down
}