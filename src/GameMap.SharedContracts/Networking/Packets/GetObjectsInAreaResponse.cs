using System.Collections.Generic;
using MemoryPack;

namespace GameMap.SharedContracts.Networking.Packets;

[MemoryPackable]
public partial class GetObjectsInAreaResponse
{
    public List<GameObjectDto> Objects { get; set; } = new();
}