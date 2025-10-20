using MemoryPack;

namespace GameMap.Shared.Contracts;

[MemoryPackable]
public partial class RegionDto
{
    public uint Id { get; set; }
    public int X1 { get; set; }
    public int Y1 { get; set; }
    public int X2 { get; set; }
    public int Y2 { get; set; }
}
