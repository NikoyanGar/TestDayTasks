using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

var builder = DistributedApplication.CreateBuilder(args);

// Redis cache
var cache = builder
    .AddRedis("redis")
    .WithEndpoint("tcp", endpoint => endpoint.Port = 9009);

// Server (waits for Redis and receives its connection string)
var server = builder.AddProject<Projects.GameMap_Server>("server")
    .WaitFor(cache)
    .WithEnvironment("ConnectionStrings__Redis", cache);

// Client (starts after server)
var client = builder.AddProject<Projects.GameMap_UdpClient>("client")
    .WaitFor(server);

builder.Build().Run();
