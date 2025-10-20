using GameMap.Core.Features.Surface;
using GameMap.Core.Models;
using Xunit;

namespace GameMap.Tests;

public class SurfaceLayerTests
{
    private const int Width = 10;
    private const int Height = 10;

    [Fact]
    public void Constructor_ShouldInitializeWithDefaultPlainTiles()
    {
        var layer = new SurfaceLayer(Width, Height);
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                Assert.Equal(TileType.Plain, layer.GetTile(x, y));
        Assert.Equal(Width * Height, layer.Count);
    }

    [Fact]
    public void Constructor_WithDefaultType_ShouldFillAllTiles()
    {
        var layer = new SurfaceLayer(Width, Height, TileType.Mountain);
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                Assert.Equal(TileType.Mountain, layer.GetTile(x, y));
        Assert.Equal(Width * Height, layer.Count);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-1, 10)]
    [InlineData(10, -1)]
    public void Constructor_InvalidDimensions_ShouldThrow(int w, int h)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SurfaceLayer(w, h));
    }

    [Fact]
    public void GetTile_SetTile_ShouldRoundtrip()
    {
        var layer = new SurfaceLayer(Width, Height);
        layer.SetTile(3, 4, TileType.Mountain);
        Assert.Equal(TileType.Mountain, layer.GetTile(3, 4));
        layer.SetTile(3, 4, TileType.Plain);
        Assert.Equal(TileType.Plain, layer.GetTile(3, 4));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(Width, 0)]
    [InlineData(0, Height)]
    [InlineData(Width, Height)]
    public void GetTile_OutOfBounds_ShouldThrow(int x, int y)
    {
        var layer = new SurfaceLayer(Width, Height);
        Assert.Throws<ArgumentOutOfRangeException>(() => layer.GetTile(x, y));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(Width, 0)]
    [InlineData(0, Height)]
    [InlineData(Width, Height)]
    public void SetTile_OutOfBounds_ShouldThrow(int x, int y)
    {
        var layer = new SurfaceLayer(Width, Height);
        Assert.Throws<ArgumentOutOfRangeException>(() => layer.SetTile(x, y, TileType.Mountain));
    }

    [Fact]
    public void TryGetTile_InBounds_ShouldReturnTrueAndType()
    {
        var layer = new SurfaceLayer(Width, Height);
        layer.SetTile(2, 2, TileType.Mountain);
        var ok = layer.TryGetTile(2, 2, out var t);
        Assert.True(ok);
        Assert.Equal(TileType.Mountain, t);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(Width, 0)]
    [InlineData(0, Height)]
    [InlineData(Width, Height)]
    public void TryGetTile_OutOfBounds_ShouldReturnFalseAndDefault(int x, int y)
    {
        var layer = new SurfaceLayer(Width, Height);
        var ok = layer.TryGetTile(x, y, out var t);
        Assert.False(ok);
        Assert.Equal(default, t);
    }

    [Fact]
    public void FromArray_ShouldCreateLayerWithTiles()
    {
        int w = 4, h = 3;
        var src = new TileType[w * h];
        // Fill checkerboard Mountain/Plain
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
            src[x + y * w] = (x + y) % 2 == 0 ? TileType.Mountain : TileType.Plain;

        var layer = SurfaceLayer.FromArray(w, h, src);
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
            Assert.Equal(src[x + y * w], layer.GetTile(x, y));
    }

    [Fact]
    public void FromArray_NullOrWrongLength_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => SurfaceLayer.FromArray(2, 2, null!));
        var wrong = new TileType[3];
        Assert.Throws<ArgumentException>(() => SurfaceLayer.FromArray(2, 2, wrong));
    }

    [Fact]
    public void FillArea_Normal_ShouldFill()
    {
        var layer = new SurfaceLayer(Width, Height);
        layer.FillArea(2, 3, 5, 5, TileType.Mountain);
        for (int y = 3; y <= 5; y++)
        for (int x = 2; x <= 5; x++)
            Assert.Equal(TileType.Mountain, layer.GetTile(x, y));
        // Outside remains Plain
        Assert.Equal(TileType.Plain, layer.GetTile(1, 3));
        Assert.Equal(TileType.Plain, layer.GetTile(6, 5));
    }

    [Fact]
    public void FillArea_SwappedCoordinates_ShouldFill()
    {
        var layer = new SurfaceLayer(Width, Height);
        layer.FillArea(7, 8, 4, 6, TileType.Mountain); // x1>x2 and y1>y2
        for (int y = 6; y <= 8; y++)
        for (int x = 4; x <= 7; x++)
            Assert.Equal(TileType.Mountain, layer.GetTile(x, y));
    }

    [Fact]
    public void FillArea_PartiallyOutOfBounds_ShouldClampAndFill()
    {
        var layer = new SurfaceLayer(Width, Height);
        layer.FillArea(-10, -10, 2, 1, TileType.Mountain);
        for (int y = 0; y <= 1; y++)
        for (int x = 0; x <= 2; x++)
            Assert.Equal(TileType.Mountain, layer.GetTile(x, y));
    }

    [Fact]
    public void FillArea_OutOfBounds_NoChanges()
    {
        var layer = new SurfaceLayer(Width, Height);
        layer.FillArea(-5, -5, -1, -1, TileType.Mountain); // no intersection
        // All remain Plain
        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width; x++)
            Assert.Equal(TileType.Plain, layer.GetTile(x, y));
    }

    [Fact]
    public void CanPlaceObjectsInArea_AllPlain_ShouldReturnTrue()
    {
        var layer = new SurfaceLayer(Width, Height);
        Assert.True(layer.CanPlaceObjectsInArea(0, 0, 3, 3));
    }

    [Fact]
    public void CanPlaceObjectsInArea_ContainsMountain_ShouldReturnFalse()
    {
        var layer = new SurfaceLayer(Width, Height);
        layer.SetTile(2, 2, TileType.Mountain);
        Assert.False(layer.CanPlaceObjectsInArea(0, 0, 3, 3));
        // Single cell check true/false
        Assert.False(layer.CanPlaceObjectsInArea(2, 2, 2, 2));
        Assert.True(layer.CanPlaceObjectsInArea(0, 0, 0, 0));
    }

    [Fact]
    public void CanPlaceObjectsInArea_ContainsWater_ShouldReturnFalse()
    {
        var layer = new SurfaceLayer(Width, Height);
        layer.SetTile(1, 1, TileType.Water);
        Assert.False(layer.CanPlaceObjectsInArea(0, 0, 2, 2));
        Assert.False(layer.CanPlaceObjectsInArea(1, 1, 1, 1));
    }

    [Fact]
    public void CanPlaceObjectsInArea_OutOfBounds_ShouldReturnFalse()
    {
        var layer = new SurfaceLayer(Width, Height);
        Assert.False(layer.CanPlaceObjectsInArea(-1, 0, 0, 0));
        Assert.False(layer.CanPlaceObjectsInArea(0, 0, Width, Height));
    }

    [Fact]
    public void EstimatedMemoryBytes_For1000x1000_IsUnder8Mb()
    {
        var layer = new SurfaceLayer(1000, 1000);
        var bytes = layer.EstimatedMemoryBytes();
        Assert.Equal(1_000_000, bytes);
        Assert.True(bytes < 8 * 1024 * 1024);
    }

    [Fact]
    public void Print_ShouldNotThrow()
    {
        var layer = new SurfaceLayer(2, 2);
        layer.SetTile(0, 0, TileType.Plain);
        layer.SetTile(1, 0, TileType.Mountain);
        layer.SetTile(0, 1, TileType.Water);
        layer.SetTile(1, 1, TileType.Plain);
        layer.Print();
    }
}