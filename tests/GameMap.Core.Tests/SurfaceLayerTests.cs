using System;
using Xunit;
using GameMap.Core.Layers.Surface;
using GameMap.Core.Models;

namespace GameMap.Core.Tests;

public class SurfaceLayerTests
{
    [Fact]
    public void Constructor_Initializes_DefaultPlain()
    {
        var layer = new SurfaceLayer(3, 2);
        Assert.Equal(3, layer.Width);
        Assert.Equal(2, layer.Height);
        Assert.Equal(6, layer.Count);

        for (int y = 0; y < layer.Height; y++)
            for (int x = 0; x < layer.Width; x++)
                Assert.Equal(TileType.Plain, layer.GetTile(x, y));
    }

    [Fact]
    public void FromArray_CreatesLayer_WithExactTiles()
    {
        var src = new[] {
            TileType.Plain, TileType.Mountain, TileType.Water,
            TileType.Water, TileType.Mountain, TileType.Plain
        };
        var layer = SurfaceLayer.FromArray(3, 2, src);

        Assert.Equal(TileType.Plain, layer.GetTile(0,0));
        Assert.Equal(TileType.Mountain, layer.GetTile(1,0));
        Assert.Equal(TileType.Water, layer.GetTile(2,0));
        Assert.Equal(TileType.Water, layer.GetTile(0,1));
        Assert.Equal(TileType.Mountain, layer.GetTile(1,1));
        Assert.Equal(TileType.Plain, layer.GetTile(2,1));
    }

    [Fact]
    public void GetSetTile_Works()
    {
        var layer = new SurfaceLayer(2, 2);
        layer.SetTile(1, 1, TileType.Mountain);
        Assert.Equal(TileType.Mountain, layer.GetTile(1,1));
    }

    [Fact]
    public void TryGetTile_OutOfBounds_ReturnsFalse()
    {
        var layer = new SurfaceLayer(2, 2);
        var ok = layer.TryGetTile(5, 5, out var type);
        Assert.False(ok);
        Assert.Equal(default, type);
    }

    [Fact]
    public void TryGetTile_InBounds_ReturnsTrue()
    {
        var layer = new SurfaceLayer(2, 2);
        var ok = layer.TryGetTile(1, 1, out var type);
        Assert.True(ok);
        Assert.Equal(TileType.Plain, type);
    }

    [Fact]
    public void FillArea_ClampsAndFills_Inclusive()
    {
        var layer = new SurfaceLayer(4, 3);
        layer.FillArea(-1, -1, 2, 1, TileType.Mountain);

        // Filled area should be [0..2]x[0..1]
        for (int y = 0; y <= 1; y++)
        {
            for (int x = 0; x <= 2; x++)
                Assert.Equal(TileType.Mountain, layer.GetTile(x, y));
        }
        // Outside remains Plain
        Assert.Equal(TileType.Plain, layer.GetTile(3, 0));
        Assert.Equal(TileType.Plain, layer.GetTile(3, 2));
    }

    [Fact]
    public void CanPlaceObjectsInArea_OutOfBounds_ReturnsFalse()
    {
        var layer = new SurfaceLayer(4, 3);
        Assert.False(layer.CanPlaceObjectsInArea(-1, 0, 1, 1));
        Assert.False(layer.CanPlaceObjectsInArea(0, -1, 1, 1));
        Assert.False(layer.CanPlaceObjectsInArea(0, 0, 4, 1));
        Assert.False(layer.CanPlaceObjectsInArea(0, 0, 1, 3));
    }

    [Fact]
    public void CanPlaceObjectsInArea_AllPlain_ReturnsTrue()
    {
        var layer = new SurfaceLayer(4, 3);
        Assert.True(layer.CanPlaceObjectsInArea(0, 0, 3, 2));
    }

    [Fact]
    public void CanPlaceObjectsInArea_ContainsBlockedTile_ReturnsFalse()
    {
        var layer = new SurfaceLayer(4, 3);
        layer.SetTile(2, 1, TileType.Mountain);
        Assert.False(layer.CanPlaceObjectsInArea(1, 1, 3, 2));
    }

    [Fact]
    public void Print_WritesExpectedSymbols()
    {
        var layer = new SurfaceLayer(3, 2);
        layer.SetTile(1, 0, TileType.Mountain);
        layer.SetTile(2, 1, TileType.Water);

        using var sw = new System.IO.StringWriter();
        Console.SetOut(sw);

        layer.Print();

        var expected =
            ".^." + Environment.NewLine +
            "..~" + Environment.NewLine;

        Assert.Equal(expected, sw.ToString());
    }

    [Fact]
    public void EstimatedMemoryBytes_EqualsTileCount()
    {
        var layer = new SurfaceLayer(5, 4);
        Assert.Equal(20, layer.EstimatedMemoryBytes());
    }
}