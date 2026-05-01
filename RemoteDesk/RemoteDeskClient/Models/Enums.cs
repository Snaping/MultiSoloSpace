namespace RemoteDeskClient.Models;

public enum ClientMode
{
    None,
    Controller,
    Controlled
}

public enum ConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Disconnecting
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

public class IceServerInfo
{
    public string[] Urls { get; set; } = Array.Empty<string>();
    public string? Username { get; set; }
    public string? Credential { get; set; }
}

public class ControlMessage
{
    public string Type { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
    public int Button { get; set; }
    public string? Text { get; set; }
    public int KeyCode { get; set; }
    public bool IsDown { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}
