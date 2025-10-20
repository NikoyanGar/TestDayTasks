using System;
using GameMap.SharedContracts.Networking.Packets;
using MemoryPack;

namespace GameMap.SharedContracts.Networking;

public static class PacketSerializer
{
    // Serialize with a 1-byte packet type header
    public static byte[] Serialize<T>(PacketType type, in T value)
    {
        var payload = MemoryPackSerializer.Serialize(value);
        var result = new byte[1 + payload.Length];
        result[0] = (byte)type;
        Buffer.BlockCopy(payload, 0, result, 1, payload.Length);
        return result;
    }

    public static bool TryDeserialize(ReadOnlySpan<byte> data, out PacketType type, out object? message)
    {
        message = null;
        if (data.Length < 1)
        {
            type = PacketType.Unknown;
            return false;
        }

        type = (PacketType)data[0];
        var payload = data.Slice(1);

        switch (type)
        {
            case PacketType.GetObjectsInAreaRequest:
                message = MemoryPackSerializer.Deserialize<GetObjectsInAreaRequest>(payload);
                return true;

            case PacketType.GetObjectsInAreaResponse:
                message = MemoryPackSerializer.Deserialize<GetObjectsInAreaResponse>(payload);
                return true;

            case PacketType.GetRegionsInAreaRequest:
                message = MemoryPackSerializer.Deserialize<GetRegionsInAreaRequest>(payload);
                return true;

            case PacketType.GetRegionsInAreaResponse:
                message = MemoryPackSerializer.Deserialize<GetRegionsInAreaResponse>(payload);
                return true;

            case PacketType.AddObjectRequest:
                message = MemoryPackSerializer.Deserialize<AddObjectRequest>(payload);
                return true;

            case PacketType.AddObjectResponse:
                message = MemoryPackSerializer.Deserialize<AddObjectResponse>(payload);
                return true;

            case PacketType.ObjectAdded:
            case PacketType.ObjectUpdated:
            case PacketType.ObjectDeleted:
                message = MemoryPackSerializer.Deserialize<ObjectEventMessage>(payload);
                return true;

            case PacketType.Ping:
                message = MemoryPackSerializer.Deserialize<PingPacket>(payload);
                return true;

            case PacketType.Pong:
                message = MemoryPackSerializer.Deserialize<PongPacket>(payload);
                return true;

            default:
                return false;
        }
    }
}