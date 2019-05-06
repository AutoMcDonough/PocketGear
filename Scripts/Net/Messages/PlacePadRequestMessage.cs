using ProtoBuf;
using Sandbox.ModAPI;
using Sisk.Utils.Net.Messages;

// ReSharper disable ExplicitCallerInfoArgument

namespace Sisk.PocketGear.Net.Messages {
    [ProtoContract]
    public class PlacePadRequestMessage : IEntityMessage {
        public PlacePadRequestMessage() { }

        public PlacePadRequestMessage(long entityId) {
            EntityId = entityId;
        }

        [ProtoMember(11)]
        public long EntityId { get; set; }

        public byte[] Serialize() {
            return MyAPIGateway.Utilities.SerializeToBinary(this);
        }
    }
}