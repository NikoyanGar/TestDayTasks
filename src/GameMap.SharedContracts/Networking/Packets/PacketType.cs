namespace GameMap.SharedContracts.Networking.Packets;

public enum PacketType : byte
{
    Unknown = 0,
    Ping = 1,
    Pong = 2,
}