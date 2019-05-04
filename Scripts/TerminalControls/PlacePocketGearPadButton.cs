using System.Collections.Generic;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Sisk.PocketGear.Localization;
using Sisk.PocketGear.Logic;
using Sisk.Utils.TerminalControls;

namespace Sisk.PocketGear.TerminalControls {
    public static class PlacePocketGearPadButton {
        private const string ID = nameof(ModText.BlockActionTitle_PlaceLandingPad);

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
            control.Title = ModText.BlockActionTitle_PlaceLandingPad;
            control.Tooltip = ModText.BlockActionTooltip_PlaceLandingPad;
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