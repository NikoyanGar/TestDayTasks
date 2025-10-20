using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

var builder = DistributedApplication.CreateBuilder(args);

// Configure shared UDP settings
const int udpPort = 9050;
const string host = "127.0.0.1";
const string? connectionKey = null; // set to a non-empty string to require a key

// Server project
var server = builder.AddProject<Projects.GameMap_Server>("server")
    // Map Network options to environment variables expected by GameMap.Server
    .WithEnvironment("Network__Port", udpPort.ToString())
    .WithEnvironment("Network__IPv6Enabled", "false")
    .WithEnvironment("Network__UnconnectedMessagesEnabled", "false")
    .WithEnvironment("Network__ConnectionKey", connectionKey ?? string.Empty);

// Client project
var clientArgs = connectionKey is { Length: > 0 }
    ? new[] { host, udpPort.ToString(), connectionKey! }
    : new[] { host, udpPort.ToString() };

var client = builder.AddProject<Projects.GameMap_UdpClient>("client")
    .WithArgs(clientArgs)
    .WaitFor(server); // ensure server is up before client connects

builder.Build().Run();
