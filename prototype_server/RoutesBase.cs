using System.Net;
using System.Net.Sockets;

#if DEBUG
    using prototype_server.Libs.LiteNetLib;
#else
    using LiteNetLib;
#endif

namespace prototype_server
{
    public abstract class RoutesBase : INetEventListener
    {
        public abstract void OnPeerConnected(NetPeer peer);
        public abstract void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo);
        public abstract void OnNetworkError(IPEndPoint endPoint, SocketError socketError);
        public abstract void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod);
        public abstract void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader,
            UnconnectedMessageType messageType);
        public abstract void OnNetworkLatencyUpdate(NetPeer peer, int latency);
        public abstract void OnConnectionRequest(ConnectionRequest request);
        public abstract void SyncWithConnectedClients();
    }
}