using GameMap.Core.Models;

namespace GameMap.Core.Features.Objects;

/// <summary>
/// Defines operations for managing map objects and querying them spatially.
/// </summary>
public interface IObjectLayer
{
    /// <summary>
    /// Raised when an object is created.
    /// </summary>
    event Action<MapObject>? ObjectCreated;

    /// <summary>
    /// Raised when an object is deleted.
    /// </summary>
    event Action<string>? ObjectDeleted;

    /// <summary>
    /// Raised when an object is updated.
    /// </summary>
    event Action<MapObject>? ObjectUpdated;

    /// <summary>
    /// Adds an object to the layer/storage.
    /// </summary>
    void AddObject(MapObject obj);

    /// <summary>
    /// Updates object data and its spatial index.
    /// </summary>
    void UpdateObject(MapObject obj);

    /// <summary>
    /// Checks that the object can be placed on the surface.
    /// </summary>
    bool CanPlaceObject(MapObject obj);

    /// <summary>
    /// Gets object by identifier.
    /// </summary>
    MapObject? GetObject(string id);

    /// <summary>
    /// Gets a single object containing the coordinate if any.
    /// </summary>
    MapObject? GetObjectAt(int x, int y);

    /// <summary>
    /// Gets all objects that intersect the specified rectangle area.
    /// </summary>
    List<MapObject> GetObjectsInArea(int x1, int y1, int x2, int y2);

    /// <summary>
    /// Fills the object's rectangle on the surface with a type (helper for tests/tools).
    /// </summary>
    void PlaceObjectOnSurface(MapObject obj, TileType type = TileType.Mountain);

    /// <summary>
    /// Removes object by identifier.
    /// </summary>
    void RemoveObject(string id);
}