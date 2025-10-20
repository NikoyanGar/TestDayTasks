using MemoryPack;

namespace GameMap.Network.Contracts;

[MemoryPackable]
public partial record GetObjectsInAreaRequest(int X1, int Y1, int X2, int Y2);

[MemoryPackable]
public partial record GetRegionsInAreaRequest(int X1, int Y1, int X2, int Y2);
