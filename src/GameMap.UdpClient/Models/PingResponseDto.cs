namespace GameMap.UdpClient.Models;

public sealed record PingResponseDto(long ServerTicksUtcMs, long RttMs);
