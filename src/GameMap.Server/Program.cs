using GameMap.Core;
using GameMap.Core.Converters;
using GameMap.Core.Features.Objects;
using GameMap.Core.Features.Regions;
using GameMap.Core.Features.Surface;
using GameMap.Core.Models;
using GameMap.Core.Storage;
using GameMap.Server.Options;
using GameMap.Server.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using StackExchange.Redis;



var builder = Host.CreateApplicationBuilder(args);

// Redis (StackExchange.Redis)
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var connStr = cfg.GetConnectionString("Redis")?? "localhost:6379";

    return ConnectionMultiplexer.Connect(connStr);
});
builder.Services.AddSingleton<IDatabase>(sp =>
    sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());//TODO: full options

builder.Services.AddSingleton<IGeoDb>(sp =>
    new RedisGeoDb(sp.GetRequiredService<IDatabase>()));

// Configuration (appsettings.json + environment)
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Options
builder.Services
    .AddOptions<MapOptions>()
    .Bind(builder.Configuration.GetSection("Map"));

// Core dependencies
builder.Services.AddSingleton<ISurfaceLayer>(sp =>
{
    var opt = sp.GetRequiredService<IOptions<MapOptions>>().Value;
    return new SurfaceLayer(opt.Width, opt.Height, opt.DefaultTile);
});

builder.Services.AddSingleton<IRegionLayer>(sp =>
{
    var opt = sp.GetRequiredService<IOptions<MapOptions>>().Value;
    return new RegionLayer(opt.Width, opt.Height, opt.RegionGridDivisions);
});

builder.Services.AddSingleton<IObjectLayer>(sp =>
{
    var opt = sp.GetRequiredService<IOptions<MapOptions>>().Value;
    var surface = sp.GetRequiredService<ISurfaceLayer>();
    var geo = sp.GetRequiredService<IGeoDb>();
    var converter = new CoordinateConverter();
    return new ObjectLayer(geo, surface, converter);
});

builder.Services.AddSingleton<IMapManager>(sp =>
{
    var surface = sp.GetRequiredService<ISurfaceLayer>();
    var objects = sp.GetRequiredService<IObjectLayer>();
    var regions = sp.GetRequiredService<IRegionLayer>();
    return new MapManager(surface, objects, regions);
});

// Hosted service (the game server loop)
builder.Services.AddHostedService<MapHostedService>();

var host = builder.Build();

await host.RunAsync();
