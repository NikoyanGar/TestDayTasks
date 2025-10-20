using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Server (ASP.NET Core + MagicOnion)
var server = builder.AddProject(
    name: "gamemap-server",
    projectPath: "..\\..\\GameMap.Network\\GameMap.Network.csproj");

// Client (console) depends on the server
var client = builder.AddProject(
        name: "gamemap-client",
        projectPath: "..\\..\\GameMap.Client\\GameMap.Client.csproj")
    .WithReference(server);

builder.Build().Run();
