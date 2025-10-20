namespace GameMap.Core.Models;

/// <summary>
/// Provides per-tile-type fast-access properties such as object placement capability.
/// </summary>
public static class TileProperties
{
    /// <summary>
    /// Bit flags describing tile capabilities.
    /// </summary>
    [Flags]
    public enum Flags : byte
    {
        None = 0,
        CanPlaceObject = 1 << 0
    }

    private static readonly Flags[] flagsByType;

    static TileProperties()
    {
        var max = Enum.GetValues(typeof(TileType)).Length;
        flagsByType = new Flags[max];

        flagsByType[(int)TileType.Plain] = Flags.CanPlaceObject;
        flagsByType[(int)TileType.Mountain] = Flags.None;
        flagsByType[(int)TileType.Water] = Flags.None;
    }

    /// <summary>
    /// Returns true if an object can be placed on a tile of the given type.
    /// </summary>
    public static bool CanPlaceObject(TileType type) => (flagsByType[(int)type] & Flags.CanPlaceObject) != 0;

    /// <summary>
    /// Overrides flags for the given tile type.
    /// </summary>
    public static void SetFlags(TileType type, Flags flags)
    {
        flagsByType[(int)type] = flags;
    }

    /// <summary>
    /// Gets flags for the given tile type.
    /// </summary>
    public static Flags GetFlags(TileType type) => flagsByType[(int)type];
}
