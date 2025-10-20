using System.ComponentModel.DataAnnotations;
using GameMap.Core.Models;

namespace GameMap.Server.Options;

public sealed class MapOptions
{
    [Range(1, 10_000)]
    public int Width { get; set; } = 100;

    [Range(1, 10_000)]
    public int Height { get; set; } = 100;

    // Number of equal divisions per side (e.g., 10 => 10x10 grid)
    [Range(1, 1_000)]
    public int RegionGridDivisions { get; set; } = 10;

    public TileType DefaultTile { get; set; } = TileType.Plain;

    public bool ShowMapOnStart { get; set; } = true;

    public List<AreaFill>? Fills { get; set; } = new()
    {
        new AreaFill { X1 = 1, Y1 = 1, X2 = 10, Y2 = 10, Tile = TileType.Mountain }
    };

    public List<ObjectPlacement>? Objects { get; set; } = new()
    {
        new ObjectPlacement { Id = "house-1", X = 12, Y = 20, Width = 3, Height = 2, OccupyTile = TileType.Mountain }
    };
}

public sealed class AreaFill
{
    public int X1 { get; set; }
    public int Y1 { get; set; }
    public int X2 { get; set; }
    public int Y2 { get; set; }
    public TileType Tile { get; set; } = TileType.Plain;
}

public sealed class ObjectPlacement
{
    [Required]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [Range(0, int.MaxValue)]
    public int X { get; set; }

    [Range(0, int.MaxValue)]
    public int Y { get; set; }

    [Range(1, int.MaxValue)]
    public int Width { get; set; } = 1;

    [Range(1, int.MaxValue)]
    public int Height { get; set; } = 1;

    // Optional: if set, the placed area will be filled with this tile
    public TileType? OccupyTile { get; set; }
}