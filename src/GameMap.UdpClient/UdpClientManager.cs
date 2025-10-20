using System.Diagnostics;
using System.Threading;
using GameMap.SharedContracts.Networking;
using GameMap.SharedContracts.Networking.Packets;
using LiteNetLib;
using Microsoft.Extensions.Options;
using GameMap.UdpClient.Options;
using GameMap.UdpClient.Models;

namespace GameMap.UdpClient;

public sealed class UdpClientManager
{
    private readonly IOptionsMonitor<UdpClientOptions> _options;
    private readonly EventBasedNetListener _listener;
    private readonly NetManager _client;

    private NetPeer? _peer;
    private TaskCompletionSource<NetPeer>? _connectedTcs;
    private TaskCompletionSource<PongPacket>? _pongTcs;

    private TaskCompletionSource<GetObjectsInAreaResponse>? _objectsTcs;
    private TaskCompletionSource<GetRegionsInAreaResponse>? _regionsTcs;
    private TaskCompletionSource<AddObjectResponse>? _addObjectTcs;

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
            Volatile.Write(ref _peer, peer);
            Console.WriteLine($"[UDP] Connected to {peer.Address}");

            var tcs = Volatile.Read(ref _connectedTcs);
            tcs?.TrySetResult(peer);
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
                        Volatile.Read(ref _pongTcs)?.TrySetResult(pong);
                        break;

                    case PacketType.GetObjectsInAreaResponse when message is GetObjectsInAreaResponse objResp:
                        Volatile.Read(ref _objectsTcs)?.TrySetResult(objResp);
                        break;

                    case PacketType.GetRegionsInAreaResponse when message is GetRegionsInAreaResponse regResp:
                        Volatile.Read(ref _regionsTcs)?.TrySetResult(regResp);
                        break;

                    case PacketType.AddObjectResponse when message is AddObjectResponse addResp:
                        _addObjectTcs?.TrySetResult(addResp);
                        break;

                    case PacketType.ObjectAdded when message is ObjectEventMessage added:
                        Console.WriteLine($"[UDP] ObjectAdded: {added.Id} at ({added.X},{added.Y}) {added.Width}x{added.Height}");
                        break;

                    case PacketType.ObjectUpdated when message is ObjectEventMessage updated:
                        Console.WriteLine($"[UDP] ObjectUpdated: {updated.Id} at ({updated.X},{updated.Y}) {updated.Width}x{updated.Height}");
                        break;

                    case PacketType.ObjectDeleted when message is ObjectEventMessage deleted:
                        Console.WriteLine($"[UDP] ObjectDeleted: {deleted.Id}");
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

    public async Task<(bool Success, GetObjectsInAreaResponse? Response, string? Error)> RequestObjectsInAreaAsync(
        int x1, int y1, int x2, int y2, TimeSpan timeout, CancellationToken ct)
    {
        try
        {
            var peer = await EnsureConnectedAsync(timeout, ct);
            if (peer == null)
                return (false, null, "Not connected");

            var req = new GetObjectsInAreaRequest { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2 };
            var bytes = PacketSerializer.Serialize(PacketType.GetObjectsInAreaRequest, req);

            var tcs = new TaskCompletionSource<GetObjectsInAreaResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            _objectsTcs = tcs;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeout);

            peer.Send(bytes, DeliveryMethod.ReliableOrdered);

            var completed = await Task.WhenAny(tcs.Task, Task.Delay(Timeout.Infinite, cts.Token));
            if (completed != tcs.Task)
                return (false, null, "Timeout waiting for GetObjectsInAreaResponse");

            return (true, await tcs.Task, null);
        }
        catch (OperationCanceledException)
        {
            return (false, null, "Canceled");
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
        finally
        {
            _objectsTcs = null;
        }
    }

    public async Task<(bool Success, GetRegionsInAreaResponse? Response, string? Error)> RequestRegionsInAreaAsync(
        int x1, int y1, int x2, int y2, TimeSpan timeout, CancellationToken ct)
    {
        try
        {
            var peer = await EnsureConnectedAsync(timeout, ct);
            if (peer == null)
                return (false, null, "Not connected");

            var req = new GetRegionsInAreaRequest { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2 };
            var bytes = PacketSerializer.Serialize(PacketType.GetRegionsInAreaRequest, req);

            var tcs = new TaskCompletionSource<GetRegionsInAreaResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            _regionsTcs = tcs;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeout);

            peer.Send(bytes, DeliveryMethod.ReliableOrdered);

            var completed = await Task.WhenAny(tcs.Task, Task.Delay(Timeout.Infinite, cts.Token));
            if (completed != tcs.Task)
                return (false, null, "Timeout waiting for GetRegionsInAreaResponse");

            return (true, await tcs.Task, null);
        }
        catch (OperationCanceledException)
        {
            return (false, null, "Canceled");
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
        finally
        {
            _regionsTcs = null;
        }
    }

    public async Task<(bool Success, AddObjectResponse? Response, string? Error)> AddObjectAsync(
        GameObjectDto obj, TimeSpan timeout, CancellationToken ct)
    {
        try
        {
            var peer = await EnsureConnectedAsync(timeout, ct);
            if (peer == null)
                return (false, null, "Not connected");

            var req = new AddObjectRequest
            {
                Id = obj.Id,
                X = obj.X,
                Y = obj.Y,
                Width = obj.Width,
                Height = obj.Height
            };

            var bytes = PacketSerializer.Serialize(PacketType.AddObjectRequest, req);

            var tcs = new TaskCompletionSource<AddObjectResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            _addObjectTcs = tcs;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeout);

            peer.Send(bytes, DeliveryMethod.ReliableOrdered);

            var completed = await Task.WhenAny(tcs.Task, Task.Delay(Timeout.Infinite, cts.Token));
            if (completed != tcs.Task)
                return (false, null, "Timeout waiting for AddObjectResponse");

            return (true, await tcs.Task, null);
        }
        catch (OperationCanceledException)
        {
            return (false, null, "Canceled");
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
        finally
        {
            _addObjectTcs = null;
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
