namespace GameMap.Core.Models;

public static class TileProperties
{
    public static bool CanPlaceObject(TileType type) => type switch
    {
        TileType.Plain => true,
        TileType.Mountain => false,
        TileType.Water => false,
        _ => false
    };
}
