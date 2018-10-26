using System;
using System.Collections.Generic;
using System.Linq;
using AutoMcD.PocketGear.Data;
using AutoMcD.PocketGear.Localization;
using AutoMcD.PocketGear.Logic;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Sisk.Utils.Localization;
using Sisk.Utils.Localization.Extensions;
using VRage.ModAPI;
using VRage.Utils;

namespace AutoMcD.PocketGear.TerminalControls {
    public static class LockRetractBehaviorCombobox {
        private const string ID = nameof(ModText.LockRetractBehavior);

        private static IMyTerminalControlCombobox _control;
        public static IMyTerminalControlCombobox Control => _control ?? (_control = CreateControl());

        private static void Content(List<MyTerminalControlComboBoxItem> list) {
            list.AddRange(Enum.GetValues(typeof(LockRetractBehaviors)).Cast<LockRetractBehaviors>().Select(x => new MyTerminalControlComboBoxItem { Key = (long) x, Value = MyStringId.GetOrCompute(Texts.GetString(x.ToString())) }));
        }

        private static IMyTerminalControlCombobox CreateControl() {
            var control = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCombobox, IMyMotorAdvancedStator>(ID);
            control.Title = MyStringId.GetOrCompute(ModText.LockRetractBehavior.GetString());
            control.Tooltip = MyStringId.GetOrCompute(ModText.Tooltip_LockRetractBehavior.GetString());
            control.ComboBoxContent = Content;
            control.Getter = Getter;
            control.Setter = Setter;
            control.SupportsMultipleBlocks = true;
            return control;
        }

        private static long Getter(IMyTerminalBlock block) {
            var logic = block.GameLogic?.GetAs<Logic.PocketGearBase>();
            if (logic != null) {
                return (long) logic.CurrentBehavior;
            }

            return (long) LockRetractBehaviors.PreventRetract;
        }

        private static void Setter(IMyTerminalBlock block, long value) {
            var logic = block.GameLogic?.GetAs<Logic.PocketGearBase>();
            if (logic != null) {
                logic.CurrentBehavior = (LockRetractBehaviors) value;
            }
        }
    }
}