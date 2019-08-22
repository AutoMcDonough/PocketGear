using System.ComponentModel;
using System.Xml.Serialization;
using ProtoBuf;
using Sisk.PocketGear.Data;

// ReSharper disable ExplicitCallerInfoArgument

namespace Sisk.PocketGear.Settings.V1 {
    [ProtoContract]
    public class PocketGearBaseSettings {
        public const float DEFAULT_VELOCITY_RPM = 1f;
        public const string GUID = "E00AEA7D-B2A5-4216-BB25-49D375B1B3C3";
        public const int VERSION = 1;
        private const bool IS_PAD_ATTACHED = false;

        private const LockRetractBehaviors LOCK_RETRACT_BEHAVIOR = LockRetractBehaviors.PreventRetract;
        private const bool SHOULD_DEPLOY = false;

        [ProtoMember(3)]
        [DefaultValue(1)]
        public float DeployVelocity { get; set; } = DEFAULT_VELOCITY_RPM;

        [ProtoMember(2)]
        [DefaultValue(IS_PAD_ATTACHED)]
        public bool IsPadAttached { get; set; } = IS_PAD_ATTACHED;

        [ProtoMember(4)]
        [DefaultValue(LOCK_RETRACT_BEHAVIOR)]
        public LockRetractBehaviors LockRetractBehavior { get; set; } = LOCK_RETRACT_BEHAVIOR;

        [ProtoMember(5)]
        [DefaultValue(SHOULD_DEPLOY)]
        public bool ShouldDeploy { get; set; } = SHOULD_DEPLOY;

        [ProtoMember(1)]
        [XmlElement(Order = 1)]
        public int Version { get; set; } = VERSION;
    }
}