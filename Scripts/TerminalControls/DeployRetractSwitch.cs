﻿using AutoMcD.PocketGear.Localization;
using AutoMcD.PocketGear.Logic;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Sisk.Utils.Localization.Extensions;

// ReSharper disable ArgumentsStyleOther
// ReSharper disable ArgumentsStyleNamedExpression
// ReSharper disable ArgumentsStyleLiteral

namespace AutoMcD.PocketGear.TerminalControls {
    public static class DeployRetractSwitch {
        public static IMyTerminalControlOnOffSwitch Create() {
            var @switch = TerminalControlUtils.CreateOnOffSwitch<IMyMotorAdvancedStator>(
                id: nameof(ModText.SwitchDeployState),
                title: ModText.SwitchDeployState.GetString(),
                tooltip: ModText.Tooltip_SwitchDeployState.GetString(),
                onText: ModText.Deploy.GetString(),
                offText: ModText.Retract.GetString(),
                getter: Getter,
                setter: Setter,
                enabled: Enabled,
                visible: PocketGearBaseControls.IsPocketGearBase,
                supportsMultipleBlocks: true);
            return @switch;
        }

        private static bool Enabled(IMyTerminalBlock block) {
            if (block == null) {
                return false;
            }

            if (!PocketGearBaseControls.IsPocketGearBase(block)) {
                return false;
            }

            var logic = block.GameLogic?.GetAs<PocketGearBaseLogic>();
            var enabled = false;
            if (logic != null) {
                enabled = logic.CanRetract;
            }

            return enabled;
        }

        private static bool Getter(IMyTerminalBlock block) {
            var logic = block.GameLogic?.GetAs<PocketGearBaseLogic>();
            if (logic != null) {
                return logic.IsDeploying;
            }

            return false;
        }

        private static void Setter(IMyTerminalBlock block, bool value) {
            var logic = block.GameLogic?.GetAs<PocketGearBaseLogic>();
            if (logic != null) {
                logic.SwitchDeployState(value);
            }
        }
    }
}