using ProtoBuf;

namespace AutoMcD.PocketGear.Net {
    [ProtoContract]
    public class PropertySyncMessage : IEntitySyncMessage {
        [ProtoMember(1)]
        public string Name { get; set; }

        [ProtoMember(2)]
        public byte[] Value { get; set; }

        public long EntityId { get; set; }
    }
}