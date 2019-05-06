using LiteNetLib.Utils;
using MessagePack;
using UnityEngine;

namespace prototype_server.Serializers
{
    [MessagePackObject]
    public class PositionPacket : _BasePacket
    {
        [Key(3)] 
        public float X { get; set; }
        [Key(4)] 
        public float Y { get; set; }
        [Key(5)] 
        public float Z { get; set; }

        [SerializationConstructor]
        public PositionPacket(int objectType, long id, bool isLocal, float x, float y, float z) : base(objectType, id, isLocal)
        {
            X = x;
            Y = y;
            Z = z;
        }
        
        /**
         * Deserialization Constructor
         */
        public PositionPacket(byte[] stateBytes) : base(stateBytes)
        {
            var positionState = IsCompressed ? 
                LZ4MessagePackSerializer.Deserialize<PositionPacket>(stateBytes) : 
                MessagePackSerializer.Deserialize<PositionPacket>(stateBytes);
            
            X = positionState.X;
            Y = positionState.Y;
            Z = positionState.Z;
        }
        
        /*
         * If Serialization doesn't work, this ctor can be used temporarily
         */
        public PositionPacket(NetDataReader reader) : base(reader)
        {
            X = reader.GetFloat();
            Y = reader.GetFloat();
            Z = reader.GetFloat();
        }

        public Vector3 ToVector3()
        {
            return new Vector3(X, Y, Z);
        }
        
        public override byte[] ToByteArray()
        {
            return IsCompressed ? 
                LZ4MessagePackSerializer.Serialize(this) : 
                MessagePackSerializer.Serialize(this);
        }
    }
}