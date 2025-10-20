using GameMap.UdpClient.Models;
using GameMap.UdpClient.Options;
using Microsoft.Extensions.Options;

namespace GameMap.UdpClient;

internal class Program
{
    static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        builder.Services.Configure<UdpClientOptions>(builder.Configuration.GetSection("UdpClient"));

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddSingleton<UdpClientManager>();
        builder.Services.AddHostedService<UdpClientBackgroundService>();

        var app = builder.Build();

        app.UseSwagger();
        app.UseSwaggerUI();

        var api = app.MapGroup("/api").WithTags("UDP Client");

        api.MapGet("/status", (UdpClientManager mgr) =>
        {
            var status = mgr.GetStatus();
            return Results.Ok(status);
        })
        .WithName("GetStatus")
        .WithSummary("Get UDP client connection status");

        api.MapPost("/connect", (UdpClientManager mgr, ConnectRequest req) =>
        {
            mgr.Reconfigure(req.Host, req.Port, req.Key);
            return Results.Accepted();
        })
        .WithName("ConfigureAndReconnect")
        .WithSummary("Set host/port/key and reconnect to the UDP server");

        api.MapPost("/messages/ping", async (UdpClientManager mgr, CancellationToken ct) =>
        {
            var result = await mgr.SendPingAsync(TimeSpan.FromSeconds(5), ct);
            return result.Success
                ? Results.Ok(new PingResponseDto(result.ServerTicksUtcMs, result.RttMs))
                : Results.Problem(result.Error ?? "Ping failed", statusCode: 504);
        })
        .WithName("SendPing")
        .WithSummary("Send a Ping to the UDP server and get RTT");

        var opt = app.Services.GetRequiredService<IOptionsMonitor<UdpClientOptions>>().CurrentValue;
        Console.WriteLine($"Starting Web API + UDP client. UDP target={opt.Host}:{opt.Port}, key={(string.IsNullOrEmpty(opt.ConnectionKey) ? "<null>" : "***")}");
        await app.RunAsync();
    }
}
