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

namespace prototype_server.Specs.Controllers
{   
    public class WhenAPlayerConnectsForTheFirstTime : ScenarioFor<PlayerController>
    {
        private enum NET_DATA_TYPE
        {
            PlayerPosition,
            PlayerPositionsArray,
        }
        
        private PlayerController _subject;
        private DbContextOptions<GameDbContext> _dbCtxOptions;
        private ModelRepository<Player> _playerModelRepo;
        private NetPeer _peerMock;

        private void EstablishContext()
        {
            _dbCtxOptions = new DbContextOptionsBuilder<GameDbContext>()
                .UseInMemoryDatabase(databaseName: "game_db")
                .Options;
            
            var ipEndpointMock = new Mock<IPEndPoint>(MockBehavior.Loose, 192168017, 15000).Object;
            var scopeMock = new Mock<IServiceScope>();
            var redisCacheMock = new Mock<RedisCache>(MockBehavior.Loose, "localhost").Object;
            var dbContext = new GameDbContext(_dbCtxOptions);
            
            _playerModelRepo = new ModelRepository<Player>(dbContext);
            _peerMock = new Mock<NetPeer>(MockBehavior.Loose, ipEndpointMock, 0).Object;
            
            scopeMock.Setup(m => m.ServiceProvider.GetService(typeof(IRepository<Player>)))
                     .Returns(_playerModelRepo);

            _subject = new PlayerController(scopeMock.Object, redisCacheMock);
            
            _subject.OnPeerConnected(_peerMock);
        }

        private void ItShouldReceiveInitialGameState()
        {    
            const long key = 0;
            const bool isLocalPlayer = true;
            const float x = 0.0f;
            const float y = 0.0f;
            const float z = 0.0f;

            var dataWriter = new NetDataWriter();

            dataWriter.Put((int)NET_DATA_TYPE.PlayerPositionsArray);
            dataWriter.Put(key);
            dataWriter.Put(isLocalPlayer);
            dataWriter.Put(x);
            dataWriter.Put(y);
            dataWriter.Put(z);
            
            _subject.DataWriter.Data.Should().Equal(dataWriter.Data);
        }

        private void ItShouldCreateARecordInDatabase()
        {
            _playerModelRepo.GetAll().Count().Should().Be(1);
        }
    }

    public class WhenAPlayerDisconnectsAndConnectsAgain : ScenarioFor<PlayerController>
    {
        private void EstablishContext()
        {
            
        }
    }
}