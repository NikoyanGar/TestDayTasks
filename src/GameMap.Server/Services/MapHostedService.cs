using GameMap.Core;
using GameMap.Core.Layers.Objects;
using GameMap.Core.Layers.Surface;
using GameMap.Core.Models;
using GameMap.Server.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameMap.Server.Services;

public sealed class MapHostedService : BackgroundService
{
    private readonly ILogger<MapHostedService> _logger;
    private readonly ISurfaceLayer _surface;
    private readonly IMapManager _map;
    private readonly IOptions<MapOptions> _options;

    public MapHostedService(
        ILogger<MapHostedService> logger,
        ISurfaceLayer surface,
        IMapManager map,
        IOptions<MapOptions> options)
    {
        _logger = logger;
        _surface = surface;
        _map = map;
        _options = options;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        InitializeMap();
        _logger.LogInformation("Game server started. Press Ctrl+C to shut down.");

        // Keep the host running until cancellation.
        return Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private void InitializeMap()
    {
        var opt = _options.Value;

        _logger.LogInformation("Initializing map {Width}x{Height}, regions: {Divisions}x{Divisions}, default tile: {Tile}",
            _surface.Width, _surface.Height, opt.RegionGridDivisions, opt.RegionGridDivisions, opt.DefaultTile);

        // Apply surface fills
        if (opt.Fills is { Count: > 0 })
        {
            foreach (var fill in opt.Fills)
            {
                _surface.FillArea(fill.X1, fill.Y1, fill.X2, fill.Y2, fill.Tile);
                _logger.LogInformation("Filled area ({X1},{Y1})..({X2},{Y2}) with {Tile}", fill.X1, fill.Y1, fill.X2, fill.Y2, fill.Tile);
            }
        }

        // Place initial objects
        if (opt.Objects is { Count: > 0 })
        {
            foreach (var spec in opt.Objects)
            {
                var obj = new MapObject(spec.Id, spec.X, spec.Y, spec.Width, spec.Height);
                var placed = _map.TryPlaceObject(obj, occupyTile: spec.OccupyTile);
                _logger.LogInformation("Place object {Id} at ({X},{Y}) {W}x{H} => {Placed}",
                    spec.Id, spec.X, spec.Y, spec.Width, spec.Height, placed);
            }
        }

        if (opt.ShowMapOnStart)
        {
            _map.PrintMapWithObjects();
        }
    }
}