using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using SignalingServer;
using SignalingServer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<SignalingService>();
builder.Services.AddSingleton<StunServer>();
builder.Services.AddSingleton<TurnServer>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors("AllowAll");
app.UseWebSockets();

var signalingService = app.Services.GetRequiredService<SignalingService>();
var stunServer = app.Services.GetRequiredService<StunServer>();
var turnServer = app.Services.GetRequiredService<TurnServer>();

_ = stunServer.StartAsync();
_ = turnServer.StartAsync();

app.Map("/ws", async (HttpContext context) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        return;
    }

    var webSocket = await context.WebSockets.AcceptWebSocketAsync();
    var clientId = Guid.NewGuid().ToString();
    
    var clientInfo = new WebSocketClient
    {
        ClientId = clientId,
        WebSocket = webSocket,
        ConnectedAt = DateTime.UtcNow
    };

    signalingService.AddClient(clientInfo);
    
    var remoteEndPoint = context.Connection.RemoteIpAddress?.ToString();
    Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Client connected: {clientId} from {remoteEndPoint}");

    try
    {
        await HandleWebSocketCommunication(clientInfo, signalingService, stunServer, turnServer);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Error for client {clientId}: {ex.Message}");
    }
    finally
    {
        signalingService.RemoveClient(clientId);
        if (webSocket.State != WebSocketState.Closed)
        {
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        }
        webSocket.Dispose();
        Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Client disconnected: {clientId}");
    }
});

app.MapGet("/health", () => new
{
    Status = "Healthy",
    Timestamp = DateTime.UtcNow,
    SignalingClients = signalingService.GetClientCount(),
    StunServer = stunServer.IsRunning ? "Running" : "Stopped",
    TurnServer = turnServer.IsRunning ? "Running" : "Stopped"
});

app.MapGet("/stun-info", () => new
{
    StunPort = StunServer.StunPort,
    TurnUdpPort = TurnServer.TurnUdpPort,
    TurnTcpPort = TurnServer.TurnTcpPort
});

var port = builder.Configuration.GetValue<int>("Port", 5000);
var url = builder.Configuration.GetValue<string>("Urls", $"http://0.0.0.0:{port}");

Console.WriteLine("========================================");
Console.WriteLine("  WebRTC Remote Desktop Signaling Server");
Console.WriteLine("========================================");
Console.WriteLine();
Console.WriteLine($"[INFO] Signaling Server: {url}/ws");
Console.WriteLine($"[INFO] Health Check:   {url}/health");
Console.WriteLine($"[INFO] STUN Server:    UDP port {StunServer.StunPort}");
Console.WriteLine($"[INFO] TURN Server:    UDP port {TurnServer.TurnUdpPort}, TCP port {TurnServer.TurnTcpPort}");
Console.WriteLine();
Console.WriteLine("[INFO] Server started. Waiting for clients...");
Console.WriteLine();

app.Run(url);

static async Task HandleWebSocketCommunication(WebSocketClient client, SignalingService signalingService, StunServer stunServer, TurnServer turnServer)
{
    var buffer = new byte[4096];
    var messageBuilder = new StringBuilder();

    while (client.WebSocket.State == WebSocketState.Open)
    {
        var result = await client.WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        if (result.MessageType == WebSocketMessageType.Close)
        {
            await client.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            break;
        }

        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
        messageBuilder.Append(message);

        if (result.EndOfMessage)
        {
            var fullMessage = messageBuilder.ToString();
            messageBuilder.Clear();

            await HandleMessage(client, fullMessage, signalingService, stunServer, turnServer);
        }
    }
}

static async Task HandleMessage(WebSocketClient client, string message, SignalingService signalingService, StunServer stunServer, TurnServer turnServer)
{
    try
    {
        var signalMessage = JsonConvert.DeserializeObject<SignalMessage>(message);
        if (signalMessage == null) return;

        Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] [{client.ClientId}] Received: {signalMessage.Type}");

        switch (signalMessage.Type)
        {
            case SignalType.Register:
                client.RoomId = signalMessage.RoomId;
                client.Role = signalMessage.Role;
                signalingService.UpdateClient(client);
                await SendMessageAsync(client.WebSocket, new SignalMessage
                {
                    Type = SignalType.Registered,
                    ClientId = client.ClientId
                });
                break;

            case SignalType.Join:
                var roomClients = signalingService.GetClientsInRoom(signalMessage.RoomId);
                
                client.RoomId = signalMessage.RoomId;
                client.Role = signalMessage.Role;
                signalingService.UpdateClient(client);
                
                if (roomClients.Count == 0)
                {
                    await SendMessageAsync(client.WebSocket, new SignalMessage
                    {
                        Type = SignalType.Waiting,
                        RoomId = signalMessage.RoomId
                    });
                }
                else
                {
                    var existingClient = roomClients.First();
                    await SendMessageAsync(existingClient.WebSocket, new SignalMessage
                    {
                        Type = SignalType.PeerConnected,
                        PeerId = client.ClientId,
                        RoomId = signalMessage.RoomId
                    });
                    await SendMessageAsync(client.WebSocket, new SignalMessage
                    {
                        Type = SignalType.PeerConnected,
                        PeerId = existingClient.ClientId,
                        RoomId = signalMessage.RoomId
                    });
                }
                break;

            case SignalType.Offer:
            case SignalType.Answer:
                var targetOffer = signalingService.GetClient(signalMessage.TargetId);
                if (targetOffer != null)
                {
                    await SendMessageAsync(targetOffer.WebSocket, new SignalMessage
                    {
                        Type = signalMessage.Type,
                        Sdp = signalMessage.Sdp,
                        SenderId = client.ClientId
                    });
                }
                break;

            case SignalType.IceCandidate:
                var targetIce = signalingService.GetClient(signalMessage.TargetId);
                if (targetIce != null)
                {
                    await SendMessageAsync(targetIce.WebSocket, new SignalMessage
                    {
                        Type = SignalType.IceCandidate,
                        Candidate = signalMessage.Candidate,
                        SdpMid = signalMessage.SdpMid,
                        SdpMlineIndex = signalMessage.SdpMlineIndex,
                        SenderId = client.ClientId
                    });
                }
                break;

            case SignalType.GetServers:
                var localIp = GetLocalIpAddress();
                var iceServers = new List<IceServerInfo>
                {
                    new IceServerInfo
                    {
                        Urls = new[] { $"stun:{localIp}:{StunServer.StunPort}", "stun:stun.l.google.com:19302" }
                    },
                    new IceServerInfo
                    {
                        Urls = new[] { $"turn:{localIp}:{TurnServer.TurnUdpPort}" },
                        Username = "remotedesk",
                        Credential = "remotedesk123"
                    }
                };
                
                await SendMessageAsync(client.WebSocket, new SignalMessage
                {
                    Type = SignalType.ServersInfo,
                    IceServers = iceServers
                });
                break;

            case SignalType.Hangup:
                var peersInRoom = signalingService.GetClientsInRoom(client.RoomId)
                    .Where(c => c.ClientId != client.ClientId);
                
                foreach (var peer in peersInRoom)
                {
                    await SendMessageAsync(peer.WebSocket, new SignalMessage
                    {
                        Type = SignalType.Hangup,
                        SenderId = client.ClientId
                    });
                }
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error handling message: {ex.Message}");
    }
}

static async Task SendMessageAsync(WebSocket webSocket, SignalMessage message)
{
    var json = JsonConvert.SerializeObject(message);
    var bytes = Encoding.UTF8.GetBytes(json);
    await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
}

static string GetLocalIpAddress()
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

namespace SignalingServer
{
    public class WebSocketClient
    {
        public string ClientId { get; set; } = string.Empty;
        public string? RoomId { get; set; }
        public ClientRole Role { get; set; } = ClientRole.None;
        public WebSocket WebSocket { get; set; } = null!;
        public DateTime ConnectedAt { get; set; }
    }

    public enum ClientRole
    {
        None,
        Controller,
        Controlled
    }

    public enum SignalType
    {
        Register,
        Registered,
        Join,
        Waiting,
        PeerConnected,
        Offer,
        Answer,
        IceCandidate,
        Hangup,
        GetServers,
        ServersInfo
    }

    public class SignalMessage
    {
        [JsonProperty("type")]
        public SignalType Type { get; set; }

        [JsonProperty("clientId")]
        public string? ClientId { get; set; }

        [JsonProperty("senderId")]
        public string? SenderId { get; set; }

        [JsonProperty("targetId")]
        public string? TargetId { get; set; }

        [JsonProperty("roomId")]
        public string? RoomId { get; set; }

        [JsonProperty("role")]
        public ClientRole Role { get; set; }

        [JsonProperty("sdp")]
        public string? Sdp { get; set; }

        [JsonProperty("candidate")]
        public string? Candidate { get; set; }

        [JsonProperty("sdpMid")]
        public string? SdpMid { get; set; }

        [JsonProperty("sdpMlineIndex")]
        public int? SdpMlineIndex { get; set; }

        [JsonProperty("iceServers")]
        public List<IceServerInfo>? IceServers { get; set; }

        [JsonProperty("peerId")]
        public string? PeerId { get; set; }
    }

    public class IceServerInfo
    {
        [JsonProperty("urls")]
        public string[] Urls { get; set; } = Array.Empty<string>();

        [JsonProperty("username")]
        public string? Username { get; set; }

        [JsonProperty("credential")]
        public string? Credential { get; set; }
    }
}

namespace SignalingServer.Services
{
    public class SignalingService
    {
        private readonly ConcurrentDictionary<string, WebSocketClient> _clients = new();

        public void AddClient(WebSocketClient client)
        {
            _clients[client.ClientId] = client;
        }

        public void RemoveClient(string clientId)
        {
            _clients.TryRemove(clientId, out _);
        }

        public void UpdateClient(WebSocketClient client)
        {
            _clients[client.ClientId] = client;
        }

        public WebSocketClient? GetClient(string clientId)
        {
            _clients.TryGetValue(clientId, out var client);
            return client;
        }

        public List<WebSocketClient> GetClientsInRoom(string? roomId)
        {
            return _clients.Values.Where(c => c.RoomId == roomId).ToList();
        }

        public int GetClientCount()
        {
            return _clients.Count;
        }
    }
}
