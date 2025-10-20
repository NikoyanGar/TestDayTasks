using GameMap.Core;
using GameMap.Core.Layers.Objects;
using GameMap.Server.Options;
using GameMap.SharedContracts.Networking;
using GameMap.SharedContracts.Networking.Packets;
using LiteNetLib;
using LiteNetLib.Utils;
using MemoryPack;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace GameMap.Server.Services;

public sealed class UdpServerHostedService : IHostedService, INetEventListener
{
    private readonly ILogger<UdpServerHostedService> _logger;
    private readonly NetworkOptions _options;
    private readonly IMapManager _map;
    private NetManager? _server;
    private CancellationTokenSource? _cts;
    private Task? _pollTask;
    private readonly object peersLock = new();
    private readonly List<NetPeer> peers = new();


    public UdpServerHostedService(
        ILogger<UdpServerHostedService> logger,
        IOptions<NetworkOptions> options,
        IMapManager map)
    {
        _logger = logger;
        _options = options.Value;
        _map = map;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var netManager = new NetManager(this)
        {
            IPv6Enabled = _options.IPv6Enabled,
            UnconnectedMessagesEnabled = _options.UnconnectedMessagesEnabled,
            AutoRecycle = false,
            //MaxConnections = _options.MaxConnections
        };

        if (!netManager.Start(_options.Port))
            throw new InvalidOperationException($"Failed to start UDP server on port {_options.Port}.");

        _server = netManager;
        _logger.LogInformation("UDP server started on port {Port}. MaxConnections={Max}", _options.Port, _options.MaxConnections);

        _pollTask = Task.Run(() => PollLoopAsync(_cts.Token), CancellationToken.None);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _cts?.Cancel();
            if (_pollTask is not null)
                await _pollTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }
        finally
        {
            if (_server is not null)
            {
                _server.Stop();
                _logger.LogInformation("UDP server stopped.");
            }
        }
    }

    private async Task PollLoopAsync(CancellationToken ct)
    {
        var pollDelay = TimeSpan.FromMilliseconds(Math.Clamp(_options.PollIntervalMs, 1, 100));
        while (!ct.IsCancellationRequested && _server is not null)
        {
            _server.PollEvents();
            try
            {
                await Task.Delay(pollDelay, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
        }
    }

    public void OnPeerConnected(NetPeer peer)
    {
        lock (peersLock) { peers.Add(peer); }
        _logger.LogInformation("Peer connected: {EndPoint}", peer.Address);
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        lock (peersLock) { peers.Remove(peer); }
        _logger.LogInformation("Peer disconnected: {EndPoint}. Reason={Reason}", peer.Address, disconnectInfo.Reason);
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketErrorCode)
    {
        _logger.LogError("Network error: {Code} at {EndPoint}", socketErrorCode, endPoint);
    }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }

    public void OnConnectionRequest(ConnectionRequest request)
    {
        if (_server is null)
        {
            request.Reject();
            return;
        }

        if (_options.ConnectionKey is { Length: > 0 })
        {
            request.AcceptIfKey(_options.ConnectionKey);
            return;
        }

        if (_server.ConnectedPeersCount < _options.MaxConnections)
            request.Accept();
        else
            request.Reject();
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        try
        {
            var dataSpan = new ReadOnlySpan<byte>(reader.RawData, reader.UserDataOffset, reader.UserDataSize);
            if (!PacketSerializer.TryDeserialize(dataSpan, out var type, out var message))
                return;

            switch (type)
            {
                case PacketType.Ping:
                {
                    var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    var response = new PongPacket(now);
                    var bytes = PacketSerializer.Serialize(PacketType.Pong, response);
                    peer.Send(bytes, DeliveryMethod.Unreliable);
                    break;
                }

                case PacketType.GetObjectsInAreaRequest when message is GetObjectsInAreaRequest reqObj:
                {
                    var objects = _map.GetObjectsInArea(reqObj.X1, reqObj.Y1, reqObj.X2, reqObj.Y2);
                    var dto = new GetObjectsInAreaResponse
                    {
                        Objects = objects.Select(o => new GameObjectDto
                        {
                            Id = o.Id,
                            X = o.X,
                            Y = o.Y,
                            Width = o.Width,
                            Height = o.Height
                        }).ToList()
                    };

                    var bytes = PacketSerializer.Serialize(PacketType.GetObjectsInAreaResponse, dto);
                    peer.Send(bytes, DeliveryMethod.ReliableOrdered);
                    break;
                }

                case PacketType.GetRegionsInAreaRequest when message is GetRegionsInAreaRequest reqReg:
                {
                    // Translate (x1,y1,x2,y2) to (x,y,width,height)
                    var x = Math.Min(reqReg.X1, reqReg.X2);
                    var y = Math.Min(reqReg.Y1, reqReg.Y2);
                    var width = Math.Abs(reqReg.X2 - reqReg.X1);
                    var height = Math.Abs(reqReg.Y2 - reqReg.Y1);
                    if (width == 0) width = 1;
                    if (height == 0) height = 1;

                    var regions = _map.GetRegionsInArea(x, y, width, height);
                    var dto = new GetRegionsInAreaResponse
                    {
                        Regions = regions.Select(r => new RegionDto
                        {
                            Id = r.Id,
                            Name = r.Name
                        }).ToList()
                    };

                    var bytes = PacketSerializer.Serialize(PacketType.GetRegionsInAreaResponse, dto);
                    peer.Send(bytes, DeliveryMethod.ReliableOrdered);
                    break;
                }

                case PacketType.AddObjectRequest when message is AddObjectRequest addReq:
                {
                    var obj = new MapObject(addReq.Id, addReq.X, addReq.Y, addReq.Width, addReq.Height);
                    var ok = _map.TryPlaceObject(obj, occupyTile: null);

                    var resp = new AddObjectResponse
                    {
                        Success = ok,
                        Error = ok ? null : "Could not place object in the requested area."
                    };

                    var bytes = PacketSerializer.Serialize(PacketType.AddObjectResponse, resp);
                    peer.Send(bytes, DeliveryMethod.ReliableOrdered);

                    if (ok && _server is not null)
                    {
                        var evt = new ObjectEventMessage
                        {
                            Id = obj.Id,
                            X = obj.X,
                            Y = obj.Y,
                            Width = obj.Width,
                            Height = obj.Height
                        };
                        var evtBytes = PacketSerializer.Serialize(PacketType.ObjectAdded, evt);
                        foreach (var p in _server.ConnectedPeerList)
                            p.Send(evtBytes, DeliveryMethod.ReliableOrdered);
                    }
                    break;
                }

                default:
                    break;
            }
        }
        finally
        {
            reader.Recycle();
        }
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        reader.Recycle();
    }

    public void BroadcastObjectEvent(PacketType type, ObjectEventMessage ev)
    {
        if (type != PacketType.ObjectAdded && type != PacketType.ObjectUpdated && type != PacketType.ObjectDeleted)
            throw new ArgumentException("Invalid event type");

        var body = MemoryPackSerializer.Serialize(ev);
        var toSend = new byte[1 + body.Length];
        toSend[0] = (byte)type;
        Array.Copy(body, 0, toSend, 1, body.Length);

        lock (peersLock)
        {
            foreach (var p in peers.ToList())
            {
                if (p.ConnectionState == ConnectionState.Connected)
                    p.Send(toSend, DeliveryMethod.ReliableOrdered);
            }
        }
    }
}