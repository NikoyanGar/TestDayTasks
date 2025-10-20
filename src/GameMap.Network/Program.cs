using GameMap.Core.Converters;
using GameMap.Core.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MagicOnion.Server;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // DI for core services
        builder.Services.AddSingleton<IGeoDb, RedisGeoDb>(_ => throw new NotImplementedException("Provide Redis connection and IDatabase."));
        builder.Services.AddSingleton<ICoordinateConverter, LinearCoordinateConverter>();
        builder.Services.AddSingleton(new GameMap.Core.Layers.SurfaceLayer(1000, 1000));
        builder.Services.AddSingleton<GameMap.Core.Layers.IObjectLayer, GameMap.Core.Layers.ObjectLayer>(sp =>
        {
            var db = sp.GetRequiredService<IGeoDb>();
            var surface = sp.GetRequiredService<GameMap.Core.Layers.SurfaceLayer>();
            var conv = sp.GetRequiredService<ICoordinateConverter>();
            return new GameMap.Core.Layers.ObjectLayer(db, surface, conv);
        });

        // MagicOnion + gRPC
        builder.Services.AddGrpc();     
        builder.Services.AddMagicOnion();

        var app = builder.Build();

        app.MapMagicOnionService();

        app.Run();
    }
}