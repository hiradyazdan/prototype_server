using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using LiteNetLib;
using UnityEngine;

using prototype_server.DB;
using prototype_server.Models;
using prototype_server.Serializers;

namespace prototype_server.Controllers
{   
    public class PlayerController : ApplicationController
    {
        private enum PacketTypes
        {
            Position,
            Positions
        }
        
        private enum ObjectTypes
        {
            Player,
            Enemy
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
                player.IsLocal = false;
                player.Moved = false;
            }
        }
        
        private void SyncWithConnectedPeer(NetPeer connectedPeer, bool onPeerConnected = true)
        {
            var deliveryMethod = onPeerConnected ? DeliveryMethod.ReliableOrdered : DeliveryMethod.Sequenced; // 3 bytes
            var playerMoved = false;
            
            long peerEndpoint = BitConverter.ToUInt32(IPAddress.Parse($"{connectedPeer.EndPoint.Address}").GetAddressBytes(), 0);
            
            DataWriter.Reset();
            Array.Clear(DataWriter.Data, 0, DataWriter.Data.Length);
            
            DataWriter.Put((int)PacketTypes.Positions); // 4 bytes

            foreach (var (_, player) in _playersDictionary)
            {
                long playerEndpoint =
                    BitConverter.ToUInt32(IPAddress.Parse($"{player.Peer.EndPoint.Address}").GetAddressBytes(), 0);

                player.IsLocal = playerEndpoint == peerEndpoint;

                if (!onPeerConnected) // non-eventful sync (loop sync)
                {
                    if (player.IsLocal && !player.Moved) continue;

                    playerMoved = true;
                }
                
                // serialized size: 30 (3 + 4 + 23)
                // un-serialized size: 32 (3 + 4 + 25)
                SetDataWriter(player, playerEndpoint);
            }
            
            if (onPeerConnected || playerMoved)
            {
                connectedPeer.Send(DataWriter, deliveryMethod);
            }
        }

        private void SetDataWriter(Player player, long playerEndpoint)
        {
            if (IsSerialized)
            {
//                _BasePacket.IsCompressed = false;

                var positionPacket = new PositionPacket(
                    (int) ObjectTypes.Player, 
                    playerEndpoint, 
                    player.IsLocal, 
                    player.X, 
                    player.Y, 
                    player.Z
                );
                
                var packetBytes = positionPacket.ToByteArray(); // 23 bytes

                DataWriter.Put(packetBytes);
            }
            else
            {
#if DEBUG
                // un-serialized total: 25 bytes
                    
                DataWriter.Put((int) ObjectTypes.Player); // 4 bytes
                    
                DataWriter.Put(playerEndpoint); // 8 bytes

                DataWriter.Put(player.IsLocal); // 1 byte

                DataWriter.Put(player.X); // 4 bytes
                DataWriter.Put(player.Y); // 4 bytes
                DataWriter.Put(player.Z); // 4 bytes
#endif
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
            // GUID is exactly 16 bytes or and 36 character in length
            // IP address max size is 16 bytes,
            // therefore the generated guid size won't ever be more than 16 bytes
            var playerGuid = ConvertBytesToGuid(peer.EndPoint.Address.GetAddressBytes());

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

            _playersDictionary[playerGuid].IsLocal = true;
            
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
            
            // GUID is exactly 16 bytes or and 36 character in length
            // IP address max size is 16 bytes,
            // therefore the generated guid size won't ever be more than 16 bytes
            var playerGuid = ConvertBytesToGuid(peer.EndPoint.Address.GetAddressBytes());

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
            if (reader.RawDataSize - deliveryMethodHeaderSize != sizeof(int) * 2 + sizeof(long) + sizeof(bool) + sizeof(float) * 3) return;
            
            var packetType = (PacketTypes)reader.GetInt(); // 4 bytes
            
            if (packetType != PacketTypes.Position) return;
            
            ObjectTypes objectType;
            bool isLocal;
            float x, y, z;

            if (IsSerialized)
            {
                var positionPacket = new PositionPacket(reader.GetRemainingBytes());
                
                // serialized total: 17 - 2 bytes
                
                objectType = (ObjectTypes) positionPacket.ObjectType;
                isLocal = positionPacket.IsLocal;
            
                x = positionPacket.X;
                y = positionPacket.Y;
                z = positionPacket.Z;
            }
            else
            {
                var positionPacket = new PositionPacket(reader);
                
                // un-serialized total: 17 bytes
                
                objectType = (ObjectTypes) positionPacket.ObjectType; // 4 bytes
                isLocal = positionPacket.IsLocal; // 1 byte
            
                x = positionPacket.X; // 4 bytes
                y = positionPacket.Y; // 4 bytes
                z = positionPacket.Z; // 4 bytes
            }
            
            // GUID is exactly 16 bytes or and 36 character in length
            // IP address max size is 16 bytes,
            // therefore the generated guid size won't ever be more than 16 bytes
            var playerGuid = ConvertBytesToGuid(peer.EndPoint.Address.GetAddressBytes());
            
            Console.WriteLine(packetType + " [" + playerGuid + "]: position packet: (x: " + x + ", y: " + y + ", z: " + z + ")");
            
//            _playersDictionary[playerGuid]
            _playersDictionary[playerGuid].IsLocal = isLocal;
            
            _playersDictionary[playerGuid].X = x;
            _playersDictionary[playerGuid].Y = y;
            _playersDictionary[playerGuid].Z = z;
            
            _playersDictionary[playerGuid].Moved = true;
        }
    }
}