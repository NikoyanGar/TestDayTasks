using GameMap.Core.Layers.Surface;
using GameMap.Core.Models;

namespace GameMap.Core.Tests;

public class SurfaceLayerTests
{
    [Fact]
    public void Create_1000x1000_Default_Plain_Memory_Ok()
    {
        var layer = new SurfaceLayer(1000, 1000, TileType.Plain);
        Assert.Equal(1000, layer.Width);
        Assert.Equal(1000, layer.Height);
        Assert.Equal(1_000_000, layer.EstimatedMemoryBytes()); // 1 byte per tile
        Assert.True(layer.EstimatedMemoryBytes() <= 8 * 1024 * 1024);
        Assert.Equal(TileType.Plain, layer.GetTile(0, 0));
    }

    [Fact]
    public void Get_Set_Tile_Ok_And_OOB_Throws()
    {
        var layer = new SurfaceLayer(10, 10);

        layer.SetTile(3, 4, TileType.Mountain);
        Assert.Equal(TileType.Mountain, layer.GetTile(3, 4));

        Assert.Throws<ArgumentOutOfRangeException>(() => layer.GetTile(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => layer.SetTile(10, 9, TileType.Plain));

        Assert.False(layer.TryGetTile(10, 10, out _));
        Assert.True(layer.TryGetTile(0, 0, out var t0));
        Assert.Equal(TileType.Plain, t0);
    }

    [Fact]
    public void FillArea_Works_And_Clamps_OutOfBounds()
    {
        var layer = new SurfaceLayer(5, 5);
        layer.FillArea(-10, -10, 1, 1, TileType.Mountain);
        for (int y = 0; y <= 1; y++)
        for (int x = 0; x <= 1; x++)
            Assert.Equal(TileType.Mountain, layer.GetTile(x, y));

        // The rest should remain plain
        Assert.Equal(TileType.Plain, layer.GetTile(2, 2));
    }

    [Fact]
    public void CanPlaceObjectsInArea_True_On_Plain_False_On_Mountain_And_OOB()
    {
        var layer = new SurfaceLayer(10, 10);
        // Make a 3x3 mountain block at (2..4,2..4)
        layer.FillArea(2, 2, 4, 4, TileType.Mountain);

        // Fully inside plains
        Assert.True(layer.CanPlaceObjectsInArea(0, 0, 1, 1));

        // Intersects mountain
        Assert.False(layer.CanPlaceObjectsInArea(3, 3, 5, 5));

        // Out of bounds
        Assert.False(layer.CanPlaceObjectsInArea(-1, 0, 1, 1));
        Assert.False(layer.CanPlaceObjectsInArea(0, 0, 10, 10));
    }

    [Fact]
    public void FromArray_Constructs_Layer_Correctly()
    {
        int w = 4, h = 3;
        var src = new TileType[w * h];
        for (int i = 0; i < src.Length; i++)
            src[i] = (i % 2 == 0) ? TileType.Plain : TileType.Mountain;

        var layer = SurfaceLayer.FromArray(w, h, src);
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            int idx = x + y * w;
            Assert.Equal(src[idx], layer.GetTile(x, y));
        }
    }
}