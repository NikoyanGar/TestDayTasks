using GameMap.Core.Features.Objects;
using GameMap.Core.Features.Surface;
using GameMap.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace GameMap.Network.Tools;

public static class Seed
{
    public static void SeedObjects(IServiceProvider sp)
    {
        var objects = sp.GetRequiredService<IObjectLayer>();
        var surface = sp.GetRequiredService<SurfaceLayer>();

        // Ensure a predictable, clear demo surface: make everything Plain
        surface.FillArea(0, 0, surface.Width - 1, surface.Height - 1, TileType.Plain);

        // Create a couple of demo objects within bounds on Plain terrain
        var candidates = new[]
        {
            new MapObject("obj-1", 2, 2, 3, 3),
            new MapObject("obj-2", 10, 10, 2, 2)
        };

        foreach (var o in candidates)
        {
            if (objects.CanPlaceObject(o))
            {
                objects.AddObject(o);
            }
        }
    }

    public static void PrintStatus(IServiceProvider sp)
    {
        var surface = sp.GetRequiredService<SurfaceLayer>();
        var objects = sp.GetRequiredService<IObjectLayer>();

        // Print an overlay of surface + objects to console
        int w = surface.Width;
        int h = surface.Height;
        var buffer = new char[h, w];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                var t = surface.GetTile(x, y);
                buffer[y, x] = t switch
                {
                    TileType.Plain => '.',
                    TileType.Mountain => '^',
                    TileType.Water => '~',
                    _ => '?'
                };
            }
        }

        var all = objects.GetObjectsInArea(0, 0, w - 1, h - 1);
        foreach (var o in all)
        {
            for (int yy = o.Y; yy < o.Y + o.Height; yy++)
            {
                for (int xx = o.X; xx < o.X + o.Width; xx++)
                {
                    if ((uint)xx < (uint)w && (uint)yy < (uint)h)
                        buffer[yy, xx] = 'O';
                }
            }
        }

        Console.WriteLine($"Map {w}x{h} with {all.Count} object(s):");
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
                Console.Write(buffer[y, x]);
            Console.WriteLine();
        }

        foreach (var o in all)
            Console.WriteLine($"- {o.Id}: pos=({o.X},{o.Y}) size=({o.Width}x{o.Height})");
    }
}
