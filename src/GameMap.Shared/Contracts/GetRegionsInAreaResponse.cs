using MemoryPack;

namespace GameMap.Shared.Contracts;

[MemoryPackable]
public partial class GetRegionsInAreaResponse
{
    public RegionDto[] Regions { get; set; } = System.Array.Empty<RegionDto>();
}
