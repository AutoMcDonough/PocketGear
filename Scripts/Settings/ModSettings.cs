using System.ComponentModel;
using ProtoBuf;

// ReSharper disable ExplicitCallerInfoArgument
// ReSharper disable ArrangeAccessorOwnerBody

namespace AutoMcD.PocketGear.Settings {
    [ProtoContract]
    public class ModSettings {
        public const int VERSION = 1;
        public const float IMPACT_TOLERANCE_MULTIPLIER = 1;

        [ProtoMember(1)]
        public int Version { get; set; } = VERSION;

        [ProtoMember(2)]
        public bool UseImpactDamageHandler { get; set; } = true;

        [ProtoMember(3)]
        public float ImpactToleranceMultiplier { get; set; } = IMPACT_TOLERANCE_MULTIPLIER;
    }
}