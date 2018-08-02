using System;
using System.Collections.Generic;
using System.Linq;
using AutoMcD.PocketGear.Localization;
using AutoMcD.PocketGear.Logic;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Sisk.Utils.Localization;
using VRage.ModAPI;

// ReSharper disable ArgumentsStyleOther
// ReSharper disable ArgumentsStyleNamedExpression
// ReSharper disable ArgumentsStyleLiteral

namespace AutoMcD.PocketGear.TerminalControls {
    public static class LockRetractBehaviorCombobox {
        public static IMyTerminalControlCombobox Create() {
            var combobox = TerminalControlUtils.CreateCombobox<IMyMotorAdvancedStator>(
                id: nameof(PocketGearText.LockRetractBehavior),
                title: PocketGearText.LockRetractBehavior.String,
                tooltip: PocketGearText.Tooltip_LockRetractBehavior.String,
                content: Content,
                getter: Getter,
                setter: Setter,
                enabled: PocketGearBaseControls.IsPocketGearBase,
                visible: PocketGearBaseControls.IsPocketGearBase,
                supportsMultipleBlocks: true);
            return combobox;
        }

        private static void Content(List<MyTerminalControlComboBoxItem> list) {
            list.AddRange(Enum.GetValues(typeof(LockRetractBehaviors)).Cast<LockRetractBehaviors>().Select(x => new MyTerminalControlComboBoxItem { Key = (long) x, Value = Localize.Get(x.ToString()) }));
        }

        private static long Getter(IMyTerminalBlock block) {
            var logic = block.GameLogic?.GetAs<PocketGearBaseLogic>();
            if (logic != null) {
                return (long) logic.CurrentBehavior;
            }

            return (long) LockRetractBehaviors.PreventRetract;
        }

        private static void Setter(IMyTerminalBlock block, long value) {
            var logic = block.GameLogic?.GetAs<PocketGearBaseLogic>();
            if (logic != null) {
                logic.CurrentBehavior = (LockRetractBehaviors) value;
            }
        }
    }
}