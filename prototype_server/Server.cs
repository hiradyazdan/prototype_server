using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;

namespace PrototypeServer
{
    internal class Server : INetEventListener
    {
        private enum NET_DATA_TYPE
        {
            PlayerPosition,
            PlayerPositionsArray,
        }

        NetDataWriter dataWriter;
        NetManager serverNetManager;

        Dictionary<long, NetworkPlayer> networkPlayersDictionary;
        
        private static void Main(string[] args)
        {
            var server = new Server();
            server.Run();

            Console.ReadKey();
        }

        private void Run()
        {
            dataWriter = new NetDataWriter();
            networkPlayersDictionary = new Dictionary<long, NetworkPlayer>();
            serverNetManager = new NetManager(this);
            if (serverNetManager.Start(15000))
            {
                Console.WriteLine("Server started listening on port 15000");
            }
            else
            {
                Console.WriteLine("Server could not start!");
                return;
            }

            while (serverNetManager.IsRunning)
            {
                serverNetManager.PollEvents();

                SendPlayerCoords();

                System.Threading.Thread.Sleep(15);
            }
        }
        
        private void SyncPlayersCoordsWithClient(NetPeer peer, long peerId = -1, bool onPeerConnected = true, bool onPeerMove = false)
        {
            var deliveryMethod = onPeerConnected ? DeliveryMethod.ReliableOrdered : DeliveryMethod.Sequenced;
            var playerMoved = false;

            dataWriter.Reset();
            dataWriter.Put((int)NET_DATA_TYPE.PlayerPositionsArray);
            
            foreach (var (key, value) in networkPlayersDictionary)
            {
                if (onPeerMove)
                {
                    if(peerId == key || !value.moved)
                    {
                        continue;
                    }
                    
                    playerMoved = true;
                }
                
                dataWriter.Put(key);

                dataWriter.Put(value.x);
                dataWriter.Put(value.y);
                dataWriter.Put(value.z);
            }
            
            if (onPeerConnected || playerMoved)
            {
                peer.Send(dataWriter, deliveryMethod);
            }
        }

        private void SendPlayerCoords()
        {
            foreach(var (key, value) in networkPlayersDictionary)
            {
                if(value == null)
                {
                    continue;
                }

                SyncPlayersCoordsWithClient(value.netPeer, key, false, true);
            }

            foreach(var (_, value) in networkPlayersDictionary)
            {
                value.moved = false;
            }
        }

        public void OnPeerConnected(NetPeer peer)
        {
            Console.WriteLine("[" + peer.Id + "] OnPeerConnected: " + peer.EndPoint.Address + ":" + peer.EndPoint.Port);

            SyncPlayersCoordsWithClient(peer);
            
            if (!networkPlayersDictionary.ContainsKey(peer.Id))
            {
                networkPlayersDictionary.Add(peer.Id, new NetworkPlayer(peer));
            }

            networkPlayersDictionary[peer.Id].moved = true;
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Console.WriteLine(
                "[" + peer.Id + "] OnPeerDisconnected: " + 
                peer.EndPoint.Address + ":" + 
                peer.EndPoint.Port + 
                " - Reason: " + disconnectInfo.Reason
            );
            
            if (networkPlayersDictionary.ContainsKey(peer.Id))
            {
                networkPlayersDictionary.Remove(peer.Id);
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

            networkPlayersDictionary[peer.Id].x = x;
            networkPlayersDictionary[peer.Id].y = y;
            networkPlayersDictionary[peer.Id].z = z;

            Console.WriteLine("[" + peer.Id + "]: position packet: (x: " + x + ", y: " + y + ", z: " + z + ")");

            networkPlayersDictionary[peer.Id].moved = true;
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

            if (serverNetManager.PeersCount < maxConn)
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
