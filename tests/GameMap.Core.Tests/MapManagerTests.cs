using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using Xunit;
using GameMap.Core;
using GameMap.Core.Layers.Objects;
using GameMap.Core.Layers.Surface;
using GameMap.Core.Layers.Regions;
using GameMap.Core.Models;
using GameMap.Core.Storage;
using GameMap.Core.Converters;

namespace GameMap.Core.Tests;

public class MapManagerTests
{
    // Default SUT (some tests will create additional instances with different dimensions)
    private readonly SurfaceLayer _surface;
    private readonly Mock<IGeoDb> _geoDbMock;
    private readonly Mock<ICoordinateConverter> _converterMock;
    private readonly ObjectLayer _objects;
    private readonly RegionLayer _regions;
    private readonly MapManager _manager;

    public MapManagerTests()
    {
        _surface = new SurfaceLayer(6, 6, TileType.Plain);
        _geoDbMock = CreateGeoDbMock();
        _converterMock = CreateIdentityConverterMock();
        _objects = new ObjectLayer(_geoDbMock.Object, _surface, _converterMock.Object);
        _regions = new RegionLayer(6, 6, 1);
        _manager = new MapManager(_surface, _objects, _regions);
    }

    private static (MapManager manager, SurfaceLayer surface, ObjectLayer objects, RegionLayer regions) NewManager(int w, int h, int regionCount)
    {
        var surface = new SurfaceLayer(w, h, TileType.Plain);
        var dbMock = CreateGeoDbMock();
        var convMock = CreateIdentityConverterMock();
        var objects = new ObjectLayer(dbMock.Object, surface, convMock.Object);
        var regions = new RegionLayer(w, h, regionCount);
        var manager = new MapManager(surface, objects, regions);
        return (manager, surface, objects, regions);
    }

    private static Mock<IGeoDb> CreateGeoDbMock()
    {
        var db = new Mock<IGeoDb>(MockBehavior.Strict);

        var geo = new Dictionary<string, Dictionary<string, (double lon, double lat)>>();
        var kv = new Dictionary<string, string>();

        db.Setup(d => d.GeoAdd(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<string>()))
          .Callback<string, double, double, string>((key, lon, lat, member) =>
          {
              if (!geo.TryGetValue(key, out var set))
              {
                  set = new Dictionary<string, (double lon, double lat)>();
                  geo[key] = set;
              }
              set[member] = (lon, lat);
          });

        db.Setup(d => d.GeoRadius(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<int>()))
          .Returns<string, double, double, int>((key, lon, lat, radiusKm) =>
          {
              if (!geo.TryGetValue(key, out var set)) return Array.Empty<string>();
              double r = radiusKm;
              return set
                  .Where(kvp =>
                  {
                      var (gx, gy) = kvp.Value;
                      var dx = gx - lon;
                      var dy = gy - lat;
                      return Math.Sqrt(dx * dx + dy * dy) <= r;
                  })
                  .Select(kvp => kvp.Key)
                  .ToArray();
          });

        db.Setup(d => d.StringSet(It.IsAny<string>(), It.IsAny<string>()))
          .Callback<string, string>((key, val) => kv[key] = val);

        db.Setup(d => d.StringGet(It.IsAny<string>()))
          .Returns<string>(key => kv.TryGetValue(key, out var v) ? v : null);

        db.Setup(d => d.KeyDelete(It.IsAny<string>()))
          .Callback<string>(key => kv.Remove(key));

        db.Setup(d => d.SortedSetRemove(It.IsAny<string>(), It.IsAny<string>()))
          .Returns<string, string>((key, member) =>
          {
              if (!geo.TryGetValue(key, out var set)) return false;
              return set.Remove(member);
          });

        return db;
    }

    private static Mock<ICoordinateConverter> CreateIdentityConverterMock()
    {
        var conv = new Mock<ICoordinateConverter>(MockBehavior.Strict);
        conv.Setup(c => c.ToRedisCoordinates(It.IsAny<int>(), It.IsAny<int>()))
            .Returns<int, int>((x, y) => (x, y));
        conv.Setup(c => c.FromRedisCoordinates(It.IsAny<double>(), It.IsAny<double>()))
            .Returns<double, double>((lon, lat) => ((int)Math.Round(lon), (int)Math.Round(lat)));
        return conv;
    }

    [Fact]
    public void TryPlaceObject_Fails_WhenSurfaceBlocked()
    {
        var (manager, surface, _, _) = NewManager(5, 5, 1);
        surface.SetTile(1, 1, TileType.Mountain);

        var obj = new MapObject("o1", 0, 0, 2, 2);
        var placed = manager.TryPlaceObject(obj);

        Assert.False(placed);
    }

    [Fact]
    public void TryPlaceObject_WhenOccupyTile_FillsSurface()
    {
        var (manager, surface, _, _) = NewManager(5, 5, 1);
        var obj = new MapObject("o1", 1, 1, 2, 2);

        var ok = manager.TryPlaceObject(obj, TileType.Mountain);
        Assert.True(ok);

        Assert.Equal(TileType.Mountain, surface.GetTile(1, 1));
        Assert.Equal(TileType.Mountain, surface.GetTile(2, 2));
    }

    [Fact]
    public void CanPlaceObjectInArea_ConsidersSurfaceAndObjects()
    {
        var (manager, surface, _, _) = NewManager(5, 5, 1);
        surface.SetTile(3, 3, TileType.Mountain);
        Assert.False(manager.CanPlaceObjectInArea(3, 3, 3, 3));

        var (manager2, _, _, _) = NewManager(5, 5, 1);
        Assert.True(manager2.CanPlaceObjectInArea(0, 0, 2, 2));
        manager2.TryPlaceObject(new MapObject("a", 1, 1, 2, 2));
        Assert.False(manager2.CanPlaceObjectInArea(0, 0, 2, 2));
    }

    [Fact]
    public void GetObjectsInArea_ForwardsToObjectLayer()
    {
        var (manager, _, _, _) = NewManager(5, 5, 1);
        manager.TryPlaceObject(new MapObject("a", 0, 0, 2, 2));
        manager.TryPlaceObject(new MapObject("b", 3, 3, 2, 2));

        var found = manager.GetObjectsInArea(0, 0, 2, 2).Select(o => o.Id).ToHashSet();
        Assert.Contains("a", found);
        Assert.DoesNotContain("b", found);
    }

    [Fact]
    public void GetRegionsInArea_ForwardsToRegionLayer()
    {
        var (manager, _, _, _) = NewManager(8, 6, 4); // 2x2 grid
        var list = manager.GetRegionsInArea(2, 1, 5, 4);
        Assert.True(list.Count >= 1);
        Assert.All(list, r => Assert.InRange(r.Id, (ushort)1, (ushort)4));
    }

    [Fact]
    public void Events_AreForwarded_FromObjectLayer()
    {
        var (manager, _, objects, _) = NewManager(5, 5, 1);

        MapObject? created = null;
        MapObject? updated = null;
        string? deleted = null;

        manager.ObjectCreated += o => created = o;
        manager.ObjectUpdated += o => updated = o;
        manager.ObjectDeleted += id => deleted = id;

        var obj = new MapObject("o1", 1, 1, 2, 2);
        Assert.True(manager.TryPlaceObject(obj));

        objects.UpdateObject(new MapObject("o1", 2, 2, 2, 2));
        objects.RemoveObject("o1");

        Assert.Equal("o1", created?.Id);
        Assert.Equal(2, updated?.X);
        Assert.Equal("o1", deleted);
    }
}