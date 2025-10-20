namespace GameMap.UdpClient.Models;

public sealed record ConnectRequest(string Host, int Port, string? Key);
