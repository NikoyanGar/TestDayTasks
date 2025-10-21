using System;
using System.Linq;
using Xunit;
using GameMap.Core.Layers.Regions;

namespace GameMap.Core.Tests;

public class RegionLayerTests
{
    [Fact]
    public void Constructor_Throws_WhenDimensionsNotDivisibleForEqualAreas()
    {
        // 10x10 map with 3 regions cannot be split into equal rectangles
        Assert.Throws<ArgumentException>(() => new RegionLayer(10, 10, 3));
    }

    [Fact]
    public void Constructor_Succeeds_ForBalancedGrid()
    {
        var layer = new RegionLayer(8, 6, 4); // 2x2 grid is expected
        Assert.Equal(8, layer.Width);
        Assert.Equal(6, layer.Height);
        // Should not throw and create region ids in [1..4]
        var ids = new ushort[] { layer.GetRegionId(0,0), layer.GetRegionId(7,5) };
        Assert.All(ids, id => Assert.InRange(id, (ushort)1, (ushort)4));
    }

    [Fact]
    public void Generate_EqualArea_10x10_On_1000x1000()
    {
        int w = 1000, h = 1000, regions = 100;
        var layer = new RegionLayer(w, h, regions);

        // Ensure every tile has a non-zero region id and count areas per region
        var counts = new Dictionary<ushort, int>();
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            var id = layer.GetRegionId(x, y);
            Assert.True(id > 0);
            counts[id] = counts.TryGetValue(id, out var c) ? c + 1 : 1;
        }

        Assert.Equal(regions, counts.Count);

        int expectedArea = (w * h) / regions; // 1,000,000 / 100 = 10,000
        foreach (var kv in counts)
            Assert.Equal(expectedArea, kv.Value);
    }

    [Fact]
    public void GetRegionById_And_IsTileInRegion_Work()
    {
        var layer = new RegionLayer(1000, 1000, 100);
        var id = layer.GetRegionId(5, 5);
        var region = layer.GetRegionById(id);
        Assert.Equal(id, region.Id);
        Assert.True(layer.IsTileInRegion(5, 5, id));
    }

    [Fact]
    public void GetRegionsInArea_Returns_Distinct_Intersecting_Regions()
    {
        var layer = new RegionLayer(1000, 1000, 100); // 10x10 grid, each region 100x100

        // Area spanning across 2x2 regions: e.g., rectangle around (95..105, 95..105)
        var regions = layer.GetRegionsInArea(95, 95, 20, 20).ToList();
        Assert.InRange(regions.Count, 4, 4); // exactly 4 regions
    }

    [Fact]
    public void GetRegionId_OOB_Throws()
    {
        var layer = new RegionLayer(1000, 1000, 100);
        Assert.Throws<ArgumentOutOfRangeException>(() => layer.GetRegionId(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => layer.GetRegionId(1000, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => layer.GetRegionId(0, 1000));
    }

    [Fact]
    public void GetRegionsInArea_EmptyForNonPositiveDims()
    {
        var layer = new RegionLayer(8, 6, 4);
        Assert.Empty(layer.GetRegionsInArea(0, 0, 0, 1));
        Assert.Empty(layer.GetRegionsInArea(0, 0, 1, 0));
    }

    [Fact]
    public void GetRegionsInArea_ClampsAndReturnsUnique()
    {
        var layer = new RegionLayer(8, 6, 4); // 2x2
        // Span across center to intersect all 4 regions
        var regions = layer.GetRegionsInArea(2, 1, 6, 5).ToList();
        Assert.Equal(4, regions.Count);
        Assert.Equal(4, regions.Select(r => r.Id).Distinct().Count());
    }

    [Fact]
    public void GetRegionById_ReturnsRegion()
    {
        var layer = new RegionLayer(8, 6, 4);
        var id = layer.GetRegionId(0, 0);
        var region = layer.GetRegionById(id);
        Assert.Equal(id, region.Id);
        Assert.StartsWith("Region_", region.Name);
    }

    [Fact]
    public void IsTileInRegion_Works()
    {
        var layer = new RegionLayer(8, 6, 4);
        var id = layer.GetRegionId(5, 4);
        Assert.True(layer.IsTileInRegion(5, 4, id));
        Assert.False(layer.IsTileInRegion(0, 0, id));
    }

    [Fact]
    public void GetRegionId_IsConsistentWithinBlock()
    {
        var layer = new RegionLayer(8, 6, 4); // 2x2 -> region width 4, height 3
        var id1 = layer.GetRegionId(0, 0);
        var id2 = layer.GetRegionId(3, 2); // still top-left block
        Assert.Equal(id1, id2);

        var id3 = layer.GetRegionId(4, 0); // top-right block
        Assert.NotEqual(id1, id3);
    }
}