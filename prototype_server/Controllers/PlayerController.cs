using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using prototype_server.DB;
using prototype_server.Models;

#if DEBUG
    using prototype_server.Libs.LiteNetLib;
#else
    using LiteNetLib;
#endif

namespace prototype_server.Controllers
{   
    public class PlayerController : ApplicationController
    {   
        private enum NET_DATA_TYPE
        {
            PlayerPosition,
            PlayerPositionsArray,
        }

        private readonly IRepository<Player> _playerModel;
        private readonly Dictionary<long, Player> _playersDictionary;

        public PlayerController(IServiceScope scope, RedisCache redis) : base(scope, redis)
        {
            _playerModel = scope.ServiceProvider.GetService<IRepository<Player>>();
            _playersDictionary = new Dictionary<long, Player>();
        }
        
        private void ResetPlayersStatus()
        {
            foreach(var (_, player) in _playersDictionary)
            {
                player.IsLocalPlayer = false;
                player.Moved = false;
            }
        }
        
        private void SyncWithConnectedClient(NetPeer peer, long peerId = -1, bool onPeerConnected = true)
        {
            var deliveryMethod = onPeerConnected ? DeliveryMethod.ReliableOrdered : DeliveryMethod.Sequenced;
            var playerMoved = false;
            
            DataWriter.Reset();
            Array.Clear(DataWriter.Data, 0, DataWriter.Data.Length);
            
            DataWriter.Put((int)NET_DATA_TYPE.PlayerPositionsArray);
            
            foreach (var (key, player) in _playersDictionary)
            {
                if (!onPeerConnected)
                {
                    if(!player.Moved)
                    {
                        continue;
                    }
                    
                    player.IsLocalPlayer = key == peerId;
                    
                    playerMoved = true;
                }
                
                DataWriter.Put(key);
                
                DataWriter.Put(player.IsLocalPlayer);
                
                DataWriter.Put(player.X);
                DataWriter.Put(player.Y);
                DataWriter.Put(player.Z);
            }
            
            if (onPeerConnected || playerMoved)
            {   
                peer.Send(DataWriter, deliveryMethod);
            }
        }

        public void SyncWithConnectedClients()
        {
            if (_playersDictionary.Count == 0) return;
            
            foreach(var (peerId, player) in _playersDictionary)
            {
                // TODO: Will there be a null player in the dictionary at all?
                if(player == null)
                {
                    continue;
                }

                SyncWithConnectedClient(player.Peer, peerId, false);
            }

            ResetPlayersStatus();
        }

        public void OnPeerConnected(NetPeer peer)
        {
#if DEBUG
            Console.WriteLine("[" + peer.Id + "] OnPeerConnected: " + peer.EndPoint.Address + ":" + peer.EndPoint.Port);
#endif
            Player newPlayer = null;
            
            if (!_playersDictionary.ContainsKey(peer.Id))
            {
                newPlayer = new Player(peer)
                {
                    Name = "user_" + new Random().Next(10000, 20000)
                };

                _playersDictionary.Add(peer.Id, newPlayer);
            }

            var cache = Redis.GetCache($"{peer.Id}");
            
            if (cache != null)
            {
#if DEBUG
                Console.WriteLine("Hit Cache");
#endif          
                var strArr = cache.Split(',', StringSplitOptions.RemoveEmptyEntries);
                var coords = Array.ConvertAll(strArr, float.Parse);
                
                _playersDictionary[peer.Id].X = coords[0];
                _playersDictionary[peer.Id].Y = coords[1];
                _playersDictionary[peer.Id].Z = coords[2];
            }
            
            _playersDictionary[peer.Id].IsLocalPlayer = true;
            
            SyncWithConnectedClient(peer);

            if (cache != null) return;
            
            _playerModel.Create(newPlayer);
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Console.WriteLine(
                "[" + peer.Id + "] OnPeerDisconnected: " + 
                peer.EndPoint.Address + ":" + 
                peer.EndPoint.Port + 
                " - Reason: " + disconnectInfo.Reason
            );

            // Why checking this?!
            if (!_playersDictionary.ContainsKey(peer.Id)) return;

            var player = _playersDictionary[peer.Id];
            
            float[] coords = { player.X, player.Y, player.Z };
            
            Redis.SetCache($"{peer.Id}", string.Join(",", coords));
            
            _playersDictionary.Remove(peer.Id);
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            const int deliveryMethodHeaderSize = 3;
            
            // Not sure if there's any point for this as it will always be a byte[] with some size
            if(reader.RawData == null) return;
            if (reader.RawDataSize - deliveryMethodHeaderSize != sizeof(int) + sizeof(bool) + sizeof(float) * 3) return;
            
            var netDataType = (NET_DATA_TYPE)reader.GetInt();
            
            if (netDataType != NET_DATA_TYPE.PlayerPosition) return;

            var isLocalPlayer = reader.GetBool();
            
            var x = reader.GetFloat();
            var y = reader.GetFloat();
            var z = reader.GetFloat();

            _playersDictionary[peer.Id].IsLocalPlayer = isLocalPlayer;
            
            _playersDictionary[peer.Id].X = x;
            _playersDictionary[peer.Id].Y = y;
            _playersDictionary[peer.Id].Z = z;

            Console.WriteLine(netDataType + " [" + peer.Id + "]: position packet: (x: " + x + ", y: " + y + ", z: " + z + ")");

            _playersDictionary[peer.Id].Moved = true;
        }
    }
}