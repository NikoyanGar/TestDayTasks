using GameMap.Core.Features.Regions;

namespace GameMap.Core.Tests;

public class RegionLayerTests
{
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
}