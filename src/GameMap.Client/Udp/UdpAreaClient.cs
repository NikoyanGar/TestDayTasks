using System.Threading;
using System.Threading.Tasks;
using GameMap.Shared.Contracts;
using LiteNetLib;
using LiteNetLib.Utils;
using MemoryPack;

namespace GameMap.Client.Udp;

public static class UdpAreaClient
{
    public static Task<GetObjectsInAreaResponse> QueryObjectsAsync(string host, int port, GetObjectsInAreaRequest req, TimeSpan timeout)
        => QueryAsync<GetObjectsInAreaResponse>(host, port,
            NetMessageType.GetObjectsInAreaRequest,
            NetMessageType.GetObjectsInAreaResponse,
            MemoryPackSerializer.Serialize(req), timeout);

    public static Task<GetRegionsInAreaResponse> QueryRegionsAsync(string host, int port, GetRegionsInAreaRequest req, TimeSpan timeout)
        => QueryAsync<GetRegionsInAreaResponse>(host, port,
            NetMessageType.GetRegionsInAreaRequest,
            NetMessageType.GetRegionsInAreaResponse,
            MemoryPackSerializer.Serialize(req), timeout);

    private static async Task<T> QueryAsync<T>(string host, int port, NetMessageType requestKind, NetMessageType responseKind, byte[] payload, TimeSpan timeout)
    {
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

        var listener = new TempListener(
            onConnected: peer =>
            {
                var w = new NetDataWriter();
                w.Put((byte)requestKind);
                w.Put(payload);
                peer.Send(w, DeliveryMethod.ReliableOrdered);
            },
            onReceive: (peer, reader) =>
            {
                try
                {
                    var respKind = (NetMessageType)reader.GetByte();
                    var respPayload = reader.GetRemainingBytes();
                    if (respKind == responseKind)
                    {
                        var resp = MemoryPackSerializer.Deserialize<T>(respPayload)!;
                        tcs.TrySetResult(resp);
                    }
                }
                finally
                {
                    reader.Recycle();
                }
            },
            onDisconnected: _ => tcs.TrySetException(new IOException("Disconnected before response."))
        );

        var client = new NetManager(listener)
        {
            IPv6Enabled = false,
            UnsyncedEvents = true,
            AutoRecycle = true
        };

        try
        {
            if (!client.Start())
                throw new IOException("Failed to start UDP client.");

            client.Connect(host, port, "gamemap");

            using var cts = new CancellationTokenSource(timeout);
            while (!tcs.Task.IsCompleted)
            {
                client.PollEvents();
                if (cts.IsCancellationRequested)
                    throw new TimeoutException("UDP request timed out.");
                await Task.Delay(10, CancellationToken.None).ConfigureAwait(false);
            }

            return await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            client.Stop();
        }
    }

    private sealed class TempListener : INetEventListener
    {
        private readonly Action<NetPeer> _onConnected;
        private readonly Action<NetPeer, NetPacketReader> _onReceive;
        private readonly Action<NetPeer>? _onDisconnected;

        public TempListener(Action<NetPeer> onConnected,
            Action<NetPeer, NetPacketReader> onReceive,
            Action<NetPeer>? onDisconnected = null)
        {
            _onConnected = onConnected;
            _onReceive = onReceive;
            _onDisconnected = onDisconnected;
        }

        public void OnPeerConnected(NetPeer peer) => _onConnected(peer);
        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo info) => _onDisconnected?.Invoke(peer);
        public void OnNetworkError(System.Net.IPEndPoint endPoint, System.Net.Sockets.SocketError socketError) { }
        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod) => _onReceive(peer, reader);
        public void OnNetworkReceiveUnconnected(System.Net.IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }
        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
        public void OnConnectionRequest(ConnectionRequest request) => request.AcceptIfKey("gamemap");
    }
}