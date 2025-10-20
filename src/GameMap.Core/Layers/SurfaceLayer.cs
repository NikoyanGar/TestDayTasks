using System.Runtime.CompilerServices;
using GameMap.Core.Models;

namespace GameMap.Core.Layers;

/// <summary>
/// Dense and cache-friendly implementation of a surface layer that stores tile types as bytes.
/// Optimized for O(1) access and minimal allocations.
/// </summary>
public sealed class SurfaceLayer : ISurfaceLayer
{
    private readonly byte[] tiles;

    /// <summary>
    /// Map width in tiles.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Map height in tiles.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Total number of tiles. Overflow-safe check is performed in the constructor.
    /// </summary>
    public int Count => checked(Width * Height);

    /// <summary>
    /// Creates a new surface layer with the given dimensions and default tile type.
    /// </summary>
    /// <param name="width">Map width in tiles.</param>
    /// <param name="height">Map height in tiles.</param>
    /// <param name="defaultType">Default tile type to fill the layer with.</param>
    public SurfaceLayer(int width, int height, TileType defaultType = TileType.Plain)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

        Width = width;
        Height = height;
        tiles = new byte[checked(width * height)];

        if (defaultType != TileType.Plain)
        {
            byte b = (byte)defaultType;
            for (int i = 0; i < tiles.Length; i++) tiles[i] = b;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Index(int x, int y)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height)
            throw new ArgumentOutOfRangeException($"Coordinates out of bounds: ({x},{y})");
        return x + y * Width;
    }

    /// <summary>
    /// Returns tile type at the given coordinates. Throws if out of bounds.
    /// </summary>
    public TileType GetTile(int x, int y) => (TileType)tiles[Index(x, y)];

    /// <summary>
    /// Attempts to read a tile type at coordinates. Returns false if out of bounds.
    /// </summary>
    public bool TryGetTile(int x, int y, out TileType type)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height)
        {
            type = default;
            return false;
        }
        type = (TileType)tiles[x + y * Width];
        return true;
    }

    /// <summary>
    /// Sets the tile type at coordinates. Throws if out of bounds.
    /// </summary>
    public void SetTile(int x, int y, TileType type) => tiles[Index(x, y)] = (byte)type;

    /// <summary>
    /// Creates a new layer with the specified tiles copied from a flat array of length width*height.
    /// </summary>
    public static SurfaceLayer FromArray(int width, int height, TileType[] source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (source.Length != width * height) throw new ArgumentException("Source array length does not match map size");

        var layer = new SurfaceLayer(width, height);
        Buffer.BlockCopy(source, 0, layer.tiles, 0, source.Length);
        return layer;
    }

    /// <summary>
    /// Fills an inclusive rectangle with the given tile type. Coordinates are clamped to the map bounds; non-intersecting rectangles are ignored.
    /// </summary>
    public void FillArea(int x1, int y1, int x2, int y2, TileType type)
    {
        if (x1 > x2) (x1, x2) = (x2, x1);
        if (y1 > y2) (y1, y2) = (y2, y1);

        if (x2 < 0 || y2 < 0 || x1 >= Width || y1 >= Height) return;

        x1 = Math.Max(x1, 0);
        y1 = Math.Max(y1, 0);
        x2 = Math.Min(x2, Width - 1);
        y2 = Math.Min(y2, Height - 1);

        byte b = (byte)type;

        for (int y = y1; y <= y2; y++)
        {
            int rowStart = y * Width + x1;
            int len = x2 - x1 + 1;
            for (int i = 0; i < len; i++)
                tiles[rowStart + i] = b;
        }
    }

    /// <summary>
    /// Returns true if all tiles within the inclusive rectangle allow placing objects; false otherwise. Returns false for out-of-bounds rectangles.
    /// </summary>
    public bool CanPlaceObjectsInArea(int x1, int y1, int x2, int y2)
    {
        if (x1 > x2) (x1, x2) = (x2, x1);
        if (y1 > y2) (y1, y2) = (y2, y1);

        if (x1 < 0 || y1 < 0 || x2 >= Width || y2 >= Height) return false;

        for (int y = y1; y <= y2; y++)
        {
            int rowStart = y * Width + x1;
            int len = x2 - x1 + 1;
            for (int i = 0; i < len; i++)
            {
                var t = (TileType)tiles[rowStart + i];
                if (!TileProperties.CanPlaceObject(t)) return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Writes the layer to the console using symbols: '.' for Plain, '^' for Mountain, '~' for Water, '?' for unknown.
    /// </summary>
    public void Print()
    {
        for (int y = 0; y < Height; y++)
        {
            int rowStart = y * Width;
            for (int x = 0; x < Width; x++)
            {
                char symbol = tiles[rowStart + x] switch
                {
                    (byte)TileType.Plain => '.',
                    (byte)TileType.Mountain => '^',
                    (byte)TileType.Water => '~',
                    _ => '?'
                };
                Console.Write(symbol);
            }
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Returns approximate memory used by the layer in bytes.
    /// </summary>
    public long EstimatedMemoryBytes() => tiles.Length;
}