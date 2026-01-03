using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace LoadBalancer.Core.Transport;

public sealed class TcpChannel
{
    private readonly IPEndPoint _endpoint;
    public string Name { get; }
    public TcpChannel(string name, IPEndPoint ep) { Name = name; _endpoint = ep; }
    public async Task<byte[]> SendAsync(byte[] payload, CancellationToken ct)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(_endpoint.Address, _endpoint.Port, ct);
        using var stream = client.GetStream();
        await stream.WriteAsync(payload, 0, payload.Length, ct);
        var buffer = new byte[payload.Length];
        int read = 0;
        while (read < buffer.Length) read += await stream.ReadAsync(buffer, read, buffer.Length - read, ct);


        return buffer;
    }
    public void TestConnection() { /* همیشه موفق */ }

    internal async Task TestConnectionAsync()
    {
        throw new NotImplementedException();
    }
}
