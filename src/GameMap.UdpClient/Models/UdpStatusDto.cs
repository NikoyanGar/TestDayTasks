namespace GameMap.UdpClient.Models;

internal sealed record UdpStatusDto(bool Connected, string? RemoteEndPoint);
