using System;
using System.Collections.Generic;
using System.Text;
using LiteNetLib;

namespace PrototypeServer
{
    public class NetworkPlayer
    {
        public float x = 0.0f;
        public float y = 0.0f;
        public float z = 0.0f;

        public NetPeer netPeer;
        public bool moved = false;


        public NetworkPlayer(NetPeer peer)
        {
            this.netPeer = peer;
        }

    }
}
