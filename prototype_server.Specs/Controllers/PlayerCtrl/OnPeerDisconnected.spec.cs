using System;
using System.Net;

using FluentAssertions;
using Moq;

using Microsoft.Extensions.DependencyInjection;

using LiteNetLib;

using prototype_server.Controllers;

using prototype_storage;
using prototype_models.OOD;
using prototype_models.OOD.ORM;
using prototype_models.OOD.Interfaces;

using prototype_server.Specs.Config;
using prototype_server.Specs.Config.Utils;
using prototype_server.Specs.Config.Utils.Helpers;

namespace prototype_server.Specs.Controllers.PlayerCtrl
{   
    public class WhenPlayerGetsDisconnected : ScenarioFor<PlayerController>
    {
        private PlayerController _subject;
        private RedisCacheAdapter _redisCacheAdapter;
        private PlayerModel _playerMock;
        private NetPeer _peerMock;
        private Guid _playerGuid;
        
        private void EstablishContext()
        {
            var peerEndpointBytes = IPAddress.Parse("192.168.0.1").GetAddressBytes();
            var peerEndpoint = BitConverter.ToUInt32(peerEndpointBytes, 0);
            
            _playerGuid = Helpers.ConvertBytesToGuid(peerEndpointBytes);
            
            var disconnectInfo = new DisconnectInfo();
            var ipEndpointMock = new Mock<IPEndPoint>(MockBehavior.Loose, peerEndpoint, 15000).Object;
            var scopeMock = new Mock<IServiceScope>();
            var dbContextMock = new Mock<GameDbContext>();
            var playerDbSetMock = Helpers.GetQueryableMockDbSet(_playerMock);

            const int deliveryMethodHeaderSize = 3;
            const int rawDataSize = deliveryMethodHeaderSize + 
                                    sizeof(int) + 
                                    sizeof(bool) + 
                                    sizeof(float) * 3;
            
            _peerMock = Helpers.GetPeerMock(ipEndpointMock);
            _playerMock = new PlayerModel(_peerMock)
            {
                GUID = _playerGuid,
                Name = "user_15000",
                X = 10.3f,
                Y = 30.3f,
                Z = 42.5f
            };
            
            var readerMock = Helpers.GetReaderMock(_playerMock, NET_DATA_TYPE.PlayerPosition, rawDataSize);
            
            dbContextMock.Setup(m => m.Set<PlayerModel>()).Returns(playerDbSetMock);
            
            var playerModelRepoMock = new Mock<ModelRepository<PlayerModel>>(MockBehavior.Loose, dbContextMock.Object);
            
            scopeMock.Setup(m => m.ServiceProvider.GetService(typeof(IRepository<PlayerModel>)))
                     .Returns(playerModelRepoMock.Object);
            
            _redisCacheAdapter = new RedisCacheAdapter("localhost");

            _subject = new PlayerController(scopeMock.Object, _redisCacheAdapter);
            
            _subject.OnPeerConnected(_peerMock);

            _subject.OnNetworkReceive(_peerMock, readerMock, DeliveryMethod.Sequenced);
            
            _subject.OnPeerDisconnected(_peerMock, disconnectInfo);
        }

        private void ItShouldStoreItsLastGameStateToRedis()
        {
            _redisCacheAdapter.GetCache(_playerGuid.ToString()).Should().Be("10.3,30.3,42.5");
        }
    }
}