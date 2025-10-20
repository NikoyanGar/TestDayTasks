using MemoryPack;

namespace GameMap.SharedContracts.Networking.Packets;

[MemoryPackable]
public partial class AddObjectResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
}