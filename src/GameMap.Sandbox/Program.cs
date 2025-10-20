using GameMap.Core;
using GameMap.Core.Features.Objects;
using GameMap.Core.Features.Regions;
using GameMap.Core.Features.Surface;
using GameMap.Core.Models;

namespace GameMap.Sandbox;

internal class Program
{
    static void Main(string[] args)
    {
        var surface = new SurfaceLayer(100, 100, TileType.Plain);
        var regions = new RegionLayer(100, 100, 100); // 10x10 equal-area grid (100 regions total)
        var geo = new InMemoryGeoDb();
        var converter = new GridCoordinateConverter();
        var objects = new ObjectLayer(geo, surface, converter);
        var map = new MapManager(surface, objects, regions);

        // 1) Surface: fill a mountain patch and verify placement checks
        surface.FillArea(1, 1, 10, 10, TileType.Mountain);
        Console.WriteLine($"Can place (0,0..1,1): {map.CanPlaceObjectInArea(0, 0, 1, 1)}"); // True
        Console.WriteLine($"Can place (2,2..4,4): {map.CanPlaceObjectInArea(2, 2, 4, 4)}"); // False

        // 2) Regions: query regions in area spanning boundaries
        var regionList = map.GetRegionsInArea(95, 95, 20, 20);
        Console.WriteLine($"Regions intersecting (95,95,20x20): {regionList.Count}");

        // 3) Object placement: add a 3x2 object at (12,20)
        var obj = new MapObject("house-1", 12, 20, 3, 2);

        // Place the object while leaving the surface as-is; occupancy is tracked by the object layer.
        bool placed = map.TryPlaceObject(obj);
        Console.WriteLine($"Placed: {placed}");

        var objectsInArea = map.GetObjectsInArea(10, 18, 20, 25);
        Console.WriteLine($"Objects in area: {objectsInArea.Count}");

        var canPlaceOverObject = map.CanPlaceObjectInArea(12, 20, 14, 21);
        Console.WriteLine($"Can place over existing: {canPlaceOverObject}");

        map.PrintMapWithObjects();
    }
}
