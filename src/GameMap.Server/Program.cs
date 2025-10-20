using GameMap.Core;
using GameMap.Core.Layers.Objects;
using GameMap.Core.Layers.Regions;
using GameMap.Core.Layers.Surface;
using GameMap.Core.Storage;
using GameMap.Server.Options;
using GameMap.Server.Services;
using GameMap.SharedContracts.Networking.Packets;
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
    var connStr = cfg.GetConnectionString("Redis") ?? "localhost:6379";
    return ConnectionMultiplexer.Connect(connStr);
});
builder.Services.AddSingleton<IDatabase>(sp =>
    sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase()); //TODO: full options

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
builder.Services
    .AddOptions<NetworkOptions>()
    .Bind(builder.Configuration.GetSection("Network"));

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

// Hosted services
builder.Services.AddHostedService<MapHostedService>();

// Register UDP server both as a singleton service and as a hosted service so we can inject/resolve it
builder.Services.AddSingleton<UdpServerHostedService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<UdpServerHostedService>());

var host = builder.Build();

// Subscribe to map object events and broadcast via UDP
var mapManager = host.Services.GetRequiredService<IMapManager>();
var udpServer = host.Services.GetRequiredService<UdpServerHostedService>();

mapManager.ObjectCreated += obj =>
{
    var ev = new ObjectEventMessage { Id = obj.Id, X = obj.X, Y = obj.Y, Width = obj.Width, Height = obj.Height };
    udpServer.BroadcastObjectEvent(PacketType.ObjectAdded, ev);
};

mapManager.ObjectUpdated += obj =>
{
    var ev = new ObjectEventMessage { Id = obj.Id, X = obj.X, Y = obj.Y, Width = obj.Width, Height = obj.Height };
    udpServer.BroadcastObjectEvent(PacketType.ObjectUpdated, ev);
};

mapManager.ObjectDeleted += id =>
{
    var ev = new ObjectEventMessage { Id = id, X = 0, Y = 0, Width = 0, Height = 0 };
    udpServer.BroadcastObjectEvent(PacketType.ObjectDeleted, ev);
};

await host.RunAsync();
