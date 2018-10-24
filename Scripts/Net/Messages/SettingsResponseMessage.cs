using AutoMcD.PocketGear.Settings;
using ProtoBuf;
using Sandbox.ModAPI;
using Sisk.Utils.Net.Messages;

// ReSharper disable ExplicitCallerInfoArgument

namespace AutoMcD.PocketGear.Net.Messages {
    [ProtoContract]
    public class SettingsResponseMessage : IMessage {
        [ProtoMember(2)]
        public ModSettings Settings { get; set; }

        [ProtoMember(1)]
        public ulong SteamId { get; set; }

        public byte[] Serialize() {
            return MyAPIGateway.Utilities.SerializeToBinary(this);
        }
    }
}