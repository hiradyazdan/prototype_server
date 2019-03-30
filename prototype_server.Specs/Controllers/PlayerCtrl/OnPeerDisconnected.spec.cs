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
using prototype_server.Models;
using prototype_server.Specs.Config;
using prototype_server.Specs.Config.Utils;

namespace prototype_server.Specs.Controllers.PlayerCtrl
{   
    public class WhenPlayerGetsDisconnected : ScenarioFor<PlayerController>
    {
        private PlayerController _subject;
        private RedisCacheAdapter _redisCacheAdapter;
        private Player _playerMock;
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
            _playerMock = new Player(_peerMock)
            {
                Name = "user_15000",
                X = 10.3f,
                Y = 30.3f,
                Z = 42.5f
            };
            
            var peerEndpointMock = BitConverter.ToUInt32(IPAddress.Parse("192.168.0.1").GetAddressBytes(), 0);
            
            var disconnectInfo = new DisconnectInfo();
            var ipEndpointMock = new Mock<IPEndPoint>(MockBehavior.Loose, peerEndpointMock, 15000).Object;
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
            
            dbContextMock.Setup(m => m.Set<Player>()).Returns(playerDbSetMock);
            
            var playerModelRepoMock = new Mock<ModelRepository<Player>>(MockBehavior.Loose, dbContextMock.Object);
            
            scopeMock.Setup(m => m.ServiceProvider.GetService(typeof(IRepository<Player>)))
                     .Returns(playerModelRepoMock.Object);
            
            var peerMock = new Mock<NetPeer>(MockBehavior.Loose, ipEndpointMock, 0);

            _peerId = peerEndpointMock;
            _peerMock = peerMock.Object;
            
            _redisCacheAdapter = new RedisCacheAdapter("localhost");

            _subject = new PlayerController(scopeMock.Object, _redisCacheAdapter);
            
            _subject.OnPeerConnected(_peerMock);

            _subject.OnNetworkReceive(_peerMock, readerMock.Object, DeliveryMethod.Sequenced);
            
            _subject.OnPeerDisconnected(_peerMock, disconnectInfo);
        }

        private void ItShouldStoreItsLastGameStateToRedis()
        {
            _redisCacheAdapter.GetCache(_peerId.ToString()).Should().Be("10.3,30.3,42.5");
        }
    }
}