using System.Threading;
using GameMap.Core;
using GameMap.Core.Converters;
using GameMap.Core.Features.Regions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using GameMap.Network.Tools;
using GameMap.Network.Udp;
using GameMap.Network.Infrastructure;
using GameMap.Network.Infrastructure.Converters;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // DI for core services
        builder.Services.AddSingleton<IGeoDb, InMemoryGeoDb>();
        builder.Services.AddSingleton<ICoordinateConverter, LinearCoordinateConverter>();
        builder.Services.AddSingleton(new SurfaceLayer(40, 20)); // smaller for console print
        builder.Services.AddSingleton<IObjectLayer, ObjectLayer>(sp =>
        {
            var db = sp.GetRequiredService<IGeoDb>();
            var surface = sp.GetRequiredService<SurfaceLayer>();
            var conv = sp.GetRequiredService<ICoordinateConverter>();
            return new ObjectLayer(db, surface, conv);
        });

        // Register RegionLayer as concrete and as IRegionLayer
        builder.Services.AddSingleton<RegionLayer>(sp => new RegionLayer(40, 20, 4));
        builder.Services.AddSingleton<IRegionLayer>(sp => sp.GetRequiredService<RegionLayer>());

        // MapManager (used by MapQueryProvider)
        builder.Services.AddSingleton<MapManager>(sp =>
        {
            var surface = sp.GetRequiredService<SurfaceLayer>();
            var db = sp.GetRequiredService<IGeoDb>();
            var conv = sp.GetRequiredService<ICoordinateConverter>();
            return new MapManager(surface, db, conv);
        });

        // IMapQueryProvider for UDP server
        builder.Services.AddSingleton<IMapQueryProvider, MapQueryProvider>();

        // MagicOnion + gRPC (default)
        builder.Services.AddGrpc();
        builder.Services.AddMagicOnion();

        builder.WebHost.ConfigureKestrel(o =>
        {
            o.ListenAnyIP(5000, lo => lo.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2);
        });

        var app = builder.Build();

        // Seed demo data for end-to-end testing and print state
        Seed.SeedObjects(app.Services);
        Seed.PrintStatus(app.Services);

        // Start UDP query server (LiteNetLib)
        var provider = app.Services.GetRequiredService<IMapQueryProvider>();
        var udp = new MapUdpServer(provider, port: 9050);

        var lifetime = app.Services.GetRequiredService<Microsoft.Extensions.Hosting.IHostApplicationLifetime>();
        lifetime.ApplicationStopping.Register(() => udp.Dispose());

        // Pump UDP events on a background timer
        var timer = new Timer(_ => udp.PollEvents(), null, 0, 10);
        lifetime.ApplicationStopping.Register(() => timer.Dispose());

        app.MapMagicOnionService();

        app.Run();
    }
}