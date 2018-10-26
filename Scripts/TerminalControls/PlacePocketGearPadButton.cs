using System.Collections.Generic;
using AutoMcD.PocketGear.Localization;
using AutoMcD.PocketGear.Logic;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Sisk.Utils.Localization.Extensions;
using Sisk.Utils.TerminalControls;
using VRage.Utils;

namespace AutoMcD.PocketGear.TerminalControls {
    public static class PlacePocketGearPadButton {
        private const string ID = nameof(ModText.PlaceLandingPad);

        private static IEnumerable<IMyTerminalAction> _actions;
        private static IMyTerminalControlButton _control;

        public static IEnumerable<IMyTerminalAction> Actions => _actions ?? (_actions = CreateActions());

        public static IMyTerminalControlButton Control => _control ?? (_control = CreateControl());

        private static void Action(IMyTerminalBlock block) {
            var stator = block as IMyMotorStator;
            var logic = stator?.GameLogic?.GetAs<PocketGearBase>();
            logic?.PlacePad();
        }

        private static IEnumerable<IMyTerminalAction> CreateActions() {
            var actions = new List<IMyTerminalAction> {
                Control.CreateButtonAction<IMyMotorAdvancedStator>()
            };

            return actions;
        }

        private static IMyTerminalControlButton CreateControl() {
            var control = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyMotorAdvancedStator>(ID);
            control.Title = MyStringId.GetOrCompute(ModText.PlaceLandingPad.GetString());
            control.Tooltip = MyStringId.GetOrCompute(ModText.Tooltip_PlaceLandingPad.GetString());
            control.Action = Action;
            control.Enabled = Enabled;
            control.SupportsMultipleBlocks = true;
            return control;
        }

        private static bool Enabled(IMyTerminalBlock block) {
            var logic = block.GameLogic?.GetAs<PocketGearBase>();
            var enabled = false;
            if (logic != null) {
                enabled = logic.CanBuiltPad;
            }

            return enabled;
        }
    }
}