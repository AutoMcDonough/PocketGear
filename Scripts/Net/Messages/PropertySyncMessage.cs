using ProtoBuf;
using Sandbox.ModAPI;
using Sisk.Utils.Net.Messages;

// ReSharper disable ExplicitCallerInfoArgument

namespace AutoMcD.PocketGear.Net.Messages {
    [ProtoContract]
    public class PropertySyncMessage : IEntityMessage {
        public PropertySyncMessage(long entityId, string name, object value) : this(entityId, name, MyAPIGateway.Utilities.SerializeToBinary(value)) { }

        public PropertySyncMessage(long entityId, string name, byte[] value) {
            EntityId = entityId;
            Name = name;
            Value = value;
        }

        [ProtoMember(2)]
        public string Name { get; set; }

        [ProtoMember(3)]
        public byte[] Value { get; set; }

        [ProtoMember(1)]
        public long EntityId { get; set; }

        public byte[] Serialize() {
            return MyAPIGateway.Utilities.SerializeToBinary(this);
        }

        public TResult GetValueAs<TResult>() {
            return MyAPIGateway.Utilities.SerializeFromBinary<TResult>(Value);
        }
    }
}