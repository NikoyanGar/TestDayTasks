using System.Collections.Generic;
using MemoryPack;

namespace GameMap.SharedContracts.Networking.Packets;

[MemoryPackable]
public partial class GetRegionsInAreaResponse
{
    public List<RegionDto> Regions { get; set; } = new();
}