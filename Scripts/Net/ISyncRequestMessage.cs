using ProtoBuf;

namespace AutoMcD.PocketGear.Net {
    [ProtoContract]
    [ProtoInclude(10, typeof(SettingsSyncRequestMessage))]
    public interface ISyncRequestMessage : IMessage {
        [ProtoMember(1)]
        ulong Sender { get; set; }
    }
}