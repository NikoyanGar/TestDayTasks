using MemoryPack;

namespace GameMap.SharedContracts.Networking.Packets;

[MemoryPackable]
public partial record struct PingPacket(long ClientTicksUtcMs);

[MemoryPackable]
public partial record struct PongPacket(long ServerTicksUtcMs);