using System.Net;
using System.Net.Sockets;

using Microsoft.Extensions.DependencyInjection;
using LiteNetLib;

using prototype_config;
using prototype_server.Controllers;
using prototype_storage;
using prototype_services.Interfaces;

namespace prototype_server
{
    public class Routes : RoutesBase
    {
        public NetManager ServerInstance { private get; set; }

        private readonly ILogService _logService;
        
        private readonly PlayerController _playerCtrl;
        
        public Routes(ServiceConfiguration serviceConfig)
        {
            var scope = serviceConfig.ServiceProvider.CreateScope();
            var redisCache = serviceConfig.ServiceProvider.GetRequiredService<RedisCache>();
            
            _logService = serviceConfig.SharedServices.Log;
            _logService.LogScope = this;
            
            _playerCtrl = new PlayerController(scope, redisCache);
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
            _logService.Log(endPoint.Address + ":" + endPoint.Port + " OnNetworkError: " + socketError);
        }

        public override void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            _playerCtrl.OnNetworkReceive(peer, reader, deliveryMethod);
        }

        public override void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            _logService.Log("OnNetworkReceiveUnconnected");
        }

        public override void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
//            _logService.Log("OnNetworkLatencyUpdate");
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

        public override void SyncWithConnectedPeers()
        {
            _playerCtrl.SyncWithConnectedPeers();
        }
    }
}