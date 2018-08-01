using ProtoBuf;

namespace AutoMcD.PocketGear.Net {
    [ProtoContract]
    [ProtoInclude(1, typeof(IEntitySyncMessage))]
    [ProtoInclude(2, typeof(ISyncRequestMessage))]
    [ProtoInclude(3, typeof(ISyncResponseMessage))]
    public interface IMessage { }
}