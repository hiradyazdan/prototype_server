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
            _redisCache = new RedisCache("localhost");
            _playerMock = new Player(_peerMock)
            {
                Name = "user_15000",
                IsLocalPlayer = true,
                X = 12.3f,
                Y = 30.3f,
                Z = 42.5f
            };
            
            var ipEndpointMock = new Mock<IPEndPoint>(MockBehavior.Loose, 192168017, 15000).Object;
            var scopeMock = new Mock<IServiceScope>();
            var dbContextMock = new Mock<GameDbContext>();
            var playerDbSetMock = GetQueryableMockDbSet(_playerMock);
            var readerMock = new Mock<NetPacketReader>();
            
            const int deliveryMethodHeaderSize = 3;
            const int rawDataSize = deliveryMethodHeaderSize + 
                                    sizeof(int) + 
                                    sizeof(bool) + 
                                    sizeof(float) * 3;

            readerMock.Setup(m => m.RawData).Returns(new byte[rawDataSize]);
            readerMock.Setup(m => m.RawDataSize).Returns(rawDataSize);
            readerMock.Setup(m => m.GetInt()).Returns((int) NET_DATA_TYPE.PlayerPosition);
            readerMock.Setup(m => m.GetBool()).Returns(_playerMock.IsLocalPlayer);
            readerMock.SetupSequence(m => m.GetFloat())
                      .Returns(_playerMock.X)
                      .Returns(_playerMock.Y)
                      .Returns(_playerMock.Z);
            
            _peerMock = new Mock<NetPeer>(MockBehavior.Loose, ipEndpointMock, 0).Object;
            
            dbContextMock.Setup(m => m.Set<Player>()).Returns(playerDbSetMock);
            
            var playerModelRepoMock = new Mock<ModelRepository<Player>>(MockBehavior.Loose, dbContextMock.Object);
            
            scopeMock.Setup(m => m.ServiceProvider.GetService(typeof(IRepository<Player>)))
                     .Returns(playerModelRepoMock.Object);

            _subject = new PlayerController(scopeMock.Object, _redisCache);

            _subject.OnPeerConnected(_peerMock);
            
            _subject.OnNetworkReceive(_peerMock, readerMock.Object, DeliveryMethod.Sequenced);
        }
        
        private void ItShouldSyncGameStateWithAllConnectedClients()
        {
            _subject.SyncWithConnectedClients();

            var dataWriter = new NetDataWriter();
            
            const long key = 0;

            dataWriter.Put((int)NET_DATA_TYPE.PlayerPositionsArray);
            dataWriter.Put(key);
            dataWriter.Put(_playerMock.IsLocalPlayer);
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
            _redisCache = new RedisCache("localhost");
            _playerMock = new Player(_peerMock)
            {
                Name = "user_15000",
                IsLocalPlayer = true,
                X = 12.3f,
                Y = 30.3f,
                Z = 42.5f
            };
            
            var ipEndpointMock = new Mock<IPEndPoint>(MockBehavior.Loose, 192168017, 15000).Object;
            var scopeMock = new Mock<IServiceScope>();
            var dbContextMock = new Mock<GameDbContext>();
            var playerDbSetMock = GetQueryableMockDbSet(_playerMock);
            var readerMock = new Mock<NetPacketReader>();

            const int deliveryMethodHeaderSize = 3;
            const int wrongRawDataSize = deliveryMethodHeaderSize + 
                                         sizeof(int) + 
                                         sizeof(float) * 3;

            readerMock.Setup(m => m.RawData).Returns(new byte[wrongRawDataSize]);
            readerMock.Setup(m => m.RawDataSize).Returns(wrongRawDataSize);
            
            _peerMock = new Mock<NetPeer>(MockBehavior.Loose, ipEndpointMock, 0).Object;
            
            dbContextMock.Setup(m => m.Set<Player>()).Returns(playerDbSetMock);
            
            var playerModelRepoMock = new Mock<ModelRepository<Player>>(MockBehavior.Loose, dbContextMock.Object);
            
            scopeMock.Setup(m => m.ServiceProvider.GetService(typeof(IRepository<Player>)))
                     .Returns(playerModelRepoMock.Object);

            _subject = new PlayerController(scopeMock.Object, _redisCache);

            _subject.OnPeerConnected(_peerMock);
            
            _subject.OnNetworkReceive(_peerMock, readerMock.Object, DeliveryMethod.Sequenced);
        }

        private void ItShouldNotSyncGameStateWithAnyClients()
        {
            _subject.SyncWithConnectedClients();
            
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
            _redisCache = new RedisCache("localhost");
            _playerMock = new Player(_peerMock)
            {
                Name = "user_15000",
                IsLocalPlayer = true,
                X = 12.3f,
                Y = 30.3f,
                Z = 42.5f
            };
            
            var ipEndpointMock = new Mock<IPEndPoint>(MockBehavior.Loose, 192168017, 15000).Object;
            var scopeMock = new Mock<IServiceScope>();
            var dbContextMock = new Mock<GameDbContext>();
            var playerDbSetMock = GetQueryableMockDbSet(_playerMock);
            var readerMock = new Mock<NetPacketReader>();

            const int deliveryMethodHeaderSize = 3;
            const int rawDataSize = deliveryMethodHeaderSize + 
                                         sizeof(int) +
                                         sizeof(bool) +
                                         sizeof(float) * 3;

            readerMock.Setup(m => m.RawData).Returns(new byte[rawDataSize]);
            readerMock.Setup(m => m.RawDataSize).Returns(rawDataSize);
            readerMock.Setup(m => m.GetInt()).Returns((int) NET_DATA_TYPE.NotPlayerPosition);
            
            _peerMock = new Mock<NetPeer>(MockBehavior.Loose, ipEndpointMock, 0).Object;
            
            dbContextMock.Setup(m => m.Set<Player>()).Returns(playerDbSetMock);
            
            var playerModelRepoMock = new Mock<ModelRepository<Player>>(MockBehavior.Loose, dbContextMock.Object);
            
            scopeMock.Setup(m => m.ServiceProvider.GetService(typeof(IRepository<Player>)))
                     .Returns(playerModelRepoMock.Object);

            _subject = new PlayerController(scopeMock.Object, _redisCache);

            _subject.OnPeerConnected(_peerMock);
            
            _subject.OnNetworkReceive(_peerMock, readerMock.Object, DeliveryMethod.Sequenced);
        }

        private void ItShouldNotSyncMoveStateWithAnyClients()
        {
            _subject.SyncWithConnectedClients();
            
            var dataWriter = new NetDataWriter();

            dataWriter.Put((int)NET_DATA_TYPE.PlayerPositionsArray);
            
            _subject.DataWriter.Data.Should().BeEquivalentTo(dataWriter.Data);
        }
    }
}