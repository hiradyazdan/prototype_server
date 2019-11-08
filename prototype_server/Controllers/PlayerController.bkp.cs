using System;
using System.Collections.Generic;
using System.Numerics;

using LiteNetLib;
using LiteNetLib.Utils;

using prototype_config;
using prototype_serializers;
using prototype_models.OOD;
using prototype_models;
using prototype_serializers.JSON;

namespace prototype_server.Controllers
{
    public class PlayerController : ApplicationController
    {
        private readonly Dictionary<long, PlayerModel> _playersDictionary;
        private readonly SerializerConfiguration _serializerConfig;
        private readonly Dictionary<HttpHeaderFields, string> _httpHeaders;
        
        private PacketTypes _packetType;
        private bool _playerIdle;
        private int _syncCount;
        
        private long _playerId;
        private string _playerSocialId;
        
        public PlayerController()
        {
            _playersDictionary = new Dictionary<long, PlayerModel>();
            
            _serializerConfig = SerializerConfiguration.Initialize(IsSerialized);
            
#if DEBUG
            const string httpHostAddress = "127.0.0.1";
            const int httpHostPort = 3000;
            const bool isHttpSecure = true;
#else
            const string httpHostAddress = "127.0.0.1";
            const int httpHostPort = 3000;
            const bool isHttpSecure = true;
#endif
            
            CrudService.SetupClient(httpHostAddress, httpHostPort, isHttpSecure);
            
            _httpHeaders = new Dictionary<HttpHeaderFields, string>
            {
                { HttpHeaderFields.AuthScheme, "Bearer" }
            };
            
            LogService.LogScope = this;
        }
        
        public void OnPeerConnected(NetPeer peer)
        {
            LogService.Log("[" + peer.Id + "] OnPeerConnected: " + peer.EndPoint.Address + ":" + peer.EndPoint.Port);
        }
        
        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            LogService.Log(
                "[" + peer.Id + "] OnPeerDisconnected: " + 
                peer.EndPoint.Address + ":" + 
                peer.EndPoint.Port + 
                " - Reason: " + disconnectInfo.Reason
            );
            
            try
            {
                _playerId = _playersDictionary.SingleOrDefaultBy(p =>
                {
                    var (_, value) = p;
                    
                    /*
                     * TODO:
                     * Not sure checking with ip and port to retrieve the profile id is the
                     * best and robust solution here as there might be some edge cases that break the functionality!
                     */
                    return value.Peer.EndPoint.Address.ToString() == peer.EndPoint.Address.ToString() && 
                           value.Peer.EndPoint.Port == peer.EndPoint.Port;
                }).Value.Id;
            }
            catch (NullReferenceException exc)
            {
                LogService.LogError(exc);
                _playerId = 0;
            }
            
            var player = _playersDictionary.ContainsKey(_playerId) ? _playersDictionary[_playerId] : null;
            
            // var cache = player != null ? Redis.GetCache($"{player.Id}") : null;
            
            /*
             * TODO:
             * utf8json doesn't parse numbers from string,
             * should create a custom formatter
             * or wait till msgpack binary formats are implemented through http/2 messages
             */
//            var gameStates = JsonSerializer.FromJsonArray<UdpGameStateModel>(cache);
            
            /*
             * If the player makes no changes on the client before disconnect
             */
            // if (
                // cache == null 
                // || player.Spawned 
//              || gameStates.All(gameState => gameState.Position.ToVector3() == player.ToVector3())
            //     )
            // {
            //     return;
            // }
            
//            var actors = new PlayerModel[] { };
//            
//            var gameStates = new GameStateModel[actors.Length];
//            
//            for (var i = 0; i < actors.Length; i++)
//            {
//                gameStates[i] = new GameStateModel
//                {
//                    ActorId = actors[i].ActorId, 
//                    Position = new PositionModel 
//                    {
//                        X = actors[i].X, 
//                        Y = actors[i].Y, 
//                        Z = actors[i].Z
//                    }
//                };
//            }
//            
//            var gameStatesJson = JsonSerializer.ToJson(gameStates);
            
            var profileGameStatesJson = JsonSerializer.ToAnyJson(new
            {
                player.Name,
                ActorsAttributes = new []
                {
                    new
                    {
                        Id = player.ActorId,
                        ActorTypeId = 1,
                        Name = "Player Actor",
                        GameStateAttributes = new
                        {
                            player.ActorId,
                            
                            player.X, 
                            player.Y, 
                            player.Z
                        }
                    }
                }
            });
            
            LogService.Log("gameStatesJson: " + profileGameStatesJson);
            
            AuthenticateProfile();
            
            var urlParams = new Dictionary<string, string>
            {
                { "id", $"{_playerId}" }
            };
            
            var patchRes = CrudService.RequestStringAsync(
                $"{ContentApiEndpoints.PROFILES}/:id",
                profileGameStatesJson,
                urlParams,
                HttpMethods.Patch,
                _httpHeaders
            ).Result;
            
            CrudService.AuthToken = null;
            
            _playersDictionary.Remove(_playerId);
        }
        
        public void OnNetworkReceive(NetPeer peer, NetPacketReader packetReader, DeliveryMethod deliveryMethod)
        {
            LogService.Log(
                $"[{peer.Id}] OnNetworkReceive: " + 
                $"{peer.EndPoint.Address}:{peer.EndPoint.Port} " +   
                $"(Delivery Method: {deliveryMethod})"
            );
            
            const int deliveryMethodHeaderSize = 3;
            
            var reader = RelayService.GetPacketReader(packetReader);
            
            var packetType = RelayService.GetPacketType(reader); // 4 bytes
            
            var expectedPacketSize = _serializerConfig.GetExpectedStateSize(packetType) + // 30 bytes
                                     sizeof(PacketTypes) + // 4 bytes
                                     deliveryMethodHeaderSize; // 3 bytes
            
            LogService.Log($"{packetType} packet expected size: {expectedPacketSize}");
            LogService.Log($"{packetType} packet actual size: {reader.RawDataSize}");
            
            // Not sure if there's any point for this as it will always be a byte[] with some size
            if (reader.RawData == null) return;
            if (reader.RawDataSize != expectedPacketSize && packetType != PacketTypes.GameState) return;
            if ((reader.RawDataSize > 60 || reader.RawDataSize < expectedPacketSize) && 
                packetType == PacketTypes.GameState) return;
            
            switch (packetType)
            {
                case PacketTypes.GameState:
                    SetGameState(peer, reader);
                    break;
                case PacketTypes.Position:
                    SetPosition(peer, reader);
                    break;
                case PacketTypes.Profile:
                    SetProfile();
                    break;
                case PacketTypes.GameStates:
                case PacketTypes.Positions:
                    throw new ArgumentOutOfRangeException(nameof(packetType), packetType, "This packet is only for sending to clients");
                default:
                    throw new ArgumentOutOfRangeException(nameof(packetType), packetType, null);
            }
        }

        private void SetGameState(NetPeer peer, NetDataReader reader)
        {
            const PacketTypes packetType = PacketTypes.GameState;
            
            var stateBytes = reader.GetRemainingBytes();
            var statesCount = stateBytes.Length / _serializerConfig.GetExpectedStateSize(packetType);
            
            var gameState = RelayService.ReadStates<GameStateSerializer, UdpPreloadGameStateModel>(stateBytes, statesCount)[0];
            
            _playerId = gameState.Id;
            _playerSocialId = gameState.SocialId;
            
            if (!_playersDictionary.ContainsKey(_playerId))
            {
                var newPlayer = new PlayerModel(peer)
                {
                    Id = _playerId,
                    ActorId = gameState.ActorId
                };
                
                _playersDictionary.Add(_playerId, newPlayer);
            }
            
            var actionType = (ActionTypes) gameState.ActionType;
            var objectType = (ObjectTypes) gameState.ObjectType;
            
            var isLocal = gameState.IsLocal;
            
            var x = gameState.Position.X;
            var y = gameState.Position.Y;
            var z = gameState.Position.Z;
            
            LogService.Log($"{packetType} [{peer.EndPoint.Address}:{peer.EndPoint.Port}: {_playerId}]: " + 
                $"(x: {x}, y: {y}, z: {z})"
            );
            
            var isSpawned = actionType == ActionTypes.Spawn;
            var isMoved = actionType == ActionTypes.Move;
            
            _playersDictionary[_playerId].Spawned = isSpawned;
            _playersDictionary[_playerId].Moved = isMoved;
            _playersDictionary[_playerId].Idle = !isSpawned && !isMoved;
            
            if (isSpawned)
            {
                /*
                 * On network receive is called once the player is spawned and sends packet to relay server
                 * Currently this is the best solution to spawn non-local players
                 * 
                 * TODO: modifying relay server to ECS Architecture can alleviate issues like this
                 */
                foreach (var (_, player) in _playersDictionary)
                {
                    player.Idle = false;
                }
            }
            
            /*
             * Packet Data
             */
            
            _playersDictionary[_playerId].ActionType = (int) actionType;
            _playersDictionary[_playerId].ObjectType = (int) objectType;
            
            _playersDictionary[_playerId].IsLocal = isLocal;
            
            _playersDictionary[_playerId].X = x;
            _playersDictionary[_playerId].Y = y;
            _playersDictionary[_playerId].Z = z;
        }
        
        private void SetProfile()
        {
            
        }
        
        private void SetPosition(NetPeer peer, NetDataReader reader)
        {
            const PacketTypes packetType = PacketTypes.Position;
            
            var stateBytes = reader.GetRemainingBytes();
            var statesCount = stateBytes.Length / _serializerConfig.GetExpectedStateSize(packetType);
            
            var positionState = RelayService.ReadStates<PositionSerializer, UdpPositionModel>(stateBytes, statesCount)[0];
            
            _playerId = positionState.Id;
            
            var actionType = (ActionTypes) positionState.ActionType;
            var objectType = (ObjectTypes) positionState.ObjectType;
            
            var isLocal = positionState.IsLocal;
            
            var x = positionState.X;
            var y = positionState.Y;
            var z = positionState.Z;
            
            LogService.Log($"{packetType} [{peer.EndPoint.Address}:{peer.EndPoint.Port}: {_playerId}]: " + 
                $"(x: {x}, y: {y}, z: {z})"
            );
            
            var isSpawned = actionType == ActionTypes.Spawn;
            var isMoved = actionType == ActionTypes.Move;
            
            _playersDictionary[_playerId].Spawned = isSpawned;
            _playersDictionary[_playerId].Moved = isMoved;
            _playersDictionary[_playerId].Idle = !isSpawned && !isMoved;
            
            if (isMoved)
            {
                /*
                 * This will avoid calling SetGameStates for non-local player moves
                 * 
                 * TODO: modifying relay server to ECS Architecture can alleviate issues like this
                 */
                foreach (var (_, player) in _playersDictionary)
                {
                    player.ActionType = (int) ActionTypes.Move;
                }
            }
            
            /*
             * Packet Data
             */
            
            _playersDictionary[_playerId].ActionType = (int) actionType;
            _playersDictionary[_playerId].ObjectType = (int) objectType;
            
            _playersDictionary[_playerId].IsLocal = isLocal;
            
            _playersDictionary[_playerId].X = x;
            _playersDictionary[_playerId].Y = y;
            _playersDictionary[_playerId].Z = z;
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
                
                SyncWithConnectedPeer(
                    player.Peer, 
                    player.Id, 
                    (ActionTypes) player.ActionType, 
                    false
                );
            }
            
            ResetPlayersStatus();
        }
        
        private void SyncWithConnectedPeer(NetPeer connectedPeer, long playerId, ActionTypes actionType, bool onPeerConnected = true)
        {
            var deliveryMethod = onPeerConnected ? DeliveryMethod.ReliableOrdered : DeliveryMethod.Sequenced; // 3 bytes
            var headerSize = deliveryMethod.GetSize() - 1; // header size is 3 bytes
            var dataWriter = RelayService.DataWriter;
            
            _playerIdle = true;
            
            RelayService.ResetDataWriter();
            
            switch (actionType)
            {
                case ActionTypes.Spawn:
                    _packetType = PacketTypes.GameStates;
                    break;
                case ActionTypes.Move:
                    _packetType = PacketTypes.Positions;
                    break;
                case ActionTypes.Idle:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(actionType), actionType, null);
            }
            
            RelayService.WriteStates((int) _packetType); // 4 bytes
            
            switch (_packetType)
            {
                case PacketTypes.GameStates:
                    SetGameStates(playerId, onPeerConnected);
                    break;
                case PacketTypes.Positions:
                    SetPositions(playerId, onPeerConnected);
                    break;
                case PacketTypes.Profile:
                case PacketTypes.GameState:
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

        private void SetGameStates(long playerId, bool onPeerConnected)
        {
            var playerCount = 0;
            
            foreach (var (_, player) in _playersDictionary)
            {
                player.IsLocal = player.Id == playerId;
                
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
                
                WriteGameStates(player, ++playerCount, onPeerConnected);
            }
        }
        
        private void WriteGameStates(PlayerModel player, int playerCount, bool onPeerConnected)
        {
            var isReadyToSerialize = playerCount == _playersDictionary.Count || !onPeerConnected;
            var position = new UdpPreloadPositionModel
            {
                X = player.X, 
                Y = player.Y, 
                Z = player.Z
            };
            
            RelayService.WriteStates<GameStateSerializer, UdpNonLocalPreloadGameStateModel>(
                isReadyToSerialize,
                
                player.ActionType, // 4 bytes
                player.ObjectType, // 4 bytes
                player.Id, // 8 bytes
                player.IsLocal, // 1 byte
                
                player.ActorId, // 4 bytes
                
                position // 4 * 3 = 12 bytes
            );
        }
        
        private void SetPositions(long playerId, bool onPeerConnected)
        {
            var playerCount = 0;
            
            foreach (var (_, player) in _playersDictionary)
            {
                player.IsLocal = player.Id == playerId;
                
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
                
                WritePositions(player, ++playerCount, onPeerConnected);
            }
        }
        
        private void WritePositions(PlayerModel player, int playerCount, bool onPeerConnected)
        {
            var isReadyToSerialize = playerCount == _playersDictionary.Count || !onPeerConnected;
            var position = new Vector3(player.X, player.Y, player.Z);
            
            RelayService.WriteStates<PositionSerializer, UdpPositionModel>(
                isReadyToSerialize,
                
                player.ActionType, // 4 bytes
                player.ObjectType, // 4 bytes
                player.Id, // 8 bytes
                player.IsLocal, // 1 byte
                
                player.ActorId, // 4 bytes
                
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
        
        private void AuthenticateProfile()
        {
            /*
             * TODO:
             * Figure out a way to generate a more secure password
             *
             * Note:
             * Could generating one-time password be a good solution in this scenario,
             * as passwords are currently always generated using
             * some already known components (social id, auth endpoint, ...)
             * However, one-time passwords are quite random and can only work per login sessions
             */
            var password = $"{_playerSocialId}/{ContentApiEndpoints.AUTH}".ConvertToSecureToken();
            
            var profile = new ProfileModel
            {
                SocialId = _playerSocialId,
                Password = password
            };
            
            var authRes = CrudService.RequestStringAsync(
                ContentApiEndpoints.AUTH,
                JsonSerializer.ToJson(profile, new [] { "action_type", "object_type", "id", "is_local" })
            ).Result;
            
            CrudService.AuthToken = JsonSerializer.FromJson<ProfileModel>(authRes)?.AuthToken;
        }
    }
}