using GameMap.Core.Models;

namespace GameMap.Core.Features.Surface;

/// <summary>
/// Abstraction for a surface layer that stores tile types and supports fast queries/updates.
/// </summary>
public interface ISurfaceLayer
{
    int Width { get; }
    int Height { get; }

    ///// <summary>
    ///// Creates a new layer from a flat array of tile types sized width*height.
    ///// </summary>
    ///// <param name="width">Map width in tiles.</param>
    ///// <param name="height">Map height in tiles.</param>
    ///// <param name="source">Flat array of tile types length equals width*height.</param>
    //static abstract SurfaceLayer FromArray(int width, int height, TileType[] source);

    /// <summary>
    /// Checks if all tiles in the specified inclusive rectangle allow placing objects.
    /// </summary>
    bool CanPlaceObjectsInArea(int x1, int y1, int x2, int y2);

    /// <summary>
    /// Returns estimated memory used by the layer in bytes.
    /// </summary>
    long EstimatedMemoryBytes();

    /// <summary>
    /// Fills a rectangle area (inclusive) with the specified tile type. Out-of-bounds portions are ignored.
    /// </summary>
    void FillArea(int x1, int y1, int x2, int y2, TileType type);

    /// <summary>
    /// Returns tile type at the specified coordinates; throws if out of bounds.
    /// </summary>
    TileType GetTile(int x, int y);

    /// <summary>
    /// Prints the layer to the console for debugging.
    /// </summary>
    void Print();

    /// <summary>
    /// Sets tile type at the specified coordinates; throws if out of bounds.
    /// </summary>
    void SetTile(int x, int y, TileType type);

    /// <summary>
    /// Attempts to read tile type at coordinates; returns false if out of bounds.
    /// </summary>
    bool TryGetTile(int x, int y, out TileType type);
}