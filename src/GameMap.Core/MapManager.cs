using GameMap.Core.Converters;
using GameMap.Core.Features.Objects;
using GameMap.Core.Features.Surface;
using GameMap.Core.Models;
using GameMap.Core.Storage;

namespace GameMap.Core;

public class MapManager
{
    private  ISurfaceLayer _surface { get; set; }
    public ObjectLayer Objects { get; }

    public MapManager(ISurfaceLayer surface, IGeoDb geodb, ICoordinateConverter coordinateConverter)
    {
        _surface = surface;
        Objects = new ObjectLayer(geodb, surface, coordinateConverter);
    }


    public bool TryPlaceObject(MapObject obj, TileType? occupyTile = null)
    {
        if (!_surface.CanPlaceObjectsInArea(obj.X, obj.Y, obj.X + obj.Width - 1, obj.Y + obj.Height - 1))
            return false;

        var overlappingObjects = Objects.GetObjectsInArea(obj.X, obj.Y, obj.X + obj.Width - 1, obj.Y + obj.Height - 1);
        if (overlappingObjects.Count > 0) return false;

        Objects.AddObject(obj);

        if (occupyTile.HasValue)
            _surface.FillArea(obj.X, obj.Y, obj.X + obj.Width - 1, obj.Y + obj.Height - 1, occupyTile.Value);

        return true;
    }

    public bool CanPlaceObjectInArea(int x1, int y1, int x2, int y2)
    {
        if (!_surface.CanPlaceObjectsInArea(x1, y1, x2, y2)) return false;
        var objects = Objects.GetObjectsInArea(x1, y1, x2, y2);
        return objects.Count == 0;
    }

    public List<MapObject> GetObjectsInArea(int x1, int y1, int x2, int y2)
        => Objects.GetObjectsInArea(x1, y1, x2, y2);

    public void PrintMapWithObjects()
    {
        for (int y = 0; y < _surface.Height; y++)
        {
            for (int x = 0; x < _surface.Width; x++)
            {
                var objHere = Objects.GetObjectsInArea(x, y, x, y).FirstOrDefault();
                if (objHere != null)
                    Console.Write('O'); // object symbol
                else
                    Console.Write(_surface.GetTile(x, y) switch
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
}

