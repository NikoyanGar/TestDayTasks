using System.Net;

namespace GameMap.Network.Udp
{
    public class MapUdpServer : INetEventListener, IDisposable
    {
        private readonly NetManager server;
        private readonly object peersLock = new();
        private readonly List<NetPeer> peers = new();

        private readonly IMapQueryProvider mapQueryProvider;
        private readonly ReaderWriterLockSlim rwLock = new();

        public MapUdpServer(IMapQueryProvider provider, int port = 9050)
        {
            mapQueryProvider = provider ?? throw new ArgumentNullException(nameof(provider));

            server = new NetManager(this) { AutoRecycle = true };
            server.Start(port);
            Console.WriteLine($"UDP listen on port: {port}");
        }

        public void RunPollLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                PollEvents();
                Thread.Sleep(5);
            }
        }

        public void PollEvents() => server.PollEvents();

        public void Stop()
        {
            server.Stop();
        }

        public void Dispose()
        {
            Stop();
            rwLock?.Dispose();
        }

        public void OnPeerConnected(NetPeer peer)
        {
            Console.WriteLine($" {nameof(OnPeerConnected)} : {peer.Address}");
            lock (peersLock) { peers.Add(peer); }
        }
        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            lock (peersLock) { peers.Remove(peer); }
        }
        public void OnNetworkError(IPEndPoint endPoint, System.Net.Sockets.SocketError socketError)
        {
            // TODO:log
        }
        public void OnConnectionRequest(ConnectionRequest request)
        {
            request.Accept();
        }
        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            try
            {
                var len = reader.AvailableBytes;
                if (len <= 0) return;

                var buffer = reader.GetRemainingBytes();
                var type = (NetMessageType)buffer[0];
                var payload = buffer.AsSpan(1).ToArray();

                switch (type)
                {
                    case NetMessageType.GetObjectsInAreaRequest:
                        HandleGetObjectsRequest(peer, payload);
                        break;
                    case NetMessageType.GetRegionsInAreaRequest:
                        HandleGetRegionsRequest(peer, payload);
                        break;
                    default:
                        Console.WriteLine($"unhandled: {type}");
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
            if (messageType == UnconnectedMessageType.Broadcast)
            {
                var data = reader.GetRemainingBytes();
                Console.WriteLine($"Broadcast from {remoteEndPoint}, Lenght {data.Length}");

                var reply = System.Text.Encoding.UTF8.GetBytes("Server received broadcast");
                server.SendUnconnectedMessage(reply, remoteEndPoint);
            }
            reader.Recycle();
        }

        public void BroadcastObjectEvent(NetMessageType type, ObjectEventMessage ev)
        {
            if (type != NetMessageType.ObjectAdded && type != NetMessageType.ObjectUpdated && type != NetMessageType.ObjectDeleted)
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

        private void HandleGetObjectsRequest(NetPeer peer, byte[] payload)
        {
            var req = MemoryPackSerializer.Deserialize<GetObjectsInAreaRequest>(payload);
            rwLock.EnterReadLock();
            try
            {
                var objects = mapQueryProvider.GetObjectsInArea(req.X1, req.Y1, req.X2, req.Y2);
                //TODO: 
                var body = MemoryPackSerializer.Serialize(new object());
                var send = new byte[1 + body.Length];
                send[0] = (byte)NetMessageType.GetObjectsInAreaResponse;
                Array.Copy(body, 0, send, 1, body.Length);
                peer.Send(send, DeliveryMethod.ReliableOrdered);
            }
            finally { rwLock.ExitReadLock(); }
        }

        private void HandleGetRegionsRequest(NetPeer peer, byte[] payload)
        {
            var req = MemoryPackSerializer.Deserialize<GetRegionsInAreaRequest>(payload);
            rwLock.EnterReadLock();
            try
            {
                var regions = mapQueryProvider.GetRegionsInArea(req.X1, req.Y1, req.X2, req.Y2);

                var body = MemoryPackSerializer.Serialize(regions.Select(r=>new RegionDto()));
                var send = new byte[1 + body.Length];
                send[0] = (byte)NetMessageType.GetRegionsInAreaResponse;
                Array.Copy(body, 0, send, 1, body.Length);
                peer.Send(send, DeliveryMethod.ReliableOrdered);
            }
            finally { rwLock.ExitReadLock(); }
        }
    }
}
