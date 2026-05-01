using Newtonsoft.Json;

namespace RemoteDeskClient.Models;

public class SignalMessage
{
    [JsonProperty("type")]
    public SignalType Type { get; set; }

    [JsonProperty("clientId")]
    public string? ClientId { get; set; }

    [JsonProperty("peerId")]
    public string? PeerId { get; set; }

    [JsonProperty("senderId")]
    public string? SenderId { get; set; }

    [JsonProperty("targetId")]
    public string? TargetId { get; set; }

    [JsonProperty("roomId")]
    public string? RoomId { get; set; }

    [JsonProperty("role")]
    public ClientMode Role { get; set; }

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
}
