using System;
using System.Net;
using System.Net.Sockets;
using GameMap.Server.Options;
using GameMap.SharedContracts.Networking;
using GameMap.SharedContracts.Networking.Packets;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameMap.Server.Services;

public sealed class UdpServerHostedService : IHostedService, INetEventListener
{
    private readonly ILogger<UdpServerHostedService> _logger;
    private readonly NetworkOptions _options;
    private NetManager? _server;
    private CancellationTokenSource? _cts;
    private Task? _pollTask;

    public UdpServerHostedService(ILogger<UdpServerHostedService> logger, IOptions<NetworkOptions> options)
    {
        _logger = logger;
        _options = options.Value;
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
        _logger.LogInformation("Peer connected: {EndPoint}", peer.Address);
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
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
}