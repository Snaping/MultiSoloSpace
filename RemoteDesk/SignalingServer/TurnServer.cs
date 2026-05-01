using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SignalingServer.Services;

public class TurnServer
{
    public const int TurnUdpPort = 3479;
    public const int TurnTcpPort = 3480;
    
    private readonly ConcurrentDictionary<string, TurnAllocation> _allocations = new();
    private readonly ConcurrentDictionary<IPEndPoint, string> _peerToAllocation = new();
    
    private UdpClient? _udpServer;
    private TcpListener? _tcpListener;
    private CancellationTokenSource? _cts;
    private Task? _udpListenTask;
    private Task? _tcpListenTask;

    private readonly string _realm = "remotedesk.local";
    private readonly string _username = "remotedesk";
    private readonly string _password = "remotedesk123";

    public bool IsRunning => (_udpListenTask != null && !_udpListenTask.IsCompleted) ||
                              (_tcpListenTask != null && !_tcpListenTask.IsCompleted);

    public async Task StartAsync()
    {
        if (IsRunning) return;

        _cts = new CancellationTokenSource();
        _udpServer = new UdpClient(TurnUdpPort);
        _tcpListener = new TcpListener(IPAddress.Any, TurnTcpPort);

        Console.WriteLine($"[TURN] Server starting...");
        Console.WriteLine($"[TURN] UDP port: {TurnUdpPort}");
        Console.WriteLine($"[TURN] TCP port: {TurnTcpPort}");
        
        _udpListenTask = ListenUdpAsync(_cts.Token);
        _tcpListenTask = ListenTcpAsync(_cts.Token);
        
        await Task.Delay(100);
        Console.WriteLine($"[TURN] Server is running.");
    }

    public async Task StopAsync()
    {
        _cts?.Cancel();
        
        if (_udpListenTask != null)
        {
            try
            {
                await _udpListenTask;
            }
            catch (OperationCanceledException)
            {
            }
        }
        
        if (_tcpListenTask != null)
        {
            try
            {
                await _tcpListenTask;
            }
            catch (OperationCanceledException)
            {
            }
        }
        
        _udpServer?.Close();
        _udpServer?.Dispose();
        _tcpListener?.Stop();
        
        Console.WriteLine("[TURN] Server stopped.");
    }

    private async Task ListenUdpAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = await _udpServer!.ReceiveAsync(cancellationToken);
                _ = HandleUdpMessageAsync(result.Buffer, result.RemoteEndPoint, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TURN] UDP Error: {ex.Message}");
            }
        }
    }

    private async Task ListenTcpAsync(CancellationToken cancellationToken)
    {
        _tcpListener!.Start();
        Console.WriteLine($"[TURN] TCP listener started on port {TurnTcpPort}");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var client = await _tcpListener.AcceptTcpClientAsync(cancellationToken);
                _ = HandleTcpClientAsync(client, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TURN] TCP Accept Error: {ex.Message}");
            }
        }
        
        _tcpListener.Stop();
    }

    private async Task HandleTcpClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        var remoteEndPoint = (IPEndPoint?)client.Client.RemoteEndPoint;
        Console.WriteLine($"[TURN] New TCP connection from {remoteEndPoint}");

        try
        {
            using var stream = client.GetStream();
            var buffer = new byte[4096];

            while (!cancellationToken.IsCancellationRequested && client.Connected)
            {
                var bytesRead = await stream.ReadAsync(buffer, cancellationToken);
                if (bytesRead == 0) break;

                if (remoteEndPoint != null)
                {
                    var message = new byte[bytesRead];
                    Array.Copy(buffer, message, bytesRead);
                    await HandleMessageAsync(message, remoteEndPoint, true, stream, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TURN] TCP Client Error: {ex.Message}");
        }
        finally
        {
            client.Close();
            Console.WriteLine($"[TURN] TCP connection closed from {remoteEndPoint}");
        }
    }

    private async Task HandleUdpMessageAsync(byte[] buffer, IPEndPoint remoteEndPoint, CancellationToken cancellationToken)
    {
        await HandleMessageAsync(buffer, remoteEndPoint, false, null, cancellationToken);
    }

    private async Task HandleMessageAsync(byte[] buffer, IPEndPoint remoteEndPoint, bool isTcp, NetworkStream? stream, CancellationToken cancellationToken)
    {
        try
        {
            if (buffer.Length < 20) return;

            var messageType = (buffer[0] << 8) | buffer[1];
            var messageLength = (buffer[2] << 8) | buffer[3];
            var magicCookie = (buffer[4] << 24) | (buffer[5] << 16) | (buffer[6] << 8) | buffer[7];

            if (magicCookie != 0x2112A442)
            {
                await HandleDataIndicationAsync(buffer, remoteEndPoint, isTcp, stream, cancellationToken);
                return;
            }

            var transactionId = new byte[12];
            Array.Copy(buffer, 8, transactionId, 0, 12);

            Console.WriteLine($"[TURN] Received message type 0x{messageType:X4} from {remoteEndPoint}");

            switch (messageType)
            {
                case 0x0001:
                    await HandleBindingRequestAsync(remoteEndPoint, transactionId, isTcp, stream, cancellationToken);
                    break;
                case 0x0003:
                    await HandleAllocateRequestAsync(buffer, remoteEndPoint, transactionId, isTcp, stream, cancellationToken);
                    break;
                case 0x0008:
                    await HandleRefreshRequestAsync(buffer, remoteEndPoint, transactionId, isTcp, stream, cancellationToken);
                    break;
                case 0x000C:
                    await HandleCreatePermissionRequestAsync(buffer, remoteEndPoint, transactionId, isTcp, stream, cancellationToken);
                    break;
                case 0x000E:
                    await HandleSendIndicationAsync(buffer, remoteEndPoint, cancellationToken);
                    break;
                default:
                    Console.WriteLine($"[TURN] Unsupported message type: 0x{messageType:X4}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TURN] Error handling message: {ex.Message}");
        }
    }

    private async Task HandleBindingRequestAsync(IPEndPoint remoteEndPoint, byte[] transactionId, bool isTcp, NetworkStream? stream, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[TURN] Binding request from {remoteEndPoint}");
        
        var response = CreateBindingSuccessResponse(remoteEndPoint, transactionId);
        await SendResponseAsync(response, remoteEndPoint, isTcp, stream, cancellationToken);
    }

    private async Task HandleAllocateRequestAsync(byte[] buffer, IPEndPoint remoteEndPoint, byte[] transactionId, bool isTcp, NetworkStream? stream, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[TURN] Allocate request from {remoteEndPoint}");

        var allocationId = Guid.NewGuid().ToString();
        var relayPort = GetAvailableRelayPort();
        var localIp = GetLocalIpAddress();
        
        var allocation = new TurnAllocation
        {
            AllocationId = allocationId,
            ClientEndpoint = remoteEndPoint,
            RelayEndpoint = new IPEndPoint(IPAddress.Parse(localIp), relayPort),
            CreatedAt = DateTime.UtcNow,
            Lifetime = 600
        };

        _allocations[allocationId] = allocation;
        
        Console.WriteLine($"[TURN] Allocated relay endpoint: {allocation.RelayEndpoint} for {remoteEndPoint}");

        var response = CreateAllocateSuccessResponse(remoteEndPoint, allocation.RelayEndpoint, transactionId);
        await SendResponseAsync(response, remoteEndPoint, isTcp, stream, cancellationToken);
    }

    private async Task HandleRefreshRequestAsync(byte[] buffer, IPEndPoint remoteEndPoint, byte[] transactionId, bool isTcp, NetworkStream? stream, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[TURN] Refresh request from {remoteEndPoint}");

        var allocation = _allocations.Values.FirstOrDefault(a => 
            a.ClientEndpoint.Address.Equals(remoteEndPoint.Address) && 
            a.ClientEndpoint.Port == remoteEndPoint.Port);

        if (allocation != null)
        {
            allocation.CreatedAt = DateTime.UtcNow;
            Console.WriteLine($"[TURN] Refreshed allocation: {allocation.AllocationId}");
        }

        var response = CreateRefreshSuccessResponse(transactionId);
        await SendResponseAsync(response, remoteEndPoint, isTcp, stream, cancellationToken);
    }

    private async Task HandleCreatePermissionRequestAsync(byte[] buffer, IPEndPoint remoteEndPoint, byte[] transactionId, bool isTcp, NetworkStream? stream, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[TURN] CreatePermission request from {remoteEndPoint}");

        var response = CreateSuccessResponse(0x0008, transactionId);
        await SendResponseAsync(response, remoteEndPoint, isTcp, stream, cancellationToken);
    }

    private async Task HandleSendIndicationAsync(byte[] buffer, IPEndPoint remoteEndPoint, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[TURN] Send indication from {remoteEndPoint}");

        if (buffer.Length < 20 + 8) return;

        var peerAddress = ParseXorPeerAddressAttribute(buffer, 20);
        if (peerAddress == null) return;

        var data = new byte[buffer.Length - 20 - 12];
        Array.Copy(buffer, 20 + 12, data, 0, data.Length);

        Console.WriteLine($"[TURN] Relaying {data.Length} bytes to {peerAddress}");

        try
        {
            await _udpServer!.SendAsync(data, data.Length, peerAddress);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TURN] Error relaying data: {ex.Message}");
        }
    }

    private async Task HandleDataIndicationAsync(byte[] buffer, IPEndPoint remoteEndPoint, bool isTcp, NetworkStream? stream, CancellationToken cancellationToken)
    {
        var allocation = _allocations.Values.FirstOrDefault(a => 
            a.Permissions.Any(p => p.Address.Equals(remoteEndPoint.Address) && p.Port == remoteEndPoint.Port));

        if (allocation == null) return;

        Console.WriteLine($"[TURN] Data indication from {remoteEndPoint} to {allocation.ClientEndpoint}");

        var indication = CreateDataIndication(buffer, remoteEndPoint);
        await SendResponseAsync(indication, allocation.ClientEndpoint, isTcp, stream, cancellationToken);
    }

    private async Task SendResponseAsync(byte[] response, IPEndPoint remoteEndPoint, bool isTcp, NetworkStream? stream, CancellationToken cancellationToken)
    {
        if (isTcp && stream != null)
        {
            await stream.WriteAsync(response, 0, response.Length, cancellationToken);
        }
        else
        {
            await _udpServer!.SendAsync(response, response.Length, remoteEndPoint);
        }
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

    private byte[] CreateAllocateSuccessResponse(IPEndPoint mappedAddress, IPEndPoint relayAddress, byte[] transactionId)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write((ushort)0x0103);
        writer.Write((ushort)0);
        writer.Write((byte)0x21);
        writer.Write((byte)0x12);
        writer.Write((byte)0xA4);
        writer.Write((byte)0x42);
        writer.Write(transactionId);

        writer.Write(CreateXorMappedAddressAttribute(mappedAddress, transactionId));
        writer.Write(CreateXorRelayedAddressAttribute(relayAddress, transactionId));
        writer.Write(CreateLifetimeAttribute(600));

        var messageLength = (int)(ms.Length - 20);
        ms.Seek(2, SeekOrigin.Begin);
        writer.Write((ushort)IPAddress.HostToNetworkOrder((short)messageLength));

        return ms.ToArray();
    }

    private byte[] CreateRefreshSuccessResponse(byte[] transactionId)
    {
        return CreateSuccessResponse(0x0104, transactionId);
    }

    private byte[] CreateSuccessResponse(ushort method, byte[] transactionId)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write((ushort)method);
        writer.Write((ushort)0);
        writer.Write((byte)0x21);
        writer.Write((byte)0x12);
        writer.Write((byte)0xA4);
        writer.Write((byte)0x42);
        writer.Write(transactionId);

        return ms.ToArray();
    }

    private byte[] CreateXorMappedAddressAttribute(IPEndPoint address, byte[] transactionId)
    {
        return CreateAddressAttribute(0x0020, address, transactionId);
    }

    private byte[] CreateXorRelayedAddressAttribute(IPEndPoint address, byte[] transactionId)
    {
        return CreateAddressAttribute(0x0016, address, transactionId);
    }

    private byte[] CreateAddressAttribute(ushort type, IPEndPoint address, byte[] transactionId)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write((ushort)type);

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

    private byte[] CreateLifetimeAttribute(int lifetime)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write((ushort)0x000D);
        writer.Write((ushort)IPAddress.HostToNetworkOrder((short)4));
        writer.Write((ushort)0);
        writer.Write((ushort)IPAddress.HostToNetworkOrder((short)lifetime));

        return ms.ToArray();
    }

    private byte[] CreateDataIndication(byte[] data, IPEndPoint peerAddress)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write((ushort)0x0115);
        writer.Write((ushort)0);
        writer.Write((byte)0x21);
        writer.Write((byte)0x12);
        writer.Write((byte)0xA4);
        writer.Write((byte)0x42);
        
        var transactionId = new byte[12];
        new Random().NextBytes(transactionId);
        writer.Write(transactionId);

        var xorPeerAddress = CreateAddressAttribute(0x0012, peerAddress, transactionId);
        writer.Write(xorPeerAddress);

        writer.Write((ushort)0x0013);
        writer.Write((ushort)IPAddress.HostToNetworkOrder((short)data.Length));
        writer.Write(data);

        var messageLength = (int)(ms.Length - 20);
        ms.Seek(2, SeekOrigin.Begin);
        writer.Write((ushort)IPAddress.HostToNetworkOrder((short)messageLength));

        return ms.ToArray();
    }

    private IPEndPoint? ParseXorPeerAddressAttribute(byte[] buffer, int offset)
    {
        if (buffer.Length < offset + 8) return null;

        var family = buffer[offset + 1];
        var xorPort = (buffer[offset + 2] << 8) | buffer[offset + 3];
        var port = xorPort ^ 0x2112;

        IPAddress? address;
        if (family == 1)
        {
            if (buffer.Length < offset + 8) return null;
            var addressBytes = new byte[4];
            var magicCookie = new byte[] { 0x21, 0x12, 0xA4, 0x42 };
            for (var i = 0; i < 4; i++)
            {
                addressBytes[i] = (byte)(buffer[offset + 4 + i] ^ magicCookie[i]);
            }
            address = new IPAddress(addressBytes);
        }
        else
        {
            return null;
        }

        return new IPEndPoint(address, port);
    }

    private static readonly object _portLock = new();
    private static int _nextRelayPort = 49152;

    private int GetAvailableRelayPort()
    {
        lock (_portLock)
        {
            return _nextRelayPort++;
        }
    }

    private string GetLocalIpAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        return "127.0.0.1";
    }
}

public class TurnAllocation
{
    public string AllocationId { get; set; } = string.Empty;
    public IPEndPoint ClientEndpoint { get; set; } = null!;
    public IPEndPoint RelayEndpoint { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public int Lifetime { get; set; }
    public List<IPEndPoint> Permissions { get; set; } = new();
}
