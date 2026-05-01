using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SignalingServer.Services;

public class StunServer
{
    public const int StunPort = 3478;
    private readonly ConcurrentDictionary<IPEndPoint, DateTime> _bindings = new();
    private UdpClient? _udpServer;
    private CancellationTokenSource? _cts;
    private Task? _listenTask;

    public bool IsRunning => _listenTask != null && !_listenTask.IsCompleted;

    public async Task StartAsync()
    {
        if (IsRunning) return;

        _cts = new CancellationTokenSource();
        _udpServer = new UdpClient(StunPort);

        Console.WriteLine($"[STUN] Server starting on UDP port {StunPort}...");
        
        _listenTask = ListenAsync(_cts.Token);
        
        await Task.Delay(100);
        Console.WriteLine($"[STUN] Server is running on UDP port {StunPort}");
    }

    public async Task StopAsync()
    {
        _cts?.Cancel();
        
        if (_listenTask != null)
        {
            try
            {
                await _listenTask;
            }
            catch (OperationCanceledException)
            {
            }
        }
        
        _udpServer?.Close();
        _udpServer?.Dispose();
        
        Console.WriteLine("[STUN] Server stopped.");
    }

    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = await _udpServer!.ReceiveAsync(cancellationToken);
                _ = HandleStunRequestAsync(result.Buffer, result.RemoteEndPoint, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[STUN] Error: {ex.Message}");
            }
        }
    }

    private async Task HandleStunRequestAsync(byte[] buffer, IPEndPoint remoteEndPoint, CancellationToken cancellationToken)
    {
        try
        {
            if (buffer.Length < 20) return;

            var messageType = (buffer[0] << 8) | buffer[1];
            var messageLength = (buffer[2] << 8) | buffer[3];
            var magicCookie = (buffer[4] << 24) | (buffer[5] << 16) | (buffer[6] << 8) | buffer[7];

            if (magicCookie != 0x2112A442)
            {
                Console.WriteLine($"[STUN] Invalid magic cookie from {remoteEndPoint}");
                return;
            }

            var transactionId = new byte[12];
            Array.Copy(buffer, 8, transactionId, 0, 12);

            Console.WriteLine($"[STUN] Received message type 0x{messageType:X4} from {remoteEndPoint}");

            switch (messageType)
            {
                case 0x0001:
                    await HandleBindingRequestAsync(remoteEndPoint, transactionId, cancellationToken);
                    break;
                default:
                    Console.WriteLine($"[STUN] Unsupported message type: 0x{messageType:X4}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[STUN] Error handling request: {ex.Message}");
        }
    }

    private async Task HandleBindingRequestAsync(IPEndPoint remoteEndPoint, byte[] transactionId, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[STUN] Binding request from {remoteEndPoint}");

        _bindings[remoteEndPoint] = DateTime.UtcNow;

        var response = CreateBindingSuccessResponse(remoteEndPoint, transactionId);
        
        await _udpServer!.SendAsync(response, response.Length, remoteEndPoint);
        
        Console.WriteLine($"[STUN] Sent binding response to {remoteEndPoint}, mapped address: {remoteEndPoint.Address}:{remoteEndPoint.Port}");
    }

    private byte[] CreateBindingSuccessResponse(IPEndPoint mappedAddress, byte[] transactionId)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write((ushort)0x0101);
        writer.Write((ushort)0);
        writer.Write((byte)0x21);
        writer.Write((byte)0x12);
        writer.Write((byte)0xA4);
        writer.Write((byte)0x42);
        writer.Write(transactionId);

        var xorMappedAddress = CreateXorMappedAddressAttribute(mappedAddress, transactionId);
        writer.Write(xorMappedAddress);

        var messageLength = (int)(ms.Length - 20);
        ms.Seek(2, SeekOrigin.Begin);
        writer.Write((ushort)IPAddress.HostToNetworkOrder((short)messageLength));

        return ms.ToArray();
    }

    private byte[] CreateXorMappedAddressAttribute(IPEndPoint address, byte[] transactionId)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write((ushort)0x0020);
        
        var addressBytes = address.Address.GetAddressBytes();
        var xorPort = (ushort)(address.Port ^ 0x2112);
        
        byte[] xorAddress;
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            xorAddress = new byte[4];
            var magicCookie = new byte[] { 0x21, 0x12, 0xA4, 0x42 };
            for (var i = 0; i < 4; i++)
            {
                xorAddress[i] = (byte)(addressBytes[i] ^ magicCookie[i]);
            }
        }
        else
        {
            xorAddress = new byte[16];
            var magicCookie = new byte[] { 0x21, 0x12, 0xA4, 0x42 };
            for (var i = 0; i < 4; i++)
            {
                xorAddress[i] = (byte)(addressBytes[i] ^ magicCookie[i]);
            }
            for (var i = 4; i < 16; i++)
            {
                xorAddress[i] = (byte)(addressBytes[i] ^ transactionId[i - 4]);
            }
        }

        var attributeLength = (ushort)(address.AddressFamily == AddressFamily.InterNetwork ? 8 : 20);
        writer.Write((ushort)IPAddress.HostToNetworkOrder((short)(attributeLength - 4)));

        writer.Write((byte)0);
        writer.Write((byte)(address.AddressFamily == AddressFamily.InterNetwork ? 1 : 2));
        writer.Write((ushort)IPAddress.HostToNetworkOrder((short)xorPort));
        writer.Write(xorAddress);

        return ms.ToArray();
    }

    public void CleanupOldBindings()
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-5);
        foreach (var kvp in _bindings.Where(kvp => kvp.Value < cutoff))
        {
            _bindings.TryRemove(kvp.Key, out _);
        }
    }
}
