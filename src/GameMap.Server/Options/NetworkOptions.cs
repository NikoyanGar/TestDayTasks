namespace GameMap.Server.Options;

public sealed class NetworkOptions
{
    public int Port { get; set; } = 9050;
    public int MaxConnections { get; set; } = 128;
    public string? ConnectionKey { get; set; } = null; // set to require a key, null to accept all
    public bool IPv6Enabled { get; set; } = false;
    public bool UnconnectedMessagesEnabled { get; set; } = false;
    public int PollIntervalMs { get; set; } = 15; // LiteNetLib event pump interval
}