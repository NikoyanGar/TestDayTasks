using MemoryPack;

namespace GameMap.Shared.Contracts;

[MemoryPackable]
public partial class GetObjectsInAreaResponse
{
    public ObjectDto[] Objects { get; set; } = System.Array.Empty<ObjectDto>();
}
