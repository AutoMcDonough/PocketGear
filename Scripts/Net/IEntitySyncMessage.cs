using ProtoBuf;

namespace AutoMcD.PocketGear.Net {
    [ProtoContract]
    [ProtoInclude(10, typeof(PropertySyncMessage))]
    public interface IEntitySyncMessage : IMessage {
        [ProtoMember(1)]
        long EntityId { get; set; }
    }
}