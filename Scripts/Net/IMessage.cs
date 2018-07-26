using ProtoBuf;

namespace AutoMcD.PocketGear.Net {
    [ProtoContract]
    [ProtoInclude(1, typeof(IEntitySyncMessage))]
    public interface IMessage { }
}