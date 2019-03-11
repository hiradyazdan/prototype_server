using System;
using System.Collections.Generic;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;
using prototype_server.DB;

namespace prototype_server
{
    public class App : INetEventListener
    {
        private enum NET_DATA_TYPE
        {
            PlayerPosition,
            PlayerPositionsArray,
        }

        private readonly RedisCache _redisCache;
        private NetDataWriter _dataWriter;
        private NetManager _serverNetManager;

        private Dictionary<long, NetworkPlayer> _networkPlayersDictionary;

        public App(RedisCache redisCache)
        {
            _redisCache = redisCache;
        }
        
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
                Console.Error.WriteLine("Server could not start!");
                return;
            }

            while (_serverNetManager.IsRunning)
            {
                _serverNetManager.PollEvents();

                OnStateChange();

                Thread.Sleep(15);
            }
        }

        private void OnStateChange()
        {
            SendPlayersCoords();
        }
        
        private void SyncPlayersCoordsWithClient(NetPeer peer, long peerId = -1, bool onPeerConnected = true)
        {
            if (_networkPlayersDictionary.Count == 0) return;
            
            var deliveryMethod = onPeerConnected ? DeliveryMethod.ReliableOrdered : DeliveryMethod.Sequenced;
            var playerMoved = false;
            
            _dataWriter.Reset();
            _dataWriter.Put((int)NET_DATA_TYPE.PlayerPositionsArray);
            
            foreach (var (key, netPlayer) in _networkPlayersDictionary)
            {
                if (!onPeerConnected)
                {
                    if(!netPlayer.moved)
                    {
                        continue;
                    }
                    
                    netPlayer.IsLocalPlayer = key == peerId;
                    
                    playerMoved = true;
                }
                
                _dataWriter.Put(key);
                
                _dataWriter.Put(netPlayer.IsLocalPlayer);
                
                _dataWriter.Put(netPlayer.x);
                _dataWriter.Put(netPlayer.y);
                _dataWriter.Put(netPlayer.z);
            }
            
            if (onPeerConnected || playerMoved)
            {   
                peer.Send(_dataWriter, deliveryMethod);
            }
        }

        private void SendPlayersCoords()
        {
            if (_networkPlayersDictionary.Count == 0) return;
            
            foreach(var (peerId, netPlayer) in _networkPlayersDictionary)
            {
                if(netPlayer == null)
                {
                    continue;
                }

                SyncPlayersCoordsWithClient(netPlayer.netPeer, peerId, false);
            }

            foreach(var (_, netPlayer) in _networkPlayersDictionary)
            {
                netPlayer.IsLocalPlayer = false;
                netPlayer.moved = false;
            }
        }

        public void OnPeerConnected(NetPeer peer)
        {
#if DEBUG
            Console.WriteLine("[" + peer.Id + "] OnPeerConnected: " + peer.EndPoint.Address + ":" + peer.EndPoint.Port);
#endif      
            if (!_networkPlayersDictionary.ContainsKey(peer.Id))
            {
                _networkPlayersDictionary.Add(peer.Id, new NetworkPlayer(peer));
            }
            
            var cache = _redisCache.GetCache($"{peer.Id}");
            
            if (cache != null)
            {
#if DEBUG
                Console.WriteLine("Hit Cache");
#endif          
                var strArr = cache.Split(',', StringSplitOptions.RemoveEmptyEntries);
                var coords = Array.ConvertAll(strArr, float.Parse);
                
                _networkPlayersDictionary[peer.Id].x = coords[0];
                _networkPlayersDictionary[peer.Id].y = coords[1];
                _networkPlayersDictionary[peer.Id].z = coords[2];
            }
            
            _networkPlayersDictionary[peer.Id].moved = true;
            _networkPlayersDictionary[peer.Id].IsLocalPlayer = true;
            
            SyncPlayersCoordsWithClient(peer);
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Console.WriteLine(
                "[" + peer.Id + "] OnPeerDisconnected: " + 
                peer.EndPoint.Address + ":" + 
                peer.EndPoint.Port + 
                " - Reason: " + disconnectInfo.Reason
            );

            if (!_networkPlayersDictionary.ContainsKey(peer.Id)) return;

            var netPlayer = _networkPlayersDictionary[peer.Id];
            
            float[] coords = { netPlayer.x, netPlayer.y, netPlayer.z };
            
            _redisCache.SetCache($"{peer.Id}", string.Join(",", coords));
            
            _networkPlayersDictionary.Remove(peer.Id);
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            Console.WriteLine(endPoint.Address + ":" + endPoint.Port + " OnNetworkError: " + socketError);
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            if(reader.RawData == null) return;

            if (reader.RawDataSize - 3 != sizeof(int) + sizeof(bool) + sizeof(float) * 3) return;
            
            var netDataType = (NET_DATA_TYPE)reader.GetInt();
            
            if (netDataType != NET_DATA_TYPE.PlayerPosition) return;

            var isLocalPlayer = reader.GetBool();
            
            var x = reader.GetFloat();
            var y = reader.GetFloat();
            var z = reader.GetFloat();

            _networkPlayersDictionary[peer.Id].IsLocalPlayer = isLocalPlayer;
            
            _networkPlayersDictionary[peer.Id].x = x;
            _networkPlayersDictionary[peer.Id].y = y;
            _networkPlayersDictionary[peer.Id].z = z;

            Console.WriteLine(netDataType + " [" + peer.Id + "]: position packet: (x: " + x + ", y: " + y + ", z: " + z + ")");

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
