using GameMap.Core.Features.Objects;
using GameMap.Core.Features.Surface;
using GameMap.Core.Models;
using GameMap.Core.Storage;
using GameMap.Network.Infrastructure.Converters;
using Moq;
using Xunit;

namespace GameMap.Tests;

public class ObjectLayerTests
{
    private static (ObjectLayer layer, Mock<IGeoDb> db) Create()
    {
        var db = new Mock<IGeoDb>(MockBehavior.Strict);
        var surface = new SurfaceLayer(10, 10);
        var conv = new LinearCoordinateConverter();
        var layer = new ObjectLayer(db.Object, surface, conv);
        return (layer, db);
    }

    [Fact]
    public void Add_Get_Remove_Object_Flow()
    {
        var (layer, db) = Create();
        var obj = new MapObject("o1", 1, 1, 2, 2);

        db.Setup(d => d.GeoAdd(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>(), obj.Id));
        db.Setup(d => d.StringSet("game:object:o1", It.IsAny<string>()));

        string? stored = null;
        db.Setup(d => d.StringGet("game:object:o1")).Returns(() => stored);
        db.Setup(d => d.KeyDelete("game:object:o1"));
        db.Setup(d => d.SortedSetRemove(It.IsAny<string>(), obj.Id));

        layer.ObjectCreated += m => Assert.Equal("o1", m.Id);
        layer.ObjectDeleted += id => Assert.Equal("o1", id);

        // Add
        layer.AddObject(obj);

        // Simulate storage
        stored = System.Text.Json.JsonSerializer.Serialize(obj);

        // Get
        var got = layer.GetObject("o1");
        Assert.NotNull(got);
        Assert.Equal(obj.Id, got!.Id);

        // Remove
        layer.RemoveObject("o1");

        db.VerifyAll();
    }

    [Fact]
    public void GetObjectAt_And_GetObjectsInArea()
    {
        var (layer, db) = Create();
        var o1 = new MapObject("a", 0, 0, 2, 2);
        var o2 = new MapObject("b", 5, 5, 2, 2);

        // Add: no strict validation here
        db.Setup(d => d.GeoAdd(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>(), o1.Id));
        db.Setup(d => d.StringSet("game:object:a", It.IsAny<string>()));
        db.Setup(d => d.GeoAdd(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>(), o2.Id));
        db.Setup(d => d.StringSet("game:object:b", It.IsAny<string>()));

        string? sa = System.Text.Json.JsonSerializer.Serialize(o1);
        string? sb = System.Text.Json.JsonSerializer.Serialize(o2);
        db.Setup(d => d.StringGet("game:object:a")).Returns(sa);
        db.Setup(d => d.StringGet("game:object:b")).Returns(sb);

        layer.AddObject(o1);
        layer.AddObject(o2);

        // GeoRadius around (1,1) and (5,5)
        db.Setup(d => d.GeoRadius(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
          .Returns<string, double, double, double>((_, __, ___, ____) => new[] { "a", "b" });

        var at = layer.GetObjectAt(1, 1);
        Assert.NotNull(at);
        Assert.Equal("a", at!.Id);

        var inArea = layer.GetObjectsInArea(0, 0, 3, 3);
        Assert.Contains(inArea, o => o.Id == "a");
        Assert.DoesNotContain(inArea, o => o.Id == "b");

        // Cleanup
        db.VerifyAll();
    }

    [Fact]
    public void CannotPlace_On_Mountains()
    {
        var surface = new SurfaceLayer(10, 10);
        surface.FillArea(0, 0, 3, 3, TileType.Mountain);
        var conv = new LinearCoordinateConverter();
        var db = new Mock<IGeoDb>(MockBehavior.Loose);
        var layer = new ObjectLayer(db.Object, surface, conv);

        var obj = new MapObject("x", 1, 1, 1, 1);
        Assert.False(layer.CanPlaceObject(obj));
    }
}
