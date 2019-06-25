using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Microsoft.Extensions.DependencyInjection;

using LiteNetLib;
using LiteNetLib.Utils;

using prototype_config;
using prototype_serializers;
using prototype_serializers.Packets;
using prototype_server.DB;
using prototype_server.Models;

namespace prototype_server.Controllers
{   
    public class PlayerController : ApplicationController
    {
        private readonly IRepository<Player> _playerModel;
        private readonly Dictionary<Guid, Player> _playersDictionary;
        private readonly SerializerConfiguration _serializerConfiguration;
        
        private PacketTypes _packetType;
        private bool _playerIdle;
        private int _syncCount;

        public PlayerController(IServiceScope scope, IRedisCache redis) : base(scope, redis)
        {
            _playerModel = scope.ServiceProvider.GetService<IRepository<Player>>();
            _playersDictionary = new Dictionary<Guid, Player>();
            
            _serializerConfiguration = SerializerConfiguration.Initialize(IsSerialized);
            
            LogService.LogScopeLock = this;
        }
        
        public void OnPeerConnected(NetPeer peer)
        {
            LogService.Log("[" + peer.Id + "] OnPeerConnected: " + peer.EndPoint.Address + ":" + peer.EndPoint.Port);
            
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
                LogService.Log("Hit Cache");
#endif
                var strArr = cache.Split(',', StringSplitOptions.RemoveEmptyEntries);
                var coords = Array.ConvertAll(strArr, float.Parse);
                
                _playersDictionary[playerGuid].X = coords[0];
                _playersDictionary[playerGuid].Y = coords[1];
                _playersDictionary[playerGuid].Z = coords[2];
            }
            
            // Sync with local client
            SyncWithConnectedPeer(peer, ActionTypes.Spawn);
            
            if (cache != null || newPlayer == null) return;
            
            _playerModel.Create(newPlayer);
        }
        
        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            LogService.Log(
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
        
        public void OnNetworkReceive(NetPeer peer, NetPacketReader packetReader, DeliveryMethod deliveryMethod)
        {
            const int deliveryMethodHeaderSize = 3;
            
            var reader = NetworkService.GetPacketReader(packetReader);
            
            var packetType = NetworkService.GetPacketType(reader); // 4 bytes
            
            var expectedPacketSize = _serializerConfiguration.GetExpectedStateSize(packetType) + 
                                     sizeof(PacketTypes) + // 4 bytes
                                     deliveryMethodHeaderSize; // 3 bytes
            
            LogService.Log($"{packetType} packet expected size: {expectedPacketSize}");
            LogService.Log($"{packetType} packet actual size: {reader.RawDataSize}");
            
            // Not sure if there's any point for this as it will always be a byte[] with some size
            if (reader.RawData == null) return;
            if (reader.RawDataSize != expectedPacketSize) return;
            
            switch (packetType)
            {
                case PacketTypes.Position:
                    SetPosition(peer, reader);
                    break;
                case PacketTypes.Profile:
                    SetProfile();
                    break;
                case PacketTypes.Positions:
                    throw new ArgumentOutOfRangeException(nameof(packetType), packetType, "This packet is only for sending to clients");
                default:
                    throw new ArgumentOutOfRangeException(nameof(packetType), packetType, null);
            }
        }
        
        private void SetProfile()
        {
            
        }
        
        private void SetPosition(NetPeer peer, NetDataReader reader)
        {
            const PacketTypes packetType = PacketTypes.Position;
            
            var stateBytes = reader.GetRemainingBytes();
            var statesCount = stateBytes.Length / _serializerConfiguration.GetExpectedStateSize(packetType);
            
            var positionState = NetworkService.ReadStates<PositionSerializer, PositionPacket>(stateBytes, statesCount)[0];
            
            var actionType = (ActionTypes) positionState.ActionType;
            var objectType = (ObjectTypes) positionState.ObjectType;
            
            var isLocal = positionState.IsLocal;
            
            var x = positionState.X;
            var y = positionState.Y;
            var z = positionState.Z;
            
            var peerEndpoint = peer.EndPoint;
            var addressBytes = peerEndpoint.Address.GetAddressBytes();
#if DEBUG
            var portBytes = BitConverter.GetBytes(peerEndpoint.Port);
            var playerGuid = ConvertBytesToGuid(addressBytes.Concat(portBytes).ToArray());
            
            LogService.Log(
                $"{packetType} [{peerEndpoint.Address}:{peerEndpoint.Port}]: " +
                $"(x: {x}, y: {y}, z: {z})"
            );
#else
            // GUID is exactly 16 bytes or and 36 character in length
            // IP address max size is 16 bytes,
            // therefore the generated guid size won't ever be more than 16 bytes
            var playerGuid = ConvertBytesToGuid(addressBytes);
            
            LogService.Log(
                $"{packetType} [{playerGuid}]: " +
                $"(x: {x}, y: {y}, z: {z})"
            );
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
                
                SyncWithConnectedPeer(player.Peer, player.ActionType, false);
            }
            
            ResetPlayersStatus();
        }
        
        private void SyncWithConnectedPeer(NetPeer connectedPeer, ActionTypes actionType, bool onPeerConnected = true)
        {
            var deliveryMethod = onPeerConnected ? DeliveryMethod.ReliableOrdered : DeliveryMethod.Sequenced; // 3 bytes
            var headerSize = deliveryMethod.GetSize() - 1; // header size is 3 bytes
            var dataWriter = NetworkService.DataWriter;
            
            _playerIdle = true;
            
            NetworkService.ResetDataWriter();
            
            switch (actionType)
            {
                case ActionTypes.Spawn:
                case ActionTypes.Move:
                    _packetType = PacketTypes.Positions;
                    break;
                case ActionTypes.Idle:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(actionType), actionType, null);
            }
            
            switch (_packetType)
            {
                case PacketTypes.Positions:
                    SetPositions(connectedPeer, onPeerConnected);
                    break;
                case PacketTypes.Profile:
                case PacketTypes.Position:
                    throw new ArgumentOutOfRangeException(nameof(_packetType), _packetType, "This packet is only for receiving from clients");
                default:
                    throw new ArgumentOutOfRangeException(nameof(_packetType), _packetType, null);
            }
            
            if (_playerIdle || _syncCount >= _playersDictionary.Count) return;
            
            LogService.Log(
                $"{(IsSerialized ? "" : "Un-")}Serialized " + 
                $"{_packetType} RawDataSize: {dataWriter.Length + headerSize}"
            );
            
            connectedPeer.Send(dataWriter, deliveryMethod);
            _syncCount++;
        }
        
        private void SetPositions(NetPeer connectedPeer, bool onPeerConnected)
        {
            var peerEndpoint = connectedPeer.EndPoint;
            var peerAddressBytes = peerEndpoint.Address.GetAddressBytes();
#if DEBUG
            var peerPortBytes = BitConverter.GetBytes(peerEndpoint.Port);
            var peerEndpointBytes = peerAddressBytes.Concat(peerPortBytes).ToArray();
            
            var peerId = BitConverter.ToUInt64(peerEndpointBytes, 0);
#else
            long peerId = BitConverter.ToUInt32(peerAddressBytes, 0);
#endif
            NetworkService.WriteStates((int) _packetType); // 4 bytes
            
            var playerCount = 0;
            
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
                    
                    _playerIdle = false;
                }
                else
                {
                    /*
                     * event-full sync
                     */
                    
                    _playerIdle = false;
                }
                
                // serialized size: 30 (3 + 4 + 23)
                // un-serialized size: 36 (3 + 4 + 29)
                WritePositions(player, (long) playerId, ++playerCount, onPeerConnected);
            }
        }
        
        private void WritePositions(Player player, long playerId, int playerCount, bool onPeerConnected)
        {
            var isReadyToSerialize = playerCount == _playersDictionary.Count || !onPeerConnected;
            var position = new Vector3(player.X, player.Y, player.Z);
            
            NetworkService.WriteStates<PositionSerializer, PositionPacket>(
                isReadyToSerialize,
                
                (int) player.ActionType, // 4 bytes
                (int) player.ObjectType, // 4 bytes
                playerId, // 8 bytes
                player.IsLocal, // 1 byte
                
                position // 4 * 3 = 12 bytes
            );
        }
        
        private void ResetPlayersStatus()
        {
            _syncCount = 0;
            
            foreach(var (_, player) in _playersDictionary)
            {
                player.IsLocal = true;
                player.Spawned = false;
                player.Moved = false;
                player.Idle = true;
            }
        }
    }
}