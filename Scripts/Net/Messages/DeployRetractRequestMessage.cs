using ProtoBuf;
using Sandbox.ModAPI;
using Sisk.Utils.Net.Messages;

// ReSharper disable ExplicitCallerInfoArgument

namespace Sisk.PocketGear.Net.Messages {
    [ProtoContract]
    public class DeployRetractRequestMessage : IEntityMessage {
        public enum DeployOrRetractData {
            Deploy,
            Retract
        }

        public DeployRetractRequestMessage() { }

        public DeployRetractRequestMessage(long entityId, DeployOrRetractData deployOrRetract) {
            EntityId = entityId;
            DeployOrRetract = deployOrRetract;
        }

        [ProtoMember(2)]
        public DeployOrRetractData DeployOrRetract { get; set; }

        [ProtoMember(11)]
        public long EntityId { get; set; }

        public byte[] Serialize() {
            return MyAPIGateway.Utilities.SerializeToBinary(this);
        }
    }
}