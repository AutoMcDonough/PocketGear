using ProtoBuf;

namespace AutoMcD.PocketGear.Net {
    [ProtoContract]
    [ProtoInclude(10, typeof(SettingsSyncResponseMessage))]
    public interface ISyncResponseMessage : IMessage {
        [ProtoMember(2)]
        ulong Requester { get; set; }

        [ProtoMember(1)]
        ulong Sender { get; set; }
    }
}