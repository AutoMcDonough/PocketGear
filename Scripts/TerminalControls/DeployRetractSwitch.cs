using System.Collections.Generic;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Sisk.PocketGear.Localization;
using Sisk.PocketGear.Logic;
using Sisk.Utils.TerminalControls;

namespace Sisk.PocketGear.TerminalControls {
    public static class DeployRetractSwitch {
        private const string ID = nameof(ModText.BlockPropertyTitle_SwitchDeployState);
        private static IEnumerable<IMyTerminalAction> _actions;
        private static IMyTerminalControlOnOffSwitch _control;
        private static IMyTerminalControlProperty<bool> _property;

        public static IEnumerable<IMyTerminalAction> Actions => _actions ?? (_actions = CreateActions());

        public static IMyTerminalControlOnOffSwitch Control => _control ?? (_control = CreateControl());

        public static IMyTerminalControlProperty<bool> Property => _property ?? (_property = CreateProperty());

        private static IEnumerable<IMyTerminalAction> CreateActions() {
            var actions = new List<IMyTerminalAction> {
                Control.CreateToggleAction<IMyMotorAdvancedStator>(),
                Control.CreateOnAction<IMyMotorAdvancedStator>(),
                Control.CreateOffAction<IMyMotorAdvancedStator>()
            };

            return actions;
        }

        private static IMyTerminalControlOnOffSwitch CreateControl() {
            var control = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, IMyMotorAdvancedStator>(ID);
            control.Title = ModText.BlockPropertyTitle_SwitchDeployState;
            control.Tooltip = ModText.BlockPropertyTooltip_SwitchDeployState;
            control.OnText = ModText.BlockPropertyTitle_SwitchDeployState_Deploy;
            control.OffText = ModText.BlockPropertyTitle_SwitchDeployState_Retract;
            control.Getter = Getter;
            control.Setter = Setter;
            control.SupportsMultipleBlocks = true;
            return control;
        }

        private static IMyTerminalControlProperty<bool> CreateProperty() {
            return Control.CreateProperty<IMyMotorAdvancedStator>();
        }

        private static bool Getter(IMyTerminalBlock block) {
            var logic = block.GameLogic?.GetAs<PocketGearBase>();
            return logic != null && logic.IsDeploying;
        }

        private static void Setter(IMyTerminalBlock block, bool value) {
            var logic = block.GameLogic?.GetAs<PocketGearBase>();

            if (value) {
                logic?.Deploy();
            } else {
                logic?.Retract();
            }
        }
    }
}