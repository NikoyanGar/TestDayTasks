using GameMap.Core.Layers.Objects;
using GameMap.Core.Layers.Regions;
using GameMap.Core.Models;

namespace GameMap.Core
{
    public interface IMapManager
    {
        // Forwarded events from IObjectLayer
        event Action<MapObject>? ObjectCreated;
        event Action<string>? ObjectDeleted;
        event Action<MapObject>? ObjectUpdated;

        bool CanPlaceObjectInArea(int x1, int y1, int x2, int y2);
        List<MapObject> GetObjectsInArea(int x1, int y1, int x2, int y2);
        List<Region> GetRegionsInArea(int x, int y, int width, int height);
        void PrintMapWithObjects();
        bool TryPlaceObject(MapObject obj, TileType? occupyTile = null);
    }
}