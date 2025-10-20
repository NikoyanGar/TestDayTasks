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
        builder.Services.AddControllers();

        builder.Services.AddSingleton<UdpClientManager>();
        builder.Services.AddHostedService<UdpClientBackgroundService>();

        var app = builder.Build();

        app.UseSwagger();
        app.UseSwaggerUI();

        app.MapControllers();

        var opt = app.Services.GetRequiredService<IOptionsMonitor<UdpClientOptions>>().CurrentValue;
        Console.WriteLine($"Starting Web API + UDP client. UDP target={opt.Host}:{opt.Port}, key={opt.ConnectionKey}");
        await app.RunAsync();
    }
}