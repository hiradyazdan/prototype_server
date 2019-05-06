using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LiteNetLib;

namespace prototype_server.Models
{
    public class Player : _BaseModel
    {
        [Required]
        public Guid GUID { get; set; }
        
        [Required]
        public string Name { get; set; }
        
        [NotMapped]
        public float X { get; set; }
        
        [NotMapped]
        public float Y { get; set; }
        
        [NotMapped]
        public float Z { get; set; }
        
        [NotMapped]
        public NetPeer Peer { get; }
        
        [NotMapped]
        public bool Moved { get; set; }
        
        [NotMapped]
        public bool IsLocal { get; set; }
        
        public Player(NetPeer peer)
        {
            Peer = peer;
            
            Moved = false;
            IsLocal = false;
            
            X = 0.0f;
            Y = 0.0f;
            Z = 0.0f;
        }
        
        internal Player() {}
    }
}