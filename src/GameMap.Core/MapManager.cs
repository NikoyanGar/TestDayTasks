using GameMap.Core.Converters;
using GameMap.Core.Features.Objects;
using GameMap.Core.Features.Regions;
using GameMap.Core.Features.Surface;
using GameMap.Core.Models;
using GameMap.Core.Storage;

namespace GameMap.Core;

public class MapManager : IMapManager
{
    private ISurfaceLayer _surfaceLayer { get; set; }

    private IObjectLayer _objectsLayer { get; }

    private IRegionLayer _regionLayer { get; set; }

    public MapManager(ISurfaceLayer surface, IObjectLayer objectLayer, IRegionLayer regionLayer)
    {
        _surfaceLayer = surface;
        _objectsLayer = objectLayer;
        _regionLayer = regionLayer;
    }


    public bool TryPlaceObject(MapObject obj, TileType? occupyTile = null)
    {
        if (!_surfaceLayer.CanPlaceObjectsInArea(obj.X, obj.Y, obj.X + obj.Width - 1, obj.Y + obj.Height - 1))
            return false;

        var overlappingObjects = _objectsLayer.GetObjectsInArea(obj.X, obj.Y, obj.X + obj.Width - 1, obj.Y + obj.Height - 1);
        if (overlappingObjects.Count > 0) return false;

        _objectsLayer.AddObject(obj);

        if (occupyTile.HasValue)
            _surfaceLayer.FillArea(obj.X, obj.Y, obj.X + obj.Width - 1, obj.Y + obj.Height - 1, occupyTile.Value);

        return true;
    }

    public bool CanPlaceObjectInArea(int x1, int y1, int x2, int y2)
    {
        if (!_surfaceLayer.CanPlaceObjectsInArea(x1, y1, x2, y2)) return false;
        var objects = _objectsLayer.GetObjectsInArea(x1, y1, x2, y2);
        return objects.Count == 0;
    }

    public List<MapObject> GetObjectsInArea(int x1, int y1, int x2, int y2)
        => _objectsLayer.GetObjectsInArea(x1, y1, x2, y2);

    public void PrintMapWithObjects()
    {
        for (int y = 0; y < _surfaceLayer.Height; y++)
        {
            for (int x = 0; x < _surfaceLayer.Width; x++)
            {
                var objHere = _objectsLayer.GetObjectsInArea(x, y, x, y).FirstOrDefault();
                if (objHere != null)
                    Console.Write('O'); // object symbol
                else
                    Console.Write(_surfaceLayer.GetTile(x, y) switch
                    {
                        TileType.Plain => '.',
                        TileType.Mountain => '^',
                        TileType.Water => '~',
                        _ => '?'
                    });
            }
            Console.WriteLine();
        }
    }

    public List<Region> GetRegionsInArea(int x1, int y1, int v1, int v2)
    {
        return _regionLayer.GetRegionsInArea(x1, y1, v1, v2).ToList();
    }
}

