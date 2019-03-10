using LiteNetLib;

namespace prototype_server
{
    public class NetworkPlayer
    {
        public float x = 0.0f;
        public float y = 0.0f;
        public float z = 0.0f;

        public readonly NetPeer netPeer;
        public bool moved = false;


        public NetworkPlayer(NetPeer peer)
        {
            netPeer = peer;
        }

    }
}
