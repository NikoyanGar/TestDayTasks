namespace GameMap.Core.Models;

/// <summary>
/// Represents a rectangular object placed on the tile map.
/// </summary>
public class MapObject
{
    /// <summary>
    /// Unique identifier of the object.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// X coordinate of the top-left corner in map tile coordinates.
    /// </summary>
    public int X { get; }

    /// <summary>
    /// Y coordinate of the top-left corner in map tile coordinates.
    /// </summary>
    public int Y { get; }

    /// <summary>
    /// Width of the object in tiles.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Height of the object in tiles.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MapObject"/> class.
    /// </summary>
    /// <param name="id">Unique identifier of the object.</param>
    /// <param name="x">X coordinate of the top-left corner (tiles).</param>
    /// <param name="y">Y coordinate of the top-left corner (tiles).</param>
    /// <param name="width">Width in tiles.</param>
    /// <param name="height">Height in tiles.</param>
    public MapObject(string id, int x, int y, int width, int height)
    {
        Id = id;
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }
}