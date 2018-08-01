using ProtoBuf;

namespace AutoMcD.PocketGear.Net {
    [ProtoContract]
    internal class MessageWrapper {
        [ProtoMember(2)]
        public IMessage Message { get; set; }

        [ProtoMember(1)]
        public ulong Sender { get; set; }
    }
}