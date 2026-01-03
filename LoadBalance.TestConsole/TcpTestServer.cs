using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public sealed class TcpTestServer
{
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cts = new();

    public TcpTestServer(string ip, int port)
    {
        _listener = new TcpListener(IPAddress.Parse(ip), port);
    }

    public void Start()
    {
        Task.Run(async () =>
        {
            _listener.Start();
            Console.WriteLine($"🟢 TCP Server UP on {_listener.LocalEndpoint}");

            while (!_cts.IsCancellationRequested)
            {
                if (!_listener.Pending())
                {
                    await Task.Delay(50);
                    continue;
                }

                var client = await _listener.AcceptTcpClientAsync();
                _ = Handle(client);
            }
        });
    }

    public void Stop()
    {
        Console.WriteLine($"🔴 TCP Server DOWN on {_listener.LocalEndpoint}");
        _cts.Cancel();
        _listener.Stop();
    }

    private async Task Handle(TcpClient client)
    {
    
        using (client)
        using (var stream = client.GetStream())
        {
            var buffer = new byte[1024];
            int read = await stream.ReadAsync(buffer);
            await stream.WriteAsync(buffer.AsMemory(0, read));
        }
    }
}

