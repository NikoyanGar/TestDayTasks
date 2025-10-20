namespace GameMap.UdpClient.Models;

internal sealed record ConnectRequest(string Host, int Port, string? Key);
