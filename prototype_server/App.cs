using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;

namespace prototype_server
{
    internal class App : INetEventListener
    {
        private enum NET_DATA_TYPE
        {
            PlayerPosition,
            PlayerPositionsArray,
        }

        private NetDataWriter _dataWriter;
        private NetManager _serverNetManager;

        private Dictionary<long, NetworkPlayer> _networkPlayersDictionary;

        public void Run()
        {
            _dataWriter = new NetDataWriter();
            _networkPlayersDictionary = new Dictionary<long, NetworkPlayer>();
            _serverNetManager = new NetManager(this);
            
            if (_serverNetManager.Start(15000))
            {
                Console.WriteLine("Server started listening on port 15000");
            }
            else
            {
                Console.WriteLine("Server could not start!");
                return;
            }

            while (_serverNetManager.IsRunning)
            {
                _serverNetManager.PollEvents();

                SendPlayerCoords();

                System.Threading.Thread.Sleep(15);
            }
        }
        
        private void SyncPlayersCoordsWithClient(NetPeer peer, long peerId = -1, bool onPeerConnected = true, bool onPeerMove = false)
        {
            var deliveryMethod = onPeerConnected ? DeliveryMethod.ReliableOrdered : DeliveryMethod.Sequenced;
            var playerMoved = false;

            _dataWriter.Reset();
            _dataWriter.Put((int)NET_DATA_TYPE.PlayerPositionsArray);
            
            foreach (var (key, value) in _networkPlayersDictionary)
            {
                if (onPeerMove)
                {
                    if(peerId == key || !value.moved)
                    {
                        continue;
                    }
                    
                    playerMoved = true;
                }
                
                _dataWriter.Put(key);

                _dataWriter.Put(value.x);
                _dataWriter.Put(value.y);
                _dataWriter.Put(value.z);
            }
            
            if (onPeerConnected || playerMoved)
            {
                peer.Send(_dataWriter, deliveryMethod);
            }
        }

        private void SendPlayerCoords()
        {
            foreach(var (key, value) in _networkPlayersDictionary)
            {
                if(value == null)
                {
                    continue;
                }

                SyncPlayersCoordsWithClient(value.netPeer, key, false, true);
            }

            foreach(var (_, value) in _networkPlayersDictionary)
            {
                value.moved = false;
            }
        }

        public void OnPeerConnected(NetPeer peer)
        {
            Console.WriteLine("[" + peer.Id + "] OnPeerConnected: " + peer.EndPoint.Address + ":" + peer.EndPoint.Port);

            SyncPlayersCoordsWithClient(peer);
            
            if (!_networkPlayersDictionary.ContainsKey(peer.Id))
            {
                _networkPlayersDictionary.Add(peer.Id, new NetworkPlayer(peer));
            }

            _networkPlayersDictionary[peer.Id].moved = true;
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Console.WriteLine(
                "[" + peer.Id + "] OnPeerDisconnected: " + 
                peer.EndPoint.Address + ":" + 
                peer.EndPoint.Port + 
                " - Reason: " + disconnectInfo.Reason
            );
            
            if (_networkPlayersDictionary.ContainsKey(peer.Id))
            {
                _networkPlayersDictionary.Remove(peer.Id);
            }
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            Console.WriteLine(endPoint.Address + ":" + endPoint.Port + " OnNetworkError: " + socketError);
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            if(reader.RawData == null)
            {
                return;
            }

            if (reader.RawDataSize - 3 != (sizeof(int) + sizeof(float) * 3)) return;
            
            var netDataType = (NET_DATA_TYPE)reader.GetInt();
            
            if (netDataType != NET_DATA_TYPE.PlayerPosition) return;
            
            var x = reader.GetFloat();
            var y = reader.GetFloat();
            var z = reader.GetFloat();

            _networkPlayersDictionary[peer.Id].x = x;
            _networkPlayersDictionary[peer.Id].y = y;
            _networkPlayersDictionary[peer.Id].z = z;

            Console.WriteLine("[" + peer.Id + "]: position packet: (x: " + x + ", y: " + y + ", z: " + z + ")");

            _networkPlayersDictionary[peer.Id].moved = true;
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            Console.WriteLine("OnNetworkReceiveUnconnected");
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
//            Console.WriteLine("OnNetworkLatencyUpdate");
        }

        public void OnConnectionRequest(ConnectionRequest request)
        {
            const int maxConn = 10;

            if (_serverNetManager.PeersCount < maxConn)
            {
                request.AcceptIfKey("SomeConnectionKey");
            }
            else
            {
                request.Reject();
            }
        }
    }
}
