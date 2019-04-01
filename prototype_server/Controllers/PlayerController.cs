using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
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
        private readonly Dictionary<Guid, Player> _playersDictionary;

        public PlayerController(IServiceScope scope, RedisCache redis) : base(scope, redis)
        {
            _playerModel = scope.ServiceProvider.GetService<IRepository<Player>>();
            _playersDictionary = new Dictionary<Guid, Player>();
        }
        
        private void ResetPlayersStatus()
        {
            foreach(var (_, player) in _playersDictionary)
            {
                player.IsLocalPlayer = false;
                player.Moved = false;
            }
        }
        
        private void SyncWithConnectedPeer(NetPeer connectedPeer, bool onPeerConnected = true)
        {
            var deliveryMethod = onPeerConnected ? DeliveryMethod.ReliableOrdered : DeliveryMethod.Sequenced;
            var playerMoved = false;
            
            long peerEndpoint = BitConverter.ToUInt32(IPAddress.Parse($"{connectedPeer.EndPoint.Address}").GetAddressBytes(), 0);
            
            DataWriter.Reset();
            Array.Clear(DataWriter.Data, 0, DataWriter.Data.Length);
            
            DataWriter.Put((int)NET_DATA_TYPE.PlayerPositionsArray);
            
            foreach (var (_, player) in _playersDictionary)
            {
                long playerEndpoint = BitConverter.ToUInt32(IPAddress.Parse($"{player.Peer.EndPoint.Address}").GetAddressBytes(), 0);
                
                player.IsLocalPlayer = playerEndpoint == peerEndpoint;

                if (!onPeerConnected) // non-eventful sync (loop sync)
                {
                    if (player.IsLocalPlayer && !player.Moved) continue;
                    
                    playerMoved = true;
                }
                
                DataWriter.Put(playerEndpoint);
                
                DataWriter.Put(player.IsLocalPlayer);
                
                DataWriter.Put(player.X);
                DataWriter.Put(player.Y);
                DataWriter.Put(player.Z);
            }
            
            if (onPeerConnected || playerMoved)
            {
                connectedPeer.Send(DataWriter, deliveryMethod);
            }
        }

        public void SyncWithConnectedPeers()
        {
            if (_playersDictionary.Count == 0) return;
            
            foreach(var (_, player) in _playersDictionary)
            {
                // TODO: Will there be a null player in the dictionary at all?
                if(player == null)
                {
                    continue;
                }

                SyncWithConnectedPeer(player.Peer, false);
            }

            ResetPlayersStatus();
        }

        public void OnPeerConnected(NetPeer peer)
        {
#if DEBUG
            Console.WriteLine("[" + peer.Id + "] OnPeerConnected: " + peer.EndPoint.Address + ":" + peer.EndPoint.Port);
#endif
            var peerEndpointBytes = IPAddress.Parse($"{peer.EndPoint.Address}").GetAddressBytes();

            // GUID is exactly 16 bytes or and 36 character in length
            // IP address max size is 16 bytes,
            // therefore the generated guid size won't ever be more than 16 bytes
            var playerGuid = ConvertBytesToGuid(peerEndpointBytes);

            Player newPlayer = null;
            
            if (!_playersDictionary.ContainsKey(playerGuid))
            {
                newPlayer = new Player(peer)
                {
                    GUID = playerGuid,
                    Name = "user_" + new Random().Next(10000, 100000)
                };

                _playersDictionary.Add(playerGuid, newPlayer);
            }
            
            var cache = Redis.GetCache($"{playerGuid}");

            if (cache != null)
            {
#if DEBUG
                Console.WriteLine("Hit Cache");
#endif
                var strArr = cache.Split(',', StringSplitOptions.RemoveEmptyEntries);
                var coords = Array.ConvertAll(strArr, float.Parse);

                _playersDictionary[playerGuid].X = coords[0];
                _playersDictionary[playerGuid].Y = coords[1];
                _playersDictionary[playerGuid].Z = coords[2];
            }

            _playersDictionary[playerGuid].IsLocalPlayer = true;
            
            // Sync with local client
            SyncWithConnectedPeer(peer);

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
            
            var peerEndpointBytes = IPAddress.Parse($"{peer.EndPoint.Address}").GetAddressBytes();
            
            // GUID is exactly 16 bytes or and 36 character in length
            // IP address max size is 16 bytes,
            // therefore the generated guid size won't ever be more than 16 bytes
            var playerGuid = ConvertBytesToGuid(peerEndpointBytes);

            // Why checking this?!
            if (!_playersDictionary.ContainsKey(playerGuid)) return;

            var player = _playersDictionary[playerGuid];
            
            float[] coords = { player.X, player.Y, player.Z };
            
            Redis.SetCache($"{playerGuid}", string.Join(",", coords));
            
            _playersDictionary.Remove(playerGuid);
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
            
            var peerEndpointBytes = IPAddress.Parse($"{peer.EndPoint.Address}").GetAddressBytes();
            
            // GUID is exactly 16 bytes or and 36 character in length
            // IP address max size is 16 bytes,
            // therefore the generated guid size won't ever be more than 16 bytes
            var playerGuid = ConvertBytesToGuid(peerEndpointBytes);
            
            _playersDictionary[playerGuid].IsLocalPlayer = isLocalPlayer;
            
            _playersDictionary[playerGuid].X = x;
            _playersDictionary[playerGuid].Y = y;
            _playersDictionary[playerGuid].Z = z;

            Console.WriteLine(netDataType + " [" + playerGuid + "]: position packet: (x: " + x + ", y: " + y + ", z: " + z + ")");

            _playersDictionary[playerGuid].Moved = true;
        }
    }
}