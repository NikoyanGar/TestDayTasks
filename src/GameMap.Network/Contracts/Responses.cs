using MemoryPack;

namespace GameMap.Network.Contracts;

[MemoryPackable]
public partial record ObjectDto(string Id, int X, int Y, int Width, int Height);

[MemoryPackable]
public partial record GetObjectsInAreaResponse(ObjectDto[] Objects);

[MemoryPackable]
public partial record RegionDto(uint Id, string Name);

[MemoryPackable]
public partial record GetRegionsInAreaResponse(RegionDto[] Regions);
