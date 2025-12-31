namespace LoadBalancer.Infrastructure.Health;

public sealed class MetricsSnapshot
{
    public long TotalRequests;
    public long FailedRequests;
    public double Tps;
    public double P95LatencyMs;
    public double P99LatencyMs;
}
