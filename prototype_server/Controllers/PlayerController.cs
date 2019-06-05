using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using LiteNetLib;

using prototype_server.Config;
using prototype_server.DB;
using prototype_server.Models;
using prototype_server.Serializers;

namespace prototype_server.Controllers
{   
    public class PlayerController : ApplicationController
    {
        private readonly IRepository<Player> _playerModel;
        private readonly Dictionary<Guid, Player> _playersDictionary;
        
        private int _syncCount;

        public PlayerController(IServiceScope scope, RedisCache redis) : base(scope, redis)
        {
            _playerModel = scope.ServiceProvider.GetService<IRepository<Player>>();
            _playersDictionary = new Dictionary<Guid, Player>();
        }
        
        public void OnPeerConnected(NetPeer peer)
        {
#if DEBUG
            Console.WriteLine("[" + peer.Id + "] OnPeerConnected: " + peer.EndPoint.Address + ":" + peer.EndPoint.Port);
#endif
            
            var addressBytes = peer.EndPoint.Address.GetAddressBytes();
#if DEBUG
            var portBytes = BitConverter.GetBytes(peer.EndPoint.Port);
            var playerGuid = ConvertBytesToGuid(addressBytes.Concat(portBytes).ToArray());
#else
            // GUID is exactly 16 bytes or and 36 character in length
            // IP address max size is 16 bytes,
            // therefore the generated guid size won't ever be more than 16 bytes
            var playerGuid = ConvertBytesToGuid(addressBytes);
#endif
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
            
            _playersDictionary[playerGuid].IsLocal = true;
            _playersDictionary[playerGuid].ActionType = ActionTypes.Spawn;
            
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
            
            // Sync with local client
            SyncWithConnectedPeer(peer);
            
            if (cache != null || newPlayer == null) return;
            
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
            
            var addressBytes = peer.EndPoint.Address.GetAddressBytes();
#if DEBUG
            var portBytes = BitConverter.GetBytes(peer.EndPoint.Port);
            var playerGuid = ConvertBytesToGuid(addressBytes.Concat(portBytes).ToArray());
#else
            // GUID is exactly 16 bytes or and 36 character in length
            // IP address max size is 16 bytes,
            // therefore the generated guid size won't ever be more than 16 bytes
            var playerGuid = ConvertBytesToGuid(addressBytes);
#endif
            
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
            const int expectedPacketSize = sizeof(int) * 3 + sizeof(long) + sizeof(bool) + sizeof(float) * 3;
            
            // Not sure if there's any point for this as it will always be a byte[] with some size
            if (reader.RawData == null) return;
            if (reader.RawDataSize - deliveryMethodHeaderSize != expectedPacketSize) return;
            
            var packetType = (PacketTypes) reader.GetInt(); // 4 bytes
            
            if (packetType != PacketTypes.Position) return;
            
            var positionPacket = IsSerialized 
                ? new PositionPacket(reader.GetRemainingBytes()) // serialized total: 17 - 2 bytes
                : new PositionPacket(reader); // un-serialized total: 17 bytes
            
            var actionType = (ActionTypes) positionPacket.ActionType;
            var objectType = (ObjectTypes) positionPacket.ObjectType;
            
            var isLocal = positionPacket.IsLocal;
            
            var x = positionPacket.X;
            var y = positionPacket.Y;
            var z = positionPacket.Z;
            
            var peerEndpoint = peer.EndPoint;
            var addressBytes = peerEndpoint.Address.GetAddressBytes();
#if DEBUG
            var portBytes = BitConverter.GetBytes(peerEndpoint.Port);
            var playerGuid = ConvertBytesToGuid(addressBytes.Concat(portBytes).ToArray());
            
            Console.WriteLine(
                $"{packetType} [{peerEndpoint.Address}:{peerEndpoint.Port}]: " +
                $"(x: {x}, y: {y}, z: {z})"
            );
#else
            // GUID is exactly 16 bytes or and 36 character in length
            // IP address max size is 16 bytes,
            // therefore the generated guid size won't ever be more than 16 bytes
            var playerGuid = ConvertBytesToGuid(addressBytes);
            
            Console.WriteLine(packetType + " [" + playerGuid + "]: position packet: (x: " + x + ", y: " + y + ", z: " + z + ")");
#endif
            var isSpawned = actionType == ActionTypes.Spawn;
            var isMoved = actionType == ActionTypes.Move;
            
            _playersDictionary[playerGuid].Spawned = isSpawned;
            _playersDictionary[playerGuid].Moved = isMoved;
            _playersDictionary[playerGuid].Idle = !isSpawned && !isMoved;
            
            /**
             * Packet Data
             */
            
            _playersDictionary[playerGuid].ActionType = actionType;
            _playersDictionary[playerGuid].ObjectType = objectType;
            
            _playersDictionary[playerGuid].IsLocal = isLocal;
            
            _playersDictionary[playerGuid].X = x;
            _playersDictionary[playerGuid].Y = y;
            _playersDictionary[playerGuid].Z = z;
        }
        
        /**
         * Non-Eventful Sync (Loop Sync)
         */
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
        
        private void SyncWithConnectedPeer(NetPeer connectedPeer, bool onPeerConnected = true)
        {
            var deliveryMethod = onPeerConnected ? DeliveryMethod.ReliableOrdered : DeliveryMethod.Sequenced; // 3 bytes
            var playerIdle = true;
            
            var peerEndpoint = connectedPeer.EndPoint;
            var peerAddressBytes = peerEndpoint.Address.GetAddressBytes();
#if DEBUG
            var peerPortBytes = BitConverter.GetBytes(peerEndpoint.Port);
            var peerEndpointBytes = peerAddressBytes.Concat(peerPortBytes).ToArray();
            
            var peerId = BitConverter.ToUInt64(peerEndpointBytes, 0);
#else
            long peerId = BitConverter.ToUInt32(peerAddressBytes, 0);
#endif
            DataWriter.Reset();
            Array.Clear(DataWriter.Data, 0, DataWriter.Data.Length);
            
            DataWriter.Put((int) PacketTypes.Positions); // 4 bytes
            
            foreach (var (_, player) in _playersDictionary)
            {
                var playerEndpoint = player.Peer.EndPoint;
                var playerAddressBytes = playerEndpoint.Address.GetAddressBytes();
#if DEBUG
                var playerPortBytes = BitConverter.GetBytes(playerEndpoint.Port);
                var playerEndpointBytes = playerAddressBytes.Concat(playerPortBytes).ToArray();
                
                var playerId = BitConverter.ToUInt64(playerEndpointBytes, 0);
#else
                long playerId = BitConverter.ToUInt32(playerAddressBytes, 0);
#endif
                player.IsLocal = playerId == peerId;
                
                if (!onPeerConnected)
                {
                    /*
                     * non-eventful sync (loop sync)
                     */
                    
                    if (player.Idle || player.IsLocal) continue;
                    
                    playerIdle = false;
                }
                else
                {
                    /*
                     * event-full sync
                     */
                    
                    playerIdle = false;
                }
                
                // serialized size: 30 (3 + 4 + 23)
                // un-serialized size: 32 (3 + 4 + 25)
                SetDataWriter(player, (long) playerId);
            }
            
            if (playerIdle || _syncCount >= _playersDictionary.Count) return;
            
            connectedPeer.Send(DataWriter, deliveryMethod);
            _syncCount++;
        }
        
        private void SetDataWriter(Player player, long playerId)
        {
            if (IsSerialized)
            {
//                _BasePacket.IsCompressed = false;
                
                var positionPacket = new PositionPacket(
                    (int) player.ActionType, 
                    (int) ObjectTypes.Player, 
                    playerId, 
                    player.IsLocal, 
                    
                    player.X, player.Y, player.Z
                );
                
                var packetBytes = positionPacket.ToByteArray(); // 23 bytes
                
                DataWriter.Put(packetBytes);
            }
            else
            {
#if DEBUG
                // un-serialized total: 25 bytes
                
                DataWriter.Put((int) player.ActionType); // 4 bytes new
                DataWriter.Put((int) ObjectTypes.Player); // 4 bytes
                
                DataWriter.Put(playerId); // 8 bytes
                
                DataWriter.Put(player.IsLocal); // 1 byte
                
                DataWriter.Put(player.X); // 4 bytes
                DataWriter.Put(player.Y); // 4 bytes
                DataWriter.Put(player.Z); // 4 bytes
#endif
            }
        }
        
        private void ResetPlayersStatus()
        {
            _syncCount = 0;
            
            foreach(var (_, player) in _playersDictionary)
            {
                player.IsLocal = false;
                player.Spawned = false;
                player.Moved = false;
                player.Idle = true;
            }
        }
    }
}