using System.Diagnostics;
using GameMap.SharedContracts.Networking;
using GameMap.SharedContracts.Networking.Packets;
using LiteNetLib;
using Microsoft.Extensions.Options;
using GameMap.UdpClient.Options;
using GameMap.UdpClient.Models;

namespace GameMap.UdpClient;

internal sealed class UdpClientManager
{
    private readonly IOptionsMonitor<UdpClientOptions> _options;
    private readonly EventBasedNetListener _listener;
    private readonly NetManager _client;

    private NetPeer? _peer;
    private TaskCompletionSource<NetPeer>? _connectedTcs;
    private TaskCompletionSource<PongPacket>? _pongTcs;
    private readonly Stopwatch _sw = new();

    private long _lastConnectAttemptTicks;
    private TimeSpan _reconnectDelay;

    private record struct Endpoint(string Host, int Port, string? Key);
    private Endpoint _endpoint;

    public UdpClientManager(IOptionsMonitor<UdpClientOptions> options)
    {
        _options = options;
        var opt = options.CurrentValue;
        _endpoint = new Endpoint(opt.Host, opt.Port, opt.ConnectionKey);
        _reconnectDelay = TimeSpan.FromMilliseconds(Math.Max(1, opt.ReconnectDelayMs));

        _listener = new EventBasedNetListener();
        _client = new NetManager(_listener)
        {
            IPv6Enabled = false,
            AutoRecycle = false
        };

        _connectedTcs = new TaskCompletionSource<NetPeer>(TaskCreationOptions.RunContinuationsAsynchronously);

        _listener.PeerConnectedEvent += peer =>
        {
            _peer = peer;
            Console.WriteLine($"[UDP] Connected to {peer.Address}");
            _connectedTcs?.TrySetResult(peer);
        };

        _listener.NetworkReceiveEvent += (peer, reader, channel, method) =>
        {
            try
            {
                var span = new ReadOnlySpan<byte>(reader.RawData, reader.UserDataOffset, reader.UserDataSize);
                if (!PacketSerializer.TryDeserialize(span, out var type, out var message))
                    return;

                switch (type)
                {
                    case PacketType.Pong when message is PongPacket pong:
                        _sw.Stop();
                        Console.WriteLine($"[UDP] Pong received. ServerTicksUtcMs={pong.ServerTicksUtcMs}. RTT≈{_sw.ElapsedMilliseconds}ms");
                        _pongTcs?.TrySetResult(pong);
                        break;
                }
            }
            finally
            {
                reader.Recycle();
            }
        };

        _listener.PeerDisconnectedEvent += (peer, info) =>
        {
            Console.WriteLine($"[UDP] Disconnected: {peer.Address}. Reason={info.Reason}");
            _peer = null;
            _connectedTcs = new TaskCompletionSource<NetPeer>(TaskCreationOptions.RunContinuationsAsynchronously);
        };

        _listener.NetworkErrorEvent += (endPoint, error) =>
        {
            Console.WriteLine($"[UDP] Network error: {error} at {endPoint}");
        };

        // React to appsettings.json changes
        _options.OnChange(o =>
        {
            _reconnectDelay = TimeSpan.FromMilliseconds(Math.Max(1, o.ReconnectDelayMs));
            Reconfigure(o.Host, o.Port, o.ConnectionKey);
        });
    }

    public void Start()
    {
        if (_client.IsRunning) return;
        _client.Start();
        Console.WriteLine($"[UDP] Connecting to {_endpoint.Host}:{_endpoint.Port} ...");
        ConnectNow();
    }

    public void Stop()
    {
        if (!_client.IsRunning) return;
        _client.Stop();
        _peer = null;
    }

    public void PollEvents()
    {
        _client.PollEvents();
        TryReconnect();
    }

    public void Reconfigure(string host, int port, string? key)
    {
        _endpoint = new Endpoint(host, port, key);
        Console.WriteLine($"[UDP] Reconfigured target to {_endpoint.Host}:{_endpoint.Port} (key={(string.IsNullOrEmpty(_endpoint.Key) ? "<null>" : "***")}). Reconnecting...");
        _peer = null;
        _connectedTcs = new TaskCompletionSource<NetPeer>(TaskCreationOptions.RunContinuationsAsynchronously);
        ConnectNow(force: true);
    }

    public async Task<(bool Success, long ServerTicksUtcMs, long RttMs, string? Error)> SendPingAsync(TimeSpan timeout, CancellationToken ct)
    {
        try
        {
            var peer = await EnsureConnectedAsync(timeout, ct);
            if (peer == null)
                return (false, 0, 0, "Not connected");

            var ping = new PingPacket(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            var bytes = PacketSerializer.Serialize(PacketType.Ping, ping);

            var tcs = new TaskCompletionSource<PongPacket>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pongTcs = tcs;

            _sw.Restart();
            peer.Send(bytes, DeliveryMethod.Unreliable);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeout);

            var completed = await Task.WhenAny(tcs.Task, Task.Delay(Timeout.Infinite, cts.Token));
            if (completed != tcs.Task)
                return (false, 0, 0, "Timeout waiting for Pong");

            var pong = await tcs.Task;
            var rtt = _sw.ElapsedMilliseconds;
            return (true, pong.ServerTicksUtcMs, rtt, null);
        }
        catch (OperationCanceledException)
        {
            return (false, 0, 0, "Canceled");
        }
        catch (Exception ex)
        {
            return (false, 0, 0, ex.Message);
        }
        finally
        {
            _pongTcs = null;
        }
    }

    public UdpStatusDto GetStatus()
    {
        var connected = _peer != null && _peer.ConnectionState == ConnectionState.Connected;
        return new UdpStatusDto(
            Connected: connected,
            RemoteEndPoint: connected ? _peer!.Address.ToString() : null
        );
    }

    private async Task<NetPeer?> EnsureConnectedAsync(TimeSpan timeout, CancellationToken ct)
    {
        if (_peer != null && _peer.ConnectionState == ConnectionState.Connected)
            return _peer;

        TryReconnect();

        var tcs = _connectedTcs ?? new TaskCompletionSource<NetPeer>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);
        var completed = await Task.WhenAny(tcs.Task, Task.Delay(Timeout.Infinite, cts.Token));
        if (completed == tcs.Task)
            return await tcs.Task;

        return null;
    }

    private void TryReconnect()
    {
        if (!_client.IsRunning)
            return;

        if (_peer != null && _peer.ConnectionState is ConnectionState.Connected or ConnectionState.Outgoing)
            return;

        var now = Environment.TickCount64;
        if (now - _lastConnectAttemptTicks < (long)_reconnectDelay.TotalMilliseconds)
            return;

        ConnectNow();
    }

    private void ConnectNow(bool force = false)
    {
        _lastConnectAttemptTicks = Environment.TickCount64;
        Console.WriteLine($"[UDP] Attempting connect to {_endpoint.Host}:{_endpoint.Port} (key={(string.IsNullOrEmpty(_endpoint.Key) ? "<null>" : "***")}) ...");
        var peer = _client.Connect(_endpoint.Host, _endpoint.Port, _endpoint.Key);
        if (force)
            _peer = peer;
    }
}
