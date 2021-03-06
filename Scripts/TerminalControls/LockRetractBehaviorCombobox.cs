﻿using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Sisk.PocketGear.Data;
using Sisk.PocketGear.Localization;
using Sisk.PocketGear.Logic;
using VRage.ModAPI;
using VRage.Utils;

namespace Sisk.PocketGear.TerminalControls {
    public static class LockRetractBehaviorCombobox {
        private const string ID = nameof(ModText.BlockPropertyTitle_LockRetractBehavior);

        private static IMyTerminalControlCombobox _control;
        public static IMyTerminalControlCombobox Control => _control ?? (_control = CreateControl());

        private static void Content(List<MyTerminalControlComboBoxItem> list) {
            list.AddRange(Enum.GetValues(typeof(LockRetractBehaviors)).Cast<LockRetractBehaviors>().Select(x => new MyTerminalControlComboBoxItem { Key = (long) x, Value = MyStringId.GetOrCompute($"BlockPropertyTitle_LockRetractBehavior_{x.ToString()}") }));
        }

        private static IMyTerminalControlCombobox CreateControl() {
            var control = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCombobox, IMyMotorAdvancedStator>(ID);
            control.Title = ModText.BlockPropertyTitle_LockRetractBehavior;
            control.Tooltip = ModText.BlockPropertyTooltip_LockRetractBehavior;
            control.ComboBoxContent = Content;
            control.Getter = Getter;
            control.Setter = Setter;
            control.Enabled = Controls.IsPocketGearBase;
            control.Visible = Controls.IsPocketGearBase;
            control.SupportsMultipleBlocks = true;
            return control;
        }

        private static long Getter(IMyTerminalBlock block) {
            var logic = block.GameLogic?.GetAs<PocketGearBase>();
            if (logic != null) {
                return (long) logic.CurrentBehavior;
            }

            return (long) LockRetractBehaviors.PreventRetract;
        }

        private static void Setter(IMyTerminalBlock block, long value) {
            var logic = block.GameLogic?.GetAs<PocketGearBase>();
            if (logic != null) {
                logic.CurrentBehavior = (LockRetractBehaviors) value;
            }
        }
    }
}