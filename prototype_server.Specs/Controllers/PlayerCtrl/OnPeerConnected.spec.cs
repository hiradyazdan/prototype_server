using System;
using System.Linq;
using System.Net;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using prototype_server.Controllers;
using prototype_server.DB;
using prototype_server.Libs.LiteNetLib;
using prototype_server.Libs.LiteNetLib.Utils;
using prototype_server.Models;
using prototype_server.Specs.Config;

namespace prototype_server.Specs.Controllers.PlayerCtrl
{
    public class WhenPlayerGetsConnectedForTheFirstTime : ScenarioFor<PlayerController>
    {
        private enum NET_DATA_TYPE
        {
            PlayerPosition,
            PlayerPositionsArray,
        }
        
        private PlayerController _subject;
        private ModelRepository<Player> _playerModelRepo;
        private NetPeer _peerMock;
        private long _peerId;

        private void EstablishContext()
        {
            var dbCtxOptions = new DbContextOptionsBuilder<GameDbContext>()
                .UseInMemoryDatabase(databaseName: "game_db_test")
                .Options;
            
            var peerEndpointMock = BitConverter.ToUInt32(IPAddress.Parse("192.168.0.1").GetAddressBytes(), 0);
            
            var ipEndpointMock = new Mock<IPEndPoint>(MockBehavior.Loose, peerEndpointMock, 15000).Object;
            var scopeMock = new Mock<IServiceScope>();
            var redisCacheMock = new Mock<RedisCache>(MockBehavior.Loose, "localhost").Object;
            var dbContext = new GameDbContext(dbCtxOptions);

            _peerId = peerEndpointMock;
            _playerModelRepo = new ModelRepository<Player>(dbContext);
            _peerMock = new Mock<NetPeer>(MockBehavior.Loose, ipEndpointMock, 0).Object;
            
            scopeMock.Setup(m => m.ServiceProvider.GetService(typeof(IRepository<Player>)))
                     .Returns(_playerModelRepo);

            _subject = new PlayerController(scopeMock.Object, redisCacheMock);
            
            _subject.OnPeerConnected(_peerMock);
        }

        private void ItShouldReceiveInitialGameState()
        {    
            const bool isLocalPlayer = true;
            const float x = 0.0f;
            const float y = 0.0f;
            const float z = 0.0f;

            var dataWriter = new NetDataWriter();

            dataWriter.Put((int)NET_DATA_TYPE.PlayerPositionsArray);
            dataWriter.Put(_peerId);
            dataWriter.Put(isLocalPlayer);
            dataWriter.Put(x);
            dataWriter.Put(y);
            dataWriter.Put(z);
            
            _subject.DataWriter.Data.Should().BeEquivalentTo(dataWriter.Data);
        }

        private void ItShouldCreateARecordInDatabase()
        {
            _playerModelRepo.GetAll().Count().Should().Be(1);
        }
    }
    
    public class WhenPlayerGetsConnectedAgain : ScenarioFor<PlayerController>
    {
        private enum NET_DATA_TYPE
        {
            PlayerPosition,
            PlayerPositionsArray,
        }
        
        private PlayerController _subject;
        private ModelRepository<Player> _playerModelRepo;
        private NetPeer _peerMock;
        private long _peerId;

        private static DbSet<T> GetQueryableMockDbSet<T>(params T[] sourceList) where T : class
        {
            var queryable = sourceList.AsQueryable();

            var dbSet = new Mock<DbSet<T>>();
            dbSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
            dbSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            dbSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            dbSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());

            return dbSet.Object;
        }
        
        private void EstablishContext()
        {
            var dbCtxOptions = new DbContextOptionsBuilder<GameDbContext>()
                .UseInMemoryDatabase(databaseName: "game_db_test")
                .Options;
            
            var peerEndpointMock = BitConverter.ToUInt32(IPAddress.Parse("192.168.0.1").GetAddressBytes(), 0);
            
            var ipEndpointMock = new Mock<IPEndPoint>(MockBehavior.Loose, peerEndpointMock, 15000).Object;
            var scopeMock = new Mock<IServiceScope>();
            var redisCacheMock = new Mock<RedisCache>(MockBehavior.Loose, "localhost");
            var dbContext = new Mock<GameDbContext>(dbCtxOptions);

            var playerDbSetMock = GetQueryableMockDbSet(
                    new Player(_peerMock) { Name = "user_15000"}
                );

            dbContext.Setup(m => m.Set<Player>()).Returns(playerDbSetMock);

            _peerId = peerEndpointMock;
            _playerModelRepo = new ModelRepository<Player>(dbContext.Object);
            _peerMock = new Mock<NetPeer>(MockBehavior.Loose, ipEndpointMock, 0).Object;
            
            scopeMock.Setup(m => m.ServiceProvider.GetService(typeof(IRepository<Player>)))
                .Returns(_playerModelRepo);
            
            redisCacheMock.Setup(m => m.GetCache(_peerId.ToString())).Returns("10.3,30.3,42.5");

            _subject = new PlayerController(scopeMock.Object, redisCacheMock.Object);
            
            _subject.OnPeerConnected(_peerMock);
        }

        private void ItShouldRetrieveItsLastGameStateFromRedis()
        {
            const bool isLocalPlayer = true;
            const float x = 10.3f;
            const float y = 30.3f;
            const float z = 42.5f;

            var dataWriter = new NetDataWriter();

            dataWriter.Put((int)NET_DATA_TYPE.PlayerPositionsArray);
            dataWriter.Put(_peerId);
            dataWriter.Put(isLocalPlayer);
            dataWriter.Put(x);
            dataWriter.Put(y);
            dataWriter.Put(z);
            
            _subject.DataWriter.Data.Should().BeEquivalentTo(dataWriter.Data);
        }

        private void ItShouldNotCreateAnotherRecordInDatabase()
        {
            _playerModelRepo.GetAll().Count().Should().Be(1);
        }
    }
}