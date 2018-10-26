using System.ComponentModel;
using System.Xml.Serialization;
using ProtoBuf;

// ReSharper disable ExplicitCallerInfoArgument

namespace AutoMcD.PocketGear.Settings {
    [ProtoContract]
    public class ModSettings {
        public const int VERSION = 1;
        private const float IMPACT_TOLERANCE_MULTIPLIER = 1;
        private const bool USE_IMPACT_DAMAGE_HANDLER = true;
        private const int PROTECTION_RADIUS = 2;

        [ProtoMember(3)]
        [DefaultValue(IMPACT_TOLERANCE_MULTIPLIER)]
        [XmlElement(Order = 3)]
        public float ImpactToleranceMultiplier { get; set; } = IMPACT_TOLERANCE_MULTIPLIER;

        [ProtoMember(2)]
        [DefaultValue(USE_IMPACT_DAMAGE_HANDLER)]
        [XmlElement(Order = 2)]
        public bool UseImpactDamageHandler { get; set; } = USE_IMPACT_DAMAGE_HANDLER;

        [ProtoMember(1)]
        [XmlElement(Order = 1)]
        public int Version { get; set; } = VERSION;

        [ProtoMember(4)]
        [DefaultValue(PROTECTION_RADIUS)]
        [XmlElement(Order = 4)]
        public int ProtectionRadius { get; set; } = PROTECTION_RADIUS;
    }
}