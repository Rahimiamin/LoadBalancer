using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace LoadBalancer.Core.Transport;

public sealed class TcpChannel
{
    private readonly IPEndPoint _endpoint;
    private TcpClient? _client;
    private NetworkStream? _stream;

    private int _active;
    public string Name { get; }

    public TcpChannel(string name, IPEndPoint endpoint)
    {
        Name = name;
        _endpoint = endpoint;
    }

    public bool IsHealthy =>
        _client is { Connected: true };

    public int ActiveConnections =>
        Volatile.Read(ref _active);

    public async ValueTask<byte[]> SendAsync(
        byte[] payload,
        CancellationToken ct)
    {
        Interlocked.Increment(ref _active);

        try
        {
            await EnsureConnected(ct);

            await _stream!.WriteAsync(payload, ct);

            var buffer = new byte[8192];
            var read = await _stream.ReadAsync(buffer, ct);

            return buffer[..read];
        }
        finally
        {
            Interlocked.Decrement(ref _active);
        }
    }

    private async Task EnsureConnected(CancellationToken ct)
    {
        if (_client?.Connected == true)
            return;

        _client?.Dispose();

        _client = new TcpClient { NoDelay = true };
        await _client.ConnectAsync(_endpoint, ct);

        _stream = _client.GetStream();
    }
}
