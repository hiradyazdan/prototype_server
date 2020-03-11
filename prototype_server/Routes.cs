using System.Net;
using System.Net.Sockets;

using prototype_config;
using prototype_services.Common.Network;
using prototype_services.Interfaces;

namespace prototype_server
{
    using Controllers;
    
    public class Routes : _BaseRoutes, INetEventListenerAdapter
    {
        private readonly RelayController _relayCtrl;
        private readonly GameController _gameCtrl;
        
        public Routes(ServiceConfiguration serviceConfig) : base(serviceConfig)
        {
            _relayCtrl = new RelayController();
            _gameCtrl = new GameController();
        }
        
        public void Start()
        {
            if (RelayService.StartNetManager())
            {
                LogService.Log($"Server started listening on port {RelayService.UdpPort}");
                
                _gameCtrl.StoreAppData();
                _gameCtrl.Start();
            }
            else
            {
                LogService.LogError("Server could not start!");
            }
        }
        
        public void FixedUpdate()
        {
            RelayService.PollEvents(this);
            
            _gameCtrl.FixedUpdate();
        }
        
        public void OnPeerConnected(object peer)
        {
            _relayCtrl.OnPeerConnected(peer);
        }
        
        public void OnPeerDisconnected(object peer, object disconnectInfo)
        {
            _relayCtrl.OnPeerDisconnected(peer, disconnectInfo);
        }
        
        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            _relayCtrl.OnNetworkError(endPoint, socketError);
        }
        
        public void OnNetworkReceive(object peer, object reader, DeliveryMethods deliveryMethod)
        {
            _relayCtrl.OnNetworkReceive(peer, reader, deliveryMethod);
        }
        
        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, object reader, UnconnectedMessageTypes messageType)
        {
            _relayCtrl.OnNetworkReceiveUnconnected(remoteEndPoint, reader, messageType);
        }
        
        public void OnNetworkLatencyUpdate(object peer, int latency)
        {
            _relayCtrl.OnNetworkLatencyUpdate(peer, latency);
        }
        
        public void OnConnectionRequest(object request)
        {
            _relayCtrl.OnConnectionRequest(request);
        }
    }
}