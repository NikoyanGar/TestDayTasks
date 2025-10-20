using MemoryPack;

namespace GameMap.Shared.Contracts;

[MemoryPackable]
public partial class CreateObjectRequest
{
    public string Id { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

[MemoryPackable]
public partial class CreateObjectResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public ObjectDto? Object { get; set; }
}

[MemoryPackable]
public partial class GetObjectRequest
{
    public string Id { get; set; } = string.Empty;
}

[MemoryPackable]
public partial class GetObjectResponse
{
    public ObjectDto? Object { get; set; }
}

[MemoryPackable]
public partial class RemoveObjectRequest
{
    public string Id { get; set; } = string.Empty;
}

[MemoryPackable]
public partial class RemoveObjectResponse
{
    public bool Success { get; set; }
}
