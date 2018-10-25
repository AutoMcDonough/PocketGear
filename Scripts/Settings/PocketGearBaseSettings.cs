using System.ComponentModel;
using AutoMcD.PocketGear.Logic;
using ProtoBuf;

// ReSharper disable ExplicitCallerInfoArgument

namespace AutoMcD.PocketGear.Settings {
    [ProtoContract]
    public class PocketGearBaseSettings {
        public const float DEFAULT_VELOCITY_RPM = 1f;
        public const string GUID = "E00AEA7D-B2A5-4216-BB25-49D375B1B3C3";

        private const LockRetractBehaviors LOCK_RETRACT_BEHAVIOR = LockRetractBehaviors.PreventRetract;
        private const bool SHOULD_DEPLOY = false;

        [ProtoMember(1)]
        [DefaultValue(1)]
        public float DeployVelocity { get; set; } = DEFAULT_VELOCITY_RPM;

        [ProtoMember(2)]
        [DefaultValue(LOCK_RETRACT_BEHAVIOR)]
        public LockRetractBehaviors LockRetractBehavior { get; set; } = LOCK_RETRACT_BEHAVIOR;

        [ProtoMember(3)]
        [DefaultValue(SHOULD_DEPLOY)]
        public bool ShouldDeploy { get; set; } = SHOULD_DEPLOY;
    }
}