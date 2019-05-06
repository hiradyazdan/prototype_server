using LiteNetLib.Utils;
using MessagePack;

namespace prototype_server.Serializers
{
    public interface IPacket
    {
        int ObjectType { get; set; }
        long Id { get; set; }
        bool IsLocal { get; set; }
        byte[] ToByteArray();
    }
    
    [MessagePackObject]
    public class _BasePacket : IPacket
    {
        [Key(0)] 
        public int ObjectType { get; set; }
        [Key(1)] 
        public long Id { get; set; }
        [Key(2)] 
        public bool IsLocal { get; set; }
        
//        [IgnoreMember]
        public static bool IsCompressed { protected get; set; } = true;

        [SerializationConstructor]
        public _BasePacket(int objectType, long id, bool isLocal)
        {
            ObjectType = objectType;
            Id = id;
            IsLocal = isLocal;
        }

        /**
         * Deserialization Constructor
         */
        protected _BasePacket(byte[] stateBytes)
        {
            var state = IsCompressed ? 
                LZ4MessagePackSerializer.Deserialize<_BasePacket>(stateBytes) : 
                MessagePackSerializer.Deserialize<_BasePacket>(stateBytes);
            
            ObjectType = state.ObjectType;
            Id = state.Id;
            IsLocal = state.IsLocal;
        }

        /*
         * If Serialization doesn't work, this ctor can be used temporarily
         */
        protected _BasePacket(NetDataReader reader)
        {
            ObjectType = reader.GetInt();
            Id = reader.GetLong();
            IsLocal = reader.GetBool();
        }
        
        public virtual byte[] ToByteArray()
        {
            return IsCompressed ? 
                LZ4MessagePackSerializer.Serialize(this) : 
                MessagePackSerializer.Serialize(this);
        }
    }
}