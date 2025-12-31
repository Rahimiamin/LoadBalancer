namespace LoadBalancer.Core.Protocol;

public interface IProtocolAdapter<TRequest, TResponse>
{
    byte[] Encode(TRequest request);
    TResponse Decode(byte[] payload);
}
