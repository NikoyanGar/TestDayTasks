namespace GameMap.UdpClient.Models;

internal sealed record PingResponseDto(long ServerTicksUtcMs, long RttMs);
