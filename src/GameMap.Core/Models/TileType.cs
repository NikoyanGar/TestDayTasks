namespace GameMap.Core.Models;

/// <summary>
/// Surface tile types on the map.
/// </summary>
public enum TileType : byte 
{
    /// <summary>
    /// Flat land; can place objects by default.
    /// </summary>
    Plain = 0,

    /// <summary>
    /// Mountain; cannot place objects by default.
    /// </summary>
    Mountain = 1,

    /// <summary>
    /// Water; cannot place objects by default.
    /// </summary>
    Water = 2
}
