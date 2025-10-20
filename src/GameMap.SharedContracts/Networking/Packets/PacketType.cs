namespace GameMap.SharedContracts.Networking.Packets;

public enum PacketType : byte
{
    Unknown = 0,

    GetObjectsInAreaRequest = 1,
    GetObjectsInAreaResponse = 2,
    GetRegionsInAreaRequest = 3,
    GetRegionsInAreaResponse = 4,

    AddObjectRequest = 5,
    AddObjectResponse = 6,

    ObjectAdded = 10,
    ObjectUpdated = 11,
    ObjectDeleted = 12,

    // Renumbered to avoid collision with the requested IDs above
    Ping = 250,
    Pong = 251,
}