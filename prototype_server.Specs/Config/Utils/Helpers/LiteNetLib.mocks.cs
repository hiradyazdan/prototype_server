using System.Linq;
using System.Net;
using System.Reflection;
using LiteNetLib;
using LiteNetLib.Utils;
using Moq;
using prototype_models.OOD;
using prototype_server.Specs.Controllers.PlayerCtrl;

namespace prototype_server.Specs.Config.Utils.Helpers
{
    internal static partial class Helpers
    {
        // currently Reflection is the only solution to mock LiteNetLib classes
        // as they're mostly internal and sealed
        internal static NetPacketReader GetReaderMock(PlayerModel player, NET_DATA_TYPE netDataType, int rawDataSize)
        {
            const BindingFlags bindingAttr = BindingFlags.NonPublic | BindingFlags.Instance;
            
            var netPacketReaderCtor = typeof(NetPacketReader).GetTypeInfo().DeclaredConstructors.First();
            var readerMock = netPacketReaderCtor.Invoke(new object[] { null, null }) as NetPacketReader;            
            var readerMockType = readerMock?.GetType();
            var dataWriter = new NetDataWriter();

            dataWriter.Put((int)netDataType);
            dataWriter.Put(player.IsLocal);
            dataWriter.Put(player.X);
            dataWriter.Put(player.Y);
            dataWriter.Put(player.Z);
            
            readerMockType?.GetField("_data", bindingAttr).SetValue(readerMock,dataWriter.Data);
            readerMockType?.GetField("_dataSize", bindingAttr).SetValue(readerMock, rawDataSize);

            return readerMock;
        }

        internal static NetPeer GetPeerMock(IPEndPoint ipEndpointMock)
        {
            var iNetEventListenerMock = new Mock<INetEventListener>().Object;
            var netManager = new NetManager(iNetEventListenerMock);
            var netPeerCtor = typeof(NetPeer).GetTypeInfo().DeclaredConstructors.First();
            
            return netPeerCtor.Invoke(new object[] { netManager, ipEndpointMock, 0 }) as NetPeer;
        }
    }
}