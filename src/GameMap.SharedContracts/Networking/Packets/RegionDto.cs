using MemoryPack;

namespace GameMap.SharedContracts.Networking.Packets;

[MemoryPackable]
public partial class RegionDto
{
    public ushort Id { get; set; }
    public string Name { get; set; } = string.Empty;
}