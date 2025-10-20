using GameMap.Core.Features.Objects;
using GameMap.Core.Features.Regions;
using GameMap.Core.Models;

namespace GameMap.Core
{
    public interface IMapManager
    {
        bool CanPlaceObjectInArea(int x1, int y1, int x2, int y2);
        List<MapObject> GetObjectsInArea(int x1, int y1, int x2, int y2);
        List<Region> GetRegionsInArea(int x1, int y1, int v1, int v2);
        void PrintMapWithObjects();
        bool TryPlaceObject(MapObject obj, TileType? occupyTile = null);
    }
}