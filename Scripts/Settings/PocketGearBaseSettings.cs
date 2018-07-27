﻿using System.ComponentModel;
using ProtoBuf;

// ReSharper disable ArrangeAccessorOwnerBody

namespace AutoMcD.PocketGear.Settings {
    [ProtoContract]
    public class PocketGearBaseSettings {
        public const float DEFAULT_VELOCITY_RPM = 1f;
        public const string GUID = "E00AEA7D-B2A5-4216-BB25-49D375B1B3C3";

        [ProtoMember(1)]
        [DefaultValue(1)]
        public float DeployVelocity { get; set; } = DEFAULT_VELOCITY_RPM;
    }
}