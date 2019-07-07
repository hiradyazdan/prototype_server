using System;
using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using LiteNetLib;
using LiteNetLib.Utils;
using Moq;

using prototype_server.Controllers;
using prototype_storage;
using prototype_models.OOD;
using prototype_server.Specs.Config;
using prototype_server.Specs.Config.Utils.Helpers;
using prototype_services.Common;
using prototype_services.Interfaces;

namespace prototype_server.Specs.Controllers.PlayerCtrl
{
    enum NET_DATA_TYPE
    {
        PlayerPosition,
        PlayerPositionsArray,
        NotPlayerPosition
    }
    
    public class WhenPlayerMoves : ScenarioFor<PlayerController>
    {
        private PlayerController _subject;
        private RedisCache _redisCache;
        private Player _playerMock;
        private NetPeer _peerMock;
        private long _peerId;
        
        private void EstablishContext()
        {
            var peerEndpoint = BitConverter.ToUInt32(IPAddress.Parse("192.168.0.1").GetAddressBytes(), 0);
            var ipEndpointMock = new Mock<IPEndPoint>(MockBehavior.Loose, peerEndpoint, 15000).Object;
            
            _peerId = peerEndpoint;
            _redisCache = new RedisCache("localhost");
            
            _peerMock = Helpers.GetPeerMock(ipEndpointMock);
            _playerMock = new Player(_peerMock)
            {
                GUID = Guid.NewGuid(),
                Name = "user_15000",
                IsLocal = true,
                X = 12.3f,
                Y = 30.3f,
                Z = 42.5f
            };
            
            const int deliveryMethodHeaderSize = 3;
            const int rawDataSize = deliveryMethodHeaderSize + 
                                    sizeof(int) + 
                                    sizeof(bool) + 
                                    sizeof(float) * 3;
            
            var scopeMock = new Mock<IServiceScope>();
            var dbContextMock = new Mock<GameDbContext>();
            var playerDbSetMock = Helpers.GetQueryableMockDbSet(_playerMock);
            var readerMock = Helpers.GetReaderMock(_playerMock, NET_DATA_TYPE.PlayerPosition, rawDataSize);
            
            dbContextMock.Setup(m => m.Set<Player>()).Returns(playerDbSetMock);
            
            var playerModelRepoMock = new Mock<ModelRepository<Player>>(MockBehavior.Loose, dbContextMock.Object);
            
            scopeMock.Setup(m => m.ServiceProvider.GetService(typeof(IRepository<Player>)))
                     .Returns(playerModelRepoMock.Object);

            _subject = new PlayerController(scopeMock.Object, _redisCache);

            _subject.OnPeerConnected(_peerMock);
            
            _subject.OnNetworkReceive(_peerMock, readerMock, DeliveryMethod.Sequenced);
        }
        
        private void ItShouldSyncGameStateWithAllConnectedClients()
        {
            _subject.SyncWithConnectedPeers();

            var dataWriter = new NetDataWriter();

            dataWriter.Put((int)NET_DATA_TYPE.PlayerPositionsArray);
            dataWriter.Put(_peerId);
            dataWriter.Put(_playerMock.IsLocal);
            dataWriter.Put(_playerMock.X);
            dataWriter.Put(_playerMock.Y);
            dataWriter.Put(_playerMock.Z);
            
            _subject.DataWriter.Data.Should().BeEquivalentTo(dataWriter.Data);
        }
    }

    public class WhenPlayerIsIdle : ScenarioFor<PlayerController>
    {   
        private PlayerController _subject;
        private RedisCache _redisCache;
        private Player _playerMock;
        private NetPeer _peerMock;
        
        private void EstablishContext()
        {
            var peerEndpoint = BitConverter.ToUInt32(IPAddress.Parse("192.168.0.1").GetAddressBytes(), 0);
            var ipEndpointMock = new Mock<IPEndPoint>(MockBehavior.Loose, peerEndpoint, 15000).Object;

            const int deliveryMethodHeaderSize = 3;
            const int wrongRawDataSize = deliveryMethodHeaderSize + 
                                         sizeof(int) + 
                                         sizeof(float) * 3;
            
            _redisCache = new RedisCache("localhost");
            
            _peerMock = Helpers.GetPeerMock(ipEndpointMock);
            _playerMock = new Player(_peerMock)
            {
                GUID = Guid.NewGuid(),
                Name = "user_15000",
                IsLocal = true,
                X = 12.3f,
                Y = 30.3f,
                Z = 42.5f
            };
            
            var scopeMock = new Mock<IServiceScope>();
            var dbContextMock = new Mock<GameDbContext>();
            var playerDbSetMock = Helpers.GetQueryableMockDbSet(_playerMock);
            var readerMock = Helpers.GetReaderMock(_playerMock, NET_DATA_TYPE.PlayerPosition, wrongRawDataSize);
            
            dbContextMock.Setup(m => m.Set<Player>()).Returns(playerDbSetMock);
            
            var playerModelRepoMock = new Mock<ModelRepository<Player>>(MockBehavior.Loose, dbContextMock.Object);
            
            scopeMock.Setup(m => m.ServiceProvider.GetService(typeof(IRepository<Player>)))
                     .Returns(playerModelRepoMock.Object);

            _subject = new PlayerController(scopeMock.Object, _redisCache);

            _subject.OnPeerConnected(_peerMock);
            
            _subject.OnNetworkReceive(_peerMock, readerMock, DeliveryMethod.Sequenced);
        }

        private void ItShouldNotSyncGameStateWithAnyClients()
        {
            _subject.SyncWithConnectedPeers();
            
            var dataWriter = new NetDataWriter();

            dataWriter.Put((int)NET_DATA_TYPE.PlayerPositionsArray);
            
            _subject.DataWriter.Data.Should().BeEquivalentTo(dataWriter.Data);
        }
    }
    
    public class WhenPlayerMakesAnotherChangeOtherThanMoving : ScenarioFor<PlayerController>
    {   
        private PlayerController _subject;
        private RedisCache _redisCache;
        private Player _playerMock;
        private NetPeer _peerMock;
        
        private void EstablishContext()
        {
            var peerEndpoint = BitConverter.ToUInt32(IPAddress.Parse("192.168.0.1").GetAddressBytes(), 0);
            var ipEndpointMock = new Mock<IPEndPoint>(MockBehavior.Loose, peerEndpoint, 15000).Object;            

            const int deliveryMethodHeaderSize = 3;
            const int rawDataSize = deliveryMethodHeaderSize + 
                                         sizeof(int) +
                                         sizeof(bool) +
                                         sizeof(float) * 3;
            
            _redisCache = new RedisCache("localhost");
            _peerMock = Helpers.GetPeerMock(ipEndpointMock);
            _playerMock = new Player(_peerMock)
            {
                GUID = Guid.NewGuid(),
                Name = "user_15000",
                IsLocal = true,
                X = 12.3f,
                Y = 30.3f,
                Z = 42.5f
            };
            
            var scopeMock = new Mock<IServiceScope>();
            var dbContextMock = new Mock<GameDbContext>();
            var playerDbSetMock = Helpers.GetQueryableMockDbSet(_playerMock);
            var readerMock = Helpers.GetReaderMock(_playerMock, NET_DATA_TYPE.NotPlayerPosition, rawDataSize);
            
            dbContextMock.Setup(m => m.Set<Player>()).Returns(playerDbSetMock);
            
            var playerModelRepoMock = new Mock<ModelRepository<Player>>(MockBehavior.Loose, dbContextMock.Object);
            
            scopeMock.Setup(m => m.ServiceProvider.GetService(typeof(IRepository<Player>)))
                     .Returns(playerModelRepoMock.Object);

            _subject = new PlayerController(scopeMock.Object, _redisCache);

            _subject.OnPeerConnected(_peerMock);
            
            _subject.OnNetworkReceive(_peerMock, readerMock, DeliveryMethod.Sequenced);
        }

        private void ItShouldNotSyncMoveStateWithAnyClients()
        {
            _subject.SyncWithConnectedPeers();
            
            var dataWriter = new NetDataWriter();

            dataWriter.Put((int)NET_DATA_TYPE.PlayerPositionsArray);
            
            _subject.DataWriter.Data.Should().BeEquivalentTo(dataWriter.Data);
        }
    }
}