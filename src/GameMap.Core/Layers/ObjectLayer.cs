using GameMap.Core.Converters;
using GameMap.Core.Models;
using GameMap.Core.Storage;

namespace GameMap.Core.Layers;

/// <summary>
/// Provides operations for storing, retrieving, and querying map objects, using a GEO-capable storage for spatial searches.
/// </summary>
public class ObjectLayer : IObjectLayer
{
    private readonly IGeoDb _db;
    private readonly SurfaceLayer _surface;
    private readonly ICoordinateConverter coordinateConverter;

    private const string GeoKey = "game:objects";

    /// <inheritdoc />
    public event Action<MapObject>? ObjectCreated;

    /// <inheritdoc />
    public event Action<MapObject>? ObjectUpdated;

    /// <inheritdoc />
    public event Action<string>? ObjectDeleted;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectLayer"/> class.
    /// </summary>
    /// <param name="db">GEO-capable storage instance.</param>
    /// <param name="surface">Surface layer for placement validation.</param>
    /// <param name="converter">Coordinate converter for GEO operations.</param>
    public ObjectLayer(IGeoDb db, SurfaceLayer surface, ICoordinateConverter converter)
    {
        _db = db;
        _surface = surface;
        coordinateConverter = converter;
    }

    /// <inheritdoc />
    public bool CanPlaceObject(MapObject obj)
    {
        if (obj.X < 0 || obj.Y < 0 || obj.X + obj.Width > _surface.Width || obj.Y + obj.Height > _surface.Height)
            return false;

        return _surface.CanPlaceObjectsInArea(obj.X, obj.Y, obj.X + obj.Width - 1, obj.Y + obj.Height - 1);
    }

    /// <inheritdoc />
    public void AddObject(MapObject obj)
    {
        if (!CanPlaceObject(obj))
            throw new InvalidOperationException("Cannot place object on this terrain.");

        (double lon, double lat) = coordinateConverter.ToRedisCoordinates(obj.X, obj.Y);

        _db.GeoAdd(GeoKey, lon, lat, obj.Id);
        _db.StringSet($"game:object:{obj.Id}", System.Text.Json.JsonSerializer.Serialize(obj));

        ObjectCreated?.Invoke(obj);
    }

    /// <inheritdoc />
    public void UpdateObject(MapObject obj)
    {
        (double lon, double lat) = coordinateConverter.ToRedisCoordinates(obj.X, obj.Y);
        _db.GeoAdd(GeoKey, lon, lat, obj.Id);
        _db.StringSet($"game:object:{obj.Id}", System.Text.Json.JsonSerializer.Serialize(obj));
        ObjectUpdated?.Invoke(obj);
    }

    /// <inheritdoc />
    public void PlaceObjectOnSurface(MapObject obj, TileType type = TileType.Mountain)
    {
        _surface.FillArea(obj.X, obj.Y, obj.X + obj.Width - 1, obj.Y + obj.Height - 1, type);
    }

    /// <inheritdoc />
    public MapObject? GetObject(string id)
    {
        var data = _db.StringGet($"game:object:{id}");
        if (string.IsNullOrEmpty(data)) return null;
        return System.Text.Json.JsonSerializer.Deserialize<MapObject>(data!);
    }

    /// <inheritdoc />
    public void RemoveObject(string id)
    {
        _db.KeyDelete($"game:object:{id}");
        _db.SortedSetRemove(GeoKey, id);

        ObjectDeleted?.Invoke(id);
    }

    /// <inheritdoc />
    public MapObject? GetObjectAt(int x, int y)
    {
        (double lon, double lat) = coordinateConverter.ToRedisCoordinates(x, y);
        var candidates = _db.GeoRadius(GeoKey, lon, lat, 1);
        foreach (var member in candidates)
        {
            var obj = GetObject(member);
            if (obj != null && x >= obj.X && x <= obj.X + obj.Width - 1 && y >= obj.Y && y <= obj.Y + obj.Height - 1)
                return obj;
        }
        return null;
    }

    /// <inheritdoc />
    public List<MapObject> GetObjectsInArea(int x1, int y1, int x2, int y2)
    {
        var results = new List<MapObject>();

        int centerX = (x1 + x2) / 2;
        int centerY = (y1 + y2) / 2;
        (double lon, double lat) = coordinateConverter.ToRedisCoordinates(centerX, centerY);

        int radiusKm = Math.Max(Math.Abs(x2 - x1), Math.Abs(y2 - y1)) / 2 + 1;

        var nearby = _db.GeoRadius(GeoKey, lon, lat, radiusKm);

        foreach (var member in nearby)
        {
            var obj = GetObject(member);
            if (obj != null &&
                obj.X + obj.Width - 1 >= x1 && obj.X <= x2 &&
                obj.Y + obj.Height - 1 >= y1 && obj.Y <= y2)
            {
                results.Add(obj);
            }
        }

        return results;
    }
}
