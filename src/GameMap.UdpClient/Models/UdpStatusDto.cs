namespace GameMap.UdpClient.Models;

public sealed record UdpStatusDto(bool Connected, string? RemoteEndPoint);
