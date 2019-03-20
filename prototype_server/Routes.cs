using System;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using prototype_server.Controllers;

namespace prototype_server
{
    public class Routes : RoutesBase
    {
        public NetManager ServerInstance;

        private readonly PlayerController _playerCtrl;
        
        public Routes()
        {
            _playerCtrl = new PlayerController();
        }

        public override void OnPeerConnected(NetPeer peer)
        {
            _playerCtrl.OnPeerConnected(peer);
        }

        public override void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            _playerCtrl.OnPeerDisconnected(peer, disconnectInfo);
        }

        public override void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            Console.WriteLine(endPoint.Address + ":" + endPoint.Port + " OnNetworkError: " + socketError);
        }

        public override void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            _playerCtrl.OnNetworkReceive(peer, reader, deliveryMethod);
        }

        public override void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            Console.WriteLine("OnNetworkReceiveUnconnected");
        }

        public override void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
//            Console.WriteLine("OnNetworkLatencyUpdate");
        }
        
        public override void OnConnectionRequest(ConnectionRequest request)
        {
            const int maxConn = 10;

            if (ServerInstance.PeersCount < maxConn)
            {
                request.AcceptIfKey("SomeConnectionKey");
            }
            else
            {
                request.Reject();
            }
        }

        public override void SyncWithConnectedClients()
        {
            _playerCtrl.SyncWithConnectedClients();
        }
    }
}