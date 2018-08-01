using AutoMcD.PocketGear.Settings;
using ProtoBuf;

namespace AutoMcD.PocketGear.Net {
    [ProtoContract]
    public class SettingsSyncResponseMessage : ISyncResponseMessage {
        [ProtoMember(1)]
        public ModSettings Settings { get; set; }

        public ulong Sender { get; set; }
        public ulong Requester { get; set; }
    }
}