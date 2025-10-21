using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using Xunit;
using GameMap.Core.Layers.Objects;
using GameMap.Core.Layers.Surface;
using GameMap.Core.Models;
using GameMap.Core.Storage;
using GameMap.Core.Converters;

namespace GameMap.Core.Tests;

public class ObjectLayerTests
{
    private readonly SurfaceLayer _surface;
    private readonly Mock<IGeoDb> _geoDbMock;
    private readonly Mock<ICoordinateConverter> _converterMock;
    private readonly ObjectLayer _layer;

    public ObjectLayerTests()
    {
        _surface = new SurfaceLayer(20, 20, TileType.Plain);
        _geoDbMock = CreateGeoDbMock();
        _converterMock = CreateIdentityConverterMock();
        _layer = new ObjectLayer(_geoDbMock.Object, _surface, _converterMock.Object);
    }

    private static Mock<IGeoDb> CreateGeoDbMock()
    {
        var db = new Mock<IGeoDb>(MockBehavior.Strict);

        // Backing stores to simulate Redis behavior for tests
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
    public void CanPlaceObject_RespectsBoundsAndSurface()
    {
        var okObj = new MapObject("a", 0, 0, 3, 3);
        Assert.True(_layer.CanPlaceObject(okObj));

        _surface.SetTile(1, 1, TileType.Mountain);
        Assert.False(_layer.CanPlaceObject(okObj));

        var outOfBounds = new MapObject("b", 18, 18, 3, 3); // Out of 20x20
        Assert.False(_layer.CanPlaceObject(outOfBounds));
    }

    [Fact]
    public void AddObject_PersistsAndRaisesEvent()
    {
        var obj = new MapObject("id1", 2, 2, 2, 2);

        MapObject? raised = null;
        _layer.ObjectCreated += o => raised = o;

        _layer.AddObject(obj);

        // Verify storage interactions and event
        _geoDbMock.Verify(d => d.StringSet("game:object:id1", It.IsAny<string>()), Times.Once);
        _geoDbMock.Verify(d => d.GeoAdd("game:objects", 2d, 2d, "id1"), Times.Once);
        Assert.NotNull(_layer.GetObject("id1"));
        Assert.Equal("id1", raised?.Id);
    }

    [Fact]
    public void UpdateObject_UpdatesLocationAndRaisesEvent()
    {
        var obj = new MapObject("id1", 2, 2, 2, 2);
        _layer.AddObject(obj);

        MapObject? raised = null;
        _layer.ObjectUpdated += o => raised = o;

        var moved = new MapObject("id1", 5, 5, 2, 2);
        _layer.UpdateObject(moved);

        var loaded = _layer.GetObject("id1");
        Assert.Equal(5, loaded!.X);
        Assert.Equal(5, raised!.X);
    }

    [Fact]
    public void PlaceObjectOnSurface_FillsArea()
    {
        var obj = new MapObject("id1", 1, 1, 2, 2);

        _layer.PlaceObjectOnSurface(obj, TileType.Mountain);

        Assert.Equal(TileType.Mountain, _surface.GetTile(1, 1));
        Assert.Equal(TileType.Mountain, _surface.GetTile(2, 2));
    }

    [Fact]
    public void GetObject_ReturnsNullWhenMissing()
    {
        Assert.Null(_layer.GetObject("missing"));
    }

    [Fact]
    public void RemoveObject_DeletesAndRaisesEvent()
    {
        var obj = new MapObject("id1", 2, 2, 2, 2);
        _layer.AddObject(obj);

        string? deletedId = null;
        _layer.ObjectDeleted += id => deletedId = id;

        _layer.RemoveObject("id1");

        _geoDbMock.Verify(d => d.KeyDelete("game:object:id1"), Times.Once);
        _geoDbMock.Verify(d => d.SortedSetRemove("game:objects", "id1"), Times.Once);
        Assert.Null(_layer.GetObject("id1"));
        Assert.Equal("id1", deletedId);
    }

    [Fact]
    public void GetObjectsInArea_ReturnsIntersectingOnly()
    {
        var a = new MapObject("a", 1, 1, 2, 2);
        var b = new MapObject("b", 5, 5, 3, 3);
        var c = new MapObject("c", 0, 8, 2, 2);

        _layer.AddObject(a);
        _layer.AddObject(b);
        _layer.AddObject(c);

        var found = _layer.GetObjectsInArea(0, 0, 3, 3).Select(o => o.Id).ToHashSet();
        Assert.Contains("a", found);
        Assert.DoesNotContain("b", found);
        Assert.DoesNotContain("c", found);

        var found2 = _layer.GetObjectsInArea(4, 4, 9, 9).Select(o => o.Id).ToHashSet();
        Assert.Contains("b", found2);
        Assert.DoesNotContain("a", found2);
    }
}