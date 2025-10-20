using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using GameMap.UdpClient.Options;

namespace GameMap.UdpClient;

internal sealed class UdpClientBackgroundService : BackgroundService
{
    private readonly UdpClientManager _manager;
    private readonly IOptionsMonitor<UdpClientOptions> _options;
    private int _pollMs;

    public UdpClientBackgroundService(UdpClientManager manager, IOptionsMonitor<UdpClientOptions> options)
    {
        _manager = manager;
        _options = options;
        _pollMs = Clamp(options.CurrentValue.PollIntervalMs);
        _options.OnChange(o => _pollMs = Clamp(o.PollIntervalMs));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _manager.Start();

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _manager.PollEvents();
                await Task.Delay(_pollMs, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // normal on shutdown
        }
        finally
        {
            _manager.Stop();
        }
    }

    private static int Clamp(int v) => Math.Clamp(v, 1, 100);
}
