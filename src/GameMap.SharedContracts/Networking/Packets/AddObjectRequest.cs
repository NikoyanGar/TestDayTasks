using MemoryPack;

namespace GameMap.SharedContracts.Networking.Packets;

[MemoryPackable]
public partial class AddObjectRequest
{
    public string Id { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}