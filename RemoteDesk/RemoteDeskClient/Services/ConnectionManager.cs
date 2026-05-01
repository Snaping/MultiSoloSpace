using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RemoteDeskClient.Models;

namespace RemoteDeskClient.Services;

public class ConnectionManager : IDisposable
{
    private ClientWebSocket? _signalingWebSocket;
    private UdpClient? _p2pUdpClient;
    private TcpClient? _relayTcpClient;
    private NetworkStream? _relayStream;
    
    private readonly string _signalingServerUrl;
    private string? _clientId;
    private string? _peerId;
    private string? _roomId;
    private IPEndPoint? _peerEndPoint;
    
    private readonly CancellationTokenSource _cts = new();
    private Task? _signalingReceiveTask;
    private Task? _p2pReceiveTask;
    private Task? _relayReceiveTask;
    
    private readonly ConcurrentQueue<byte[]> _sendQueue = new();
    private readonly object _sendLock = new();
    
    public ClientMode Mode { get; private set; } = ClientMode.None;
    public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
    public bool IsP2PConnected { get; private set; }
    public bool IsRelayConnected { get; private set; }
    
    public event EventHandler<ConnectionState>? ConnectionStateChanged;
    public event EventHandler<string>? LogMessage;
    public event EventHandler<byte[]>? VideoFrameReceived;
    public event EventHandler<ControlMessage>? ControlMessageReceived;
    public event EventHandler<string>? IceServersReceived;

    public ConnectionManager(string signalingServerUrl = "ws://localhost:5000/ws")
    {
        _signalingServerUrl = signalingServerUrl;
    }

    public async Task<bool> ConnectToSignalingServerAsync()
    {
        try
        {
            _signalingWebSocket = new ClientWebSocket();
            await _signalingWebSocket.ConnectAsync(new Uri(_signalingServerUrl), _cts.Token);
            
            _signalingReceiveTask = ReceiveSignalingMessagesAsync(_cts.Token);
            
            Log($"Connected to signaling server: {_signalingServerUrl}");
            
            await GetIceServersAsync();
            
            return true;
        }
        catch (Exception ex)
        {
            Log($"Failed to connect to signaling server: {ex.Message}");
            return false;
        }
    }

    private async Task GetIceServersAsync()
    {
        await SendSignalingMessageAsync(new SignalMessage
        {
            Type = SignalType.GetServers
        });
    }

    public async Task<bool> JoinRoomAsync(string roomId, ClientMode mode)
    {
        if (_signalingWebSocket?.State != WebSocketState.Open)
        {
            Log("Not connected to signaling server.");
            return false;
        }

        _roomId = roomId;
        Mode = mode;
        
        State = ConnectionState.Connecting;
        ConnectionStateChanged?.Invoke(this, State);
        
        Log($"Joining room: {roomId} as {mode}");
        
        await SendSignalingMessageAsync(new SignalMessage
        {
            Type = SignalType.Join,
            RoomId = roomId,
            Role = mode
        });

        return true;
    }

    public async Task InitializeP2PAsync()
    {
        Log("Initializing P2P connection...");
        
        try
        {
            _p2pUdpClient = new UdpClient(0);
            _p2pUdpClient.EnableBroadcast = true;
            
            var localEndPoint = (IPEndPoint)_p2pUdpClient.Client.LocalEndPoint!;
            Log($"Local P2P port: {localEndPoint.Port}");
            
            _p2pReceiveTask = ReceiveP2PMessagesAsync(_cts.Token);
            
            var publicEndPoint = await GetPublicEndpointViaStunAsync();
            if (publicEndPoint != null)
            {
                Log($"Public endpoint: {publicEndPoint.Address}:{publicEndPoint.Port}");
            }
            
            if (Mode == ClientMode.Controller)
            {
                await SendSignalingMessageAsync(new SignalMessage
                {
                    Type = SignalType.Offer,
                    TargetId = _peerId,
                    Sdp = JsonConvert.SerializeObject(new
                    {
                        LocalPort = localEndPoint.Port,
                        PublicAddress = publicEndPoint?.Address.ToString(),
                        PublicPort = publicEndPoint?.Port ?? 0
                    })
                });
                Log("Sent offer to peer.");
            }
            else
            {
                Log("Waiting for offer from controller...");
            }
        }
        catch (Exception ex)
        {
            Log($"P2P initialization error: {ex.Message}");
        }
    }

    private async Task<IPEndPoint?> GetPublicEndpointViaStunAsync()
    {
        try
        {
            var stunServers = new[]
            {
                ("stun.l.google.com", 19302),
                ("stun1.l.google.com", 19302)
            };

            foreach (var (host, port) in stunServers)
            {
                try
                {
                    using var udp = new UdpClient();
                    udp.Client.ReceiveTimeout = 3000;
                    
                    var stunRequest = CreateStunBindingRequest();
                    await udp.SendAsync(stunRequest, stunRequest.Length, host, port);
                    
                    var remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    var result = await udp.ReceiveAsync();
                    
                    var mappedAddress = ParseStunResponse(result.Buffer);
                    if (mappedAddress != null)
                    {
                        return mappedAddress;
                    }
                }
                catch
                {
                }
            }
        }
        catch (Exception ex)
        {
            Log($"STUN error: {ex.Message}");
        }
        
        return null;
    }

    private byte[] CreateStunBindingRequest()
    {
        var request = new byte[20];
        request[0] = 0x00;
        request[1] = 0x01;
        request[2] = 0x00;
        request[3] = 0x00;
        request[4] = 0x21;
        request[5] = 0x12;
        request[6] = 0xA4;
        request[7] = 0x42;
        
        var random = new Random();
        for (var i = 8; i < 20; i++)
        {
            request[i] = (byte)random.Next(256);
        }
        
        return request;
    }

    private IPEndPoint? ParseStunResponse(byte[] response)
    {
        if (response.Length < 20) return null;
        
        var messageType = (response[0] << 8) | response[1];
        if (messageType != 0x0101) return null;
        
        var messageLength = (response[2] << 8) | response[3];
        
        var offset = 20;
        while (offset + 4 <= response.Length && offset < 20 + messageLength)
        {
            var attrType = (response[offset] << 8) | response[offset + 1];
            var attrLength = (response[offset + 2] << 8) | response[offset + 3];
            
            if (attrType == 0x0020 || attrType == 0x0001)
            {
                var family = response[offset + 5];
                var port = (response[offset + 6] << 8) | response[offset + 7];
                
                var xorPort = port ^ 0x2112;
                
                IPAddress address;
                if (family == 1)
                {
                    var addressBytes = new byte[4];
                    var magicCookie = new byte[] { 0x21, 0x12, 0xA4, 0x42 };
                    for (var i = 0; i < 4; i++)
                    {
                        addressBytes[i] = (byte)(response[offset + 8 + i] ^ magicCookie[i]);
                    }
                    address = new IPAddress(addressBytes);
                }
                else
                {
                    continue;
                }
                
                return new IPEndPoint(address, xorPort);
            }
            
            offset += 4 + ((attrLength + 3) & ~3);
        }
        
        return null;
    }

    public async Task ConnectToRelayAsync(string relayHost, int relayPort)
    {
        try
        {
            Log($"Connecting to relay server: {relayHost}:{relayPort}");
            
            _relayTcpClient = new TcpClient();
            await _relayTcpClient.ConnectAsync(relayHost, relayPort);
            _relayStream = _relayTcpClient.GetStream();
            
            IsRelayConnected = true;
            Log("Connected to relay server.");
            
            _relayReceiveTask = ReceiveRelayMessagesAsync(_cts.Token);
        }
        catch (Exception ex)
        {
            Log($"Relay connection error: {ex.Message}");
        }
    }

    public async Task SendVideoFrameAsync(byte[] frameData)
    {
        var message = new DataMessage
        {
            Type = "video",
            Data = Convert.ToBase64String(frameData),
            Timestamp = DateTime.UtcNow.Ticks
        };
        
        await SendDataMessageAsync(message);
    }

    public async Task SendControlMessageAsync(ControlMessage controlMessage)
    {
        var message = new DataMessage
        {
            Type = "control",
            Data = JsonConvert.SerializeObject(controlMessage),
            Timestamp = DateTime.UtcNow.Ticks
        };
        
        await SendDataMessageAsync(message);
    }

    private async Task SendDataMessageAsync(DataMessage message)
    {
        var json = JsonConvert.SerializeObject(message);
        var bytes = Encoding.UTF8.GetBytes(json);
        
        var lengthBytes = BitConverter.GetBytes(bytes.Length);
        
        if (IsP2PConnected && _peerEndPoint != null && _p2pUdpClient != null)
        {
            try
            {
                using var ms = new MemoryStream();
                ms.Write(lengthBytes, 0, 4);
                ms.Write(bytes, 0, bytes.Length);
                var data = ms.ToArray();
                
                await _p2pUdpClient.SendAsync(data, data.Length, _peerEndPoint);
                return;
            }
            catch
            {
            }
        }
        
        if (IsRelayConnected && _relayStream != null)
        {
            try
            {
                await _relayStream.WriteAsync(lengthBytes, 0, 4);
                await _relayStream.WriteAsync(bytes, 0, bytes.Length);
                await _relayStream.FlushAsync();
            }
            catch
            {
            }
        }
    }

    private async Task SendSignalingMessageAsync(SignalMessage message)
    {
        if (_signalingWebSocket?.State != WebSocketState.Open) return;

        var json = JsonConvert.SerializeObject(message);
        var bytes = Encoding.UTF8.GetBytes(json);
        await _signalingWebSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cts.Token);
    }

    private async Task ReceiveSignalingMessagesAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        var messageBuilder = new StringBuilder();

        while (_signalingWebSocket?.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = await _signalingWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _signalingWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationToken);
                    break;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                messageBuilder.Append(message);

                if (result.EndOfMessage)
                {
                    var fullMessage = messageBuilder.ToString();
                    messageBuilder.Clear();

                    await HandleSignalingMessageAsync(fullMessage);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log($"Signaling receive error: {ex.Message}");
                break;
            }
        }

        State = ConnectionState.Disconnected;
        ConnectionStateChanged?.Invoke(this, State);
    }

    private async Task HandleSignalingMessageAsync(string message)
    {
        try
        {
            var signalMessage = JsonConvert.DeserializeObject<SignalMessage>(message);
            if (signalMessage == null) return;

            Log($"Received signal: {signalMessage.Type}");

            switch (signalMessage.Type)
            {
                case SignalType.Registered:
                    _clientId = signalMessage.ClientId;
                    Log($"Registered with client ID: {_clientId}");
                    break;

                case SignalType.Waiting:
                    Log("Waiting for peer to join...");
                    break;

                case SignalType.PeerConnected:
                    _peerId = signalMessage.PeerId ?? signalMessage.SenderId;
                    Log($"Peer connected: {_peerId}");
                    
                    await InitializeP2PAsync();
                    break;

                case SignalType.Offer:
                    var offerData = JsonConvert.DeserializeObject<dynamic>(signalMessage.Sdp ?? "{}");
                    var remotePort = (int)(offerData?.LocalPort ?? 0);
                    var publicAddress = (string?)(offerData?.PublicAddress ?? "");
                    var publicPort = (int)(offerData?.PublicPort ?? 0);
                    
                    Log($"Received offer - Remote port: {remotePort}, Public: {publicAddress}:{publicPort}");
                    
                    if (Mode == ClientMode.Controlled)
                    {
                        var localPublicEndPoint = await GetPublicEndpointViaStunAsync();
                        
                        await SendSignalingMessageAsync(new SignalMessage
                        {
                            Type = SignalType.Answer,
                            TargetId = _peerId,
                            Sdp = JsonConvert.SerializeObject(new
                            {
                                LocalPort = ((IPEndPoint?)_p2pUdpClient?.Client.LocalEndPoint)?.Port ?? 0,
                                PublicAddress = localPublicEndPoint?.Address.ToString(),
                                PublicPort = localPublicEndPoint?.Port ?? 0
                            })
                        });
                    }
                    
                    if (!string.IsNullOrEmpty(publicAddress) && publicPort > 0)
                    {
                        try
                        {
                            _peerEndPoint = new IPEndPoint(IPAddress.Parse(publicAddress), publicPort);
                            Log($"Set peer endpoint (public): {_peerEndPoint}");
                            
                            await TestP2PConnectionAsync();
                        }
                        catch (Exception ex)
                        {
                            Log($"Failed to set peer endpoint: {ex.Message}");
                        }
                    }
                    break;

                case SignalType.Answer:
                    var answerData = JsonConvert.DeserializeObject<dynamic>(signalMessage.Sdp ?? "{}");
                    var answerPort = (int)(answerData?.LocalPort ?? 0);
                    var answerPublicAddress = (string?)(answerData?.PublicAddress ?? "");
                    var answerPublicPort = (int)(answerData?.PublicPort ?? 0);
                    
                    Log($"Received answer - Remote port: {answerPort}, Public: {answerPublicAddress}:{answerPublicPort}");
                    
                    if (!string.IsNullOrEmpty(answerPublicAddress) && answerPublicPort > 0)
                    {
                        try
                        {
                            _peerEndPoint = new IPEndPoint(IPAddress.Parse(answerPublicAddress), answerPublicPort);
                            Log($"Set peer endpoint (public): {_peerEndPoint}");
                        }
                        catch (Exception ex)
                        {
                            Log($"Failed to set peer endpoint: {ex.Message}");
                        }
                    }
                    
                    await TestP2PConnectionAsync();
                    break;

                case SignalType.IceCandidate:
                    Log($"ICE candidate: {signalMessage.Candidate}");
                    break;

                case SignalType.ServersInfo:
                    if (signalMessage.IceServers != null)
                    {
                        IceServersReceived?.Invoke(this, JsonConvert.SerializeObject(signalMessage.IceServers));
                        Log($"Received {signalMessage.IceServers.Count} ICE servers");
                    }
                    break;

                case SignalType.Hangup:
                    Log("Peer hung up.");
                    await DisconnectAsync();
                    break;
            }
        }
        catch (Exception ex)
        {
            Log($"Error handling signaling message: {ex.Message}");
        }
    }

    private async Task TestP2PConnectionAsync()
    {
        if (_peerEndPoint == null || _p2pUdpClient == null) return;
        
        try
        {
            var testMessage = new DataMessage
            {
                Type = "ping",
                Timestamp = DateTime.UtcNow.Ticks
            };
            var json = JsonConvert.SerializeObject(testMessage);
            var bytes = Encoding.UTF8.GetBytes(json);
            var lengthBytes = BitConverter.GetBytes(bytes.Length);
            
            using var ms = new MemoryStream();
            ms.Write(lengthBytes, 0, 4);
            ms.Write(bytes, 0, bytes.Length);
            var data = ms.ToArray();
            
            for (var i = 0; i < 5; i++)
            {
                await _p2pUdpClient.SendAsync(data, data.Length, _peerEndPoint);
                await Task.Delay(200);
            }
            
            Log($"Sent P2P ping to {_peerEndPoint}");
        }
        catch (Exception ex)
        {
            Log($"P2P test error: {ex.Message}");
        }
    }

    private async Task ReceiveP2PMessagesAsync(CancellationToken cancellationToken)
    {
        while (_p2pUdpClient != null && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = await _p2pUdpClient.ReceiveAsync();
                
                if (result.Buffer.Length >= 4)
                {
                    var messageLength = BitConverter.ToInt32(result.Buffer, 0);
                    if (messageLength > 0 && result.Buffer.Length >= 4 + messageLength)
                    {
                        var json = Encoding.UTF8.GetString(result.Buffer, 4, messageLength);
                        HandleDataMessage(json, result.RemoteEndPoint);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
            }
        }
    }

    private async Task ReceiveRelayMessagesAsync(CancellationToken cancellationToken)
    {
        var lengthBuffer = new byte[4];
        var dataBuffer = new byte[65536];

        while (_relayStream != null && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                var bytesRead = await _relayStream.ReadAsync(lengthBuffer, 0, 4, cancellationToken);
                if (bytesRead == 0) break;

                var messageLength = BitConverter.ToInt32(lengthBuffer, 0);
                if (messageLength <= 0 || messageLength > 65536) continue;

                bytesRead = 0;
                while (bytesRead < messageLength)
                {
                    var read = await _relayStream.ReadAsync(dataBuffer, bytesRead, messageLength - bytesRead, cancellationToken);
                    if (read == 0) break;
                    bytesRead += read;
                }

                if (bytesRead == messageLength)
                {
                    var json = Encoding.UTF8.GetString(dataBuffer, 0, messageLength);
                    HandleDataMessage(json, null);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
            }
        }
    }

    private async void HandleDataMessage(string json, IPEndPoint? remoteEndPoint)
    {
        try
        {
            var message = JsonConvert.DeserializeObject<DataMessage>(json);
            if (message == null) return;

            switch (message.Type)
            {
                case "ping":
                    if (remoteEndPoint != null)
                    {
                        _peerEndPoint = remoteEndPoint;
                        IsP2PConnected = true;
                        
                        State = ConnectionState.Connected;
                        ConnectionStateChanged?.Invoke(this, State);
                        
                        Log($"P2P connection established with {remoteEndPoint}");
                        
                        var pongMessage = new DataMessage
                        {
                            Type = "pong",
                            Timestamp = DateTime.UtcNow.Ticks
                        };
                        var pongJson = JsonConvert.SerializeObject(pongMessage);
                        var pongBytes = Encoding.UTF8.GetBytes(pongJson);
                        var pongLengthBytes = BitConverter.GetBytes(pongBytes.Length);
                        
                        using var ms = new MemoryStream();
                        ms.Write(pongLengthBytes, 0, 4);
                        ms.Write(pongBytes, 0, pongBytes.Length);
                        var pongData = ms.ToArray();
                        
                        if (_p2pUdpClient != null)
                        {
                            await _p2pUdpClient.SendAsync(pongData, pongData.Length, remoteEndPoint);
                            Log("Sent pong response.");
                        }
                    }
                    break;
                
                case "pong":
                    if (remoteEndPoint != null && !IsP2PConnected)
                    {
                        _peerEndPoint = remoteEndPoint;
                        IsP2PConnected = true;
                        
                        State = ConnectionState.Connected;
                        ConnectionStateChanged?.Invoke(this, State);
                        
                        Log($"P2P connection confirmed with {remoteEndPoint}");
                    }
                    break;

                case "video":
                    var videoBytes = Convert.FromBase64String(message.Data ?? "");
                    VideoFrameReceived?.Invoke(this, videoBytes);
                    break;

                case "control":
                    var controlMessage = JsonConvert.DeserializeObject<ControlMessage>(message.Data ?? "{}");
                    if (controlMessage != null)
                    {
                        ControlMessageReceived?.Invoke(this, controlMessage);
                    }
                    break;
            }
        }
        catch
        {
        }
    }

    public async Task DisconnectAsync()
    {
        State = ConnectionState.Disconnecting;
        ConnectionStateChanged?.Invoke(this, State);

        if (!string.IsNullOrEmpty(_peerId) && _signalingWebSocket?.State == WebSocketState.Open)
        {
            await SendSignalingMessageAsync(new SignalMessage
            {
                Type = SignalType.Hangup,
                TargetId = _peerId
            });
        }

        _cts.Cancel();
        
        if (_signalingReceiveTask != null)
        {
            try
            {
                await _signalingReceiveTask;
            }
            catch
            {
            }
        }

        if (_p2pReceiveTask != null)
        {
            try
            {
                await _p2pReceiveTask;
            }
            catch
            {
            }
        }

        if (_relayReceiveTask != null)
        {
            try
            {
                await _relayReceiveTask;
            }
            catch
            {
            }
        }

        _p2pUdpClient?.Close();
        _p2pUdpClient?.Dispose();
        
        _relayStream?.Close();
        _relayStream?.Dispose();
        _relayTcpClient?.Close();
        _relayTcpClient?.Dispose();
        
        if (_signalingWebSocket != null)
        {
            if (_signalingWebSocket.State == WebSocketState.Open)
            {
                await _signalingWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", CancellationToken.None);
            }
            _signalingWebSocket.Dispose();
        }

        IsP2PConnected = false;
        IsRelayConnected = false;
        _peerEndPoint = null;
        _peerId = null;
        _clientId = null;
        Mode = ClientMode.None;
        
        State = ConnectionState.Disconnected;
        ConnectionStateChanged?.Invoke(this, State);
        
        Log("Disconnected.");
    }

    private void Log(string message)
    {
        LogMessage?.Invoke(this, message);
    }

    public void Dispose()
    {
        _ = DisconnectAsync();
        _cts.Dispose();
    }
}

public class DataMessage
{
    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    [JsonProperty("data")]
    public string? Data { get; set; }

    [JsonProperty("timestamp")]
    public long Timestamp { get; set; }
}
