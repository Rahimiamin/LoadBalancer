using LoadBalancer.Core.Routing;

namespace LoadBalancer.Core.Transport;

public sealed class TransactionHandler
{
    private readonly RoutingEngine _router;

    public TransactionHandler(RoutingEngine router)
    {
        _router = router;
    }

    public Task<byte[]> HandleAsync(
        string acq,
        string terminal,
        string trace,
        byte[] payload,
        CancellationToken ct)
    {
        return _router.RouteAsync(payload, ct);
    }
}
