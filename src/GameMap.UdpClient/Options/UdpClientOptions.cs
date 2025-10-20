namespace GameMap.UdpClient.Options;

public sealed class UdpClientOptions
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 9050;
    public string? ConnectionKey { get; set; }
    public int PollIntervalMs { get; set; } = 15;
    public int ReconnectDelayMs { get; set; } = 2000;
}