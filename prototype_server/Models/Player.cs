using System.ComponentModel.DataAnnotations;

#if DEBUG
    using prototype_server.Libs.LiteNetLib;
#else
    using LiteNetLib;
#endif

namespace prototype_server.Models
{
    public class Player : _BaseModel
    {        
        [Required]
        public string Name { get; set; }
        
        public float X, Y, Z;
        public readonly NetPeer Peer;
        public bool Moved, IsLocalPlayer;
        
        public Player(NetPeer peer)
        {
            Peer = peer;
            
            Moved = false;
            IsLocalPlayer = false;
            
            X = 0.0f;
            Y = 0.0f;
            Z = 0.0f;
        }
        
        internal Player() {}
    }
}