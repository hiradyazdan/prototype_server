using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

using prototype_config;
using prototype_services.Common.Network;
using prototype_services.Interfaces;
using prototype_models;
using prototype_models.Interfaces;
using prototype_serializers;

namespace prototype_server.Controllers
{
    public class RelayController : ApplicationController, INetEventListenerAdapter
    {
        private readonly SerializerConfiguration _serializerConfig;
        private readonly ActionContext _actionContext;
        
        private ActionEntity _actionEntity;
        
        public RelayController()
        {
            _actionContext = Contexts.action;
            _serializerConfig = SerializerConfiguration.Initialize(IsSerialized);
            
            var httpHostAddress = Config["HTTP_HOST"];
            var httpHostPort = int.Parse(Config["HTTP_PORT"]);
            var isHttpSecure = Config.IsConfigActive("HTTP_SECURE");
            
            CrudService.SetupClient(httpHostAddress, httpHostPort, isHttpSecure);
            
            LogService.LogScope = this;
        }
        
        public void OnPeerConnected(object netPeer)
        {
            var peer = RelayService.GetNetPeer(netPeer);
            
            LogService.Log(
                $"OnPeerConnected: " +
                $"{peer.EndPoint.Address} : " +
                $"{peer.EndPoint.Port}"
            );
        }
        
        public void OnPeerDisconnected(object netPeer, object disconnectInformation)
        {
            var peer = RelayService.GetNetPeer(netPeer);
            var disconnectInfo = RelayService.GetDisconnectInfo(disconnectInformation);
            
            LogService.Log(
                "[" + peer.Id + "] OnPeerDisconnected: " + 
                peer.EndPoint.Address + ":" + 
                peer.EndPoint.Port + 
                " - Reason: " + disconnectInfo.Reason
            );
            
            _actionEntity = _actionContext.CreateEntity();
            
            _actionEntity.isQuitStarted = true;
            
            _actionEntity.AddConnectedPeer(peer);
        }
        
        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            LogService.Log(
                $"OnNetworkError: " +
                $"{endPoint.Address} : " +
                $"{endPoint.Port}" +
                " - socketError: " + socketError
            );
        }
        
        public void OnNetworkReceive(object netPeer, object packetReader, DeliveryMethods deliveryMethod)
        {
            /*
             * ReliableUnordered
             * Sequenced
             * ReliableOrdered
             * AckReliable
             * AckReliableOrdered
             * Ping
             * ReliableSequenced
             * AckReliableSequenced
             */
            const int headerSize = 3;
            
            var minRequiredSize = headerSize + // 3 bytes
                                  sizeof(PacketTypes) + // 4 bytes
                                  _serializerConfig.GetExpectedStateSize(); // 21 bytes
            
            var reader = RelayService.GetPacketReader(packetReader);
            
            if (reader.RawData == null || 
                reader.UserDataOffset != headerSize ||
                reader.RawDataSize < minRequiredSize) return;
            
            var packetType = RelayService.GetPacketType(reader); // 4 bytes
            
            LogService.Log(
                $"{(IsSerialized ? "" : "Un-")}Serialized " + 
                $"{packetType} RawDataSize: {reader.RawDataSize}"
            );
            
            var states = SetStates(packetType, reader.GetRemainingBytes());
            
            SetEntities(netPeer, packetType, states);
        }
        
        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, object reader, UnconnectedMessageTypes messageType)
        {
            LogService.Log("OnNetworkReceiveUnconnected");
        }
        
        public void OnNetworkLatencyUpdate(object peer, int latency)
        {
//            throw new System.NotImplementedException();
        }
        
        public void OnConnectionRequest(object connRequest)
        {
            /*
             * TODO:
             * Should decide on how to calculate the maximum limit of concurrent connections
             * per relay server instance dynamically depending on the location and network latencies
             * which then should be passed over here to limit the connections at server's core infra.
             */
            const int maxConn = 10;
            var request = RelayService.GetConnectionRequest(connRequest);
            
            if (RelayService.NetManager.PeersCount <= maxConn)
            {
                /*
                 * TODO:
                 * Should send UDP Connection keys from HTTP Server to:
                 * (which are generated per each group of peers per relay server instance
                 * that are limited to the maximum number specified)
                 * - Relay Server (HTTP Request on application start)
                 * - Clients (HTTP Request on Preload Session) 
                 */
                if (request.AcceptIfKey(Config["UDP_CONN_KEY"]) != null) return;
                
                LogService.LogError(
                    $"UDP Connection Key Mismatch on client: " +
                    $"{request.RemoteEndPoint.Address}:{request.RemoteEndPoint.Port}"
                );
            }
            else
            {
                LogService.LogError($"UDP connections exceed maximum limit of {maxConn}!");
                request.Reject();
            }
        }
        
        private void SetEntities(object netPeer, PacketTypes packetType, IEnumerable<IModel> states)
        {
            foreach (var state in states)
            {
                _actionEntity = _actionContext.CreateEntity();
                _actionEntity.AddConnectedPeer(RelayService.GetNetPeer(netPeer));
                
                SetBaseEntityState(state);
                
                switch (packetType)
                {
                    case PacketTypes.GameState:
                        var gameState = (IGameStateModel) state;
                        
                        SetGameState(gameState);
                        break;
                    case PacketTypes.Position:
                        var positionState = (IPositionModel) state;
                        
                        SetPositionState(positionState);
                        break;
                    case PacketTypes.Profile:
                        break;
                    case PacketTypes.GameStates:
                    case PacketTypes.Positions:
                        throw new ArgumentOutOfRangeException(nameof(packetType), packetType, "This packet is only for sending to client");
                    default:
                        throw new ArgumentOutOfRangeException(nameof(packetType), packetType, null);
                }
            }
        }
        
        private IEnumerable<IModel> SetStates(PacketTypes packetType, byte[] stateBytes)
        {
            var statesCount = stateBytes.Length / _serializerConfig.GetExpectedStateSize(packetType);
            
            switch (packetType)
            {
                case PacketTypes.GameState:
                    return RelayService.ReadStates<GameStateSerializer, UdpPreloadGameStateModel>(stateBytes, statesCount);
                case PacketTypes.Position:
                    return RelayService.ReadStates<PositionSerializer, UdpPositionModel>(stateBytes, statesCount);
                case PacketTypes.Profile:
                    return null;
                case PacketTypes.GameStates:                    
                case PacketTypes.Positions:
                    throw new ArgumentOutOfRangeException(nameof(packetType), packetType, "This packet is only for sending to clients");
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        private void SetGameState(IGameStateModel state)
        {
            var position = state.Position.ToVector3();
            
            _actionEntity.AddSocialId(((UdpPreloadGameStateModel) state).SocialId);
            
            _actionEntity.AddActorId(state.ActorId);
            _actionEntity.AddPosition(position);
        }
        
        private void SetPositionState(IPositionModel state)
        {
            var position = state.ToVector3();
            
            _actionEntity.AddActorId(state.ActorId);
            _actionEntity.AddPosition(position);
        }
        
        private void SetBaseEntityState(IModel state)
        {
            /*
             * ONLY non-local players gets spawned here through relay server
             */
            
            var isSpawn = (ActionTypes) state.ActionType == ActionTypes.Spawn;
            var isMove = (ActionTypes) state.ActionType == ActionTypes.Move;
            
            var isSpawnStarted = isSpawn && !isMove;
            var isMoveStarted = isMove && !isSpawn;
            
            var isPlayerState = (ObjectTypes) state.ObjectType == ObjectTypes.Player;
            var isEnemyState = (ObjectTypes) state.ObjectType == ObjectTypes.Enemy;
            
            _actionEntity.isSpawnStarted = isSpawnStarted;
            _actionEntity.isMoveStarted = isMoveStarted;
            _actionEntity.isIdle = !isSpawnStarted && !isMoveStarted;
            
            _actionEntity.AddId(state.Id);
            
            _actionEntity.isPlayer = isPlayerState;
            _actionEntity.isEnemy = isEnemyState;
            _actionEntity.isLocal = state.IsLocal;
        }
    }
}