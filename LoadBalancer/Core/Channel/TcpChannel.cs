using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;

namespace LoadBalancer.Core.Channel;

public sealed class TcpChannel : IDisposable
{
    private readonly string _host;
    private readonly int _port;

    private TcpClient _client;
    private NetworkStream _stream;
    private int _state;

    private readonly CircuitBreaker _breaker =
        new CircuitBreaker(3, TimeSpan.FromSeconds(5));

    public ChannelMetrics Metrics { get; } = new();

    public ChannelState State =>
        (ChannelState)Volatile.Read(ref _state);

    public TcpChannel(string host, int port)
    {
        _host = host;
        _port = port;
        Connect();
        _state = (int)ChannelState.Healthy;
    }

    private void Connect()
    {
        _client = new TcpClient { NoDelay = true };
        _client.Connect(_host, _port);
        _stream = _client.GetStream();
    }

    public async ValueTask<byte[]> SendAsync(
        byte[] payload,
        CancellationToken ct)
    {
        if (State == ChannelState.Down)
            throw new InvalidOperationException("Channel down");

        if (!_breaker.AllowRequest())
            throw new InvalidOperationException("Circuit open");

        try
        {
            Interlocked.Increment(ref Metrics.InFlight);

            await _stream.WriteAsync(payload, ct);
            await _stream.FlushAsync(ct);

            var buffer = ArrayPool<byte>.Shared.Rent(4096);
            var len = await _stream.ReadAsync(buffer, ct);

            Metrics.MarkSuccess();
            _breaker.OnSuccess();

            return buffer[..len];
        }
        catch
        {
            _breaker.OnFailure();
            MarkDown();
            throw;
        }
        finally
        {
            Interlocked.Decrement(ref Metrics.InFlight);
        }
    }

    public void MarkDown()
    {
        if (Interlocked.Exchange(ref _state, (int)ChannelState.Down)
            == (int)ChannelState.Down)
            return;

        try { _client.Close(); } catch { }
    }

    public void Dispose()
    {
        try { _stream?.Dispose(); } catch { }
        try { _client?.Dispose(); } catch { }
    }
}



