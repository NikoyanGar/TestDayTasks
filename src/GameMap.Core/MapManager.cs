using GameMap.Core.Converters;
using GameMap.Core.Layers.Objects;
using GameMap.Core.Layers.Regions;
using GameMap.Core.Layers.Surface;
using GameMap.Core.Models;

namespace GameMap.Core;

public sealed class MapManager : IMapManager
{
    private readonly ISurfaceLayer _surfaceLayer;
    private readonly IObjectLayer _objectsLayer;
    private readonly IRegionLayer _regionLayer;

    public MapManager(ISurfaceLayer surface, IObjectLayer objectLayer, IRegionLayer regionLayer)
    {
        _surfaceLayer = surface ?? throw new ArgumentNullException(nameof(surface));
        _objectsLayer = objectLayer ?? throw new ArgumentNullException(nameof(objectLayer));
        _regionLayer = regionLayer ?? throw new ArgumentNullException(nameof(regionLayer));
    }

    public bool TryPlaceObject(MapObject obj, TileType? occupyTile = null)
    {
        if (obj is null) throw new ArgumentNullException(nameof(obj));

        var (x2, y2) = (obj.X + obj.Width - 1, obj.Y + obj.Height - 1);

        if (!_surfaceLayer.CanPlaceObjectsInArea(obj.X, obj.Y, x2, y2))
            return false;

        var overlapping = _objectsLayer.GetObjectsInArea(obj.X, obj.Y, x2, y2);
        if (overlapping.Count > 0) return false;

        _objectsLayer.AddObject(obj);

        if (occupyTile.HasValue)
            _surfaceLayer.FillArea(obj.X, obj.Y, x2, y2, occupyTile.Value);

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
        int width = _surfaceLayer.Width;
        int height = _surfaceLayer.Height;

        var originalColor = Console.ForegroundColor;
        try
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    char ch;
                    var objHere = _objectsLayer.GetObjectAt(x, y);
                    if (objHere is not null)
                    {
                        ch = 'O'; // object symbol
                    }
                    else
                    {
                        ch = _surfaceLayer.GetTile(x, y) switch
                        {
                            TileType.Plain => '.',
                            TileType.Mountain => '^',
                            TileType.Water => '~',
                            _ => '?'
                        };
                    }

                    bool isRegionBorder = IsRegionBorderTile(x, y);

                    if (isRegionBorder)
                        Console.ForegroundColor = ConsoleColor.Red;

                    Console.Write(ch);

                    if (isRegionBorder)
                        Console.ForegroundColor = originalColor;
                }
                Console.WriteLine();
            }
        }
        finally
        {
            Console.ForegroundColor = originalColor;
        }
    }

    private bool IsRegionBorderTile(int x, int y)
    {
        ushort id = _regionLayer.GetRegionId(x, y);

        // Check any 4-neighborhood change in region id to mark this tile as on a border.
        if (x > 0 && _regionLayer.GetRegionId(x - 1, y) != id) return true;                          // West
        if (x < _regionLayer.Width - 1 && _regionLayer.GetRegionId(x + 1, y) != id) return true;     // East
        if (y > 0 && _regionLayer.GetRegionId(x, y - 1) != id) return true;                          // North
        if (y < _regionLayer.Height - 1 && _regionLayer.GetRegionId(x, y + 1) != id) return true;    // South

        return false;
    }

    public List<Region> GetRegionsInArea(int x, int y, int width, int height)
        => _regionLayer.GetRegionsInArea(x, y, width, height).ToList();
}

