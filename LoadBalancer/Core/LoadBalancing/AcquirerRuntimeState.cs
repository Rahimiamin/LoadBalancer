namespace LoadBalancer.Core.LoadBalancing;

public class AcquirerRuntimeState
{
    public string AcquirerId { get; init; } = default!;
    public int InFlight;
    public long WindowCount;
    public long WindowStartTicks;
}
