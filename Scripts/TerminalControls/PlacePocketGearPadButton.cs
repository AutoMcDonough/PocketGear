using AutoMcD.PocketGear.Localization;
using AutoMcD.PocketGear.Logic;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Sisk.Utils.Localization.Extensions;

// ReSharper disable UseNegatedPatternMatching
// ReSharper disable ArgumentsStyleOther
// ReSharper disable ArgumentsStyleNamedExpression
// ReSharper disable ArgumentsStyleLiteral

namespace AutoMcD.PocketGear.TerminalControls {
    public static class PlacePocketGearPadButton {
        public static IMyTerminalControlButton Create() {
            var button = TerminalControlUtils.CreateButton<IMyMotorAdvancedStator>(
                id: nameof(ModText.PlaceLandingPad),
                title: ModText.PlaceLandingPad.GetString(),
                tooltip: ModText.Tooltip_PlaceLandingPad.GetString(),
                action: Action,
                enabled: Enabled,
                visible: PocketGearBaseControls.IsPocketGearBase,
                supportsMultipleBlocks: true);
            return button;
        }

        private static void Action(IMyTerminalBlock block) {
            var stator = block as IMyMotorStator;
            if (stator == null) {
                return;
            }

            var top = stator.Top;
            if (top == null) {
                return;
            }

            var logic = top.GameLogic?.GetAs<PocketGearPartLogic>();
            if (logic != null) {
                logic.PlacePocketGearPad();
            }
        }

        private static bool Enabled(IMyTerminalBlock block) {
            if (!PocketGearBaseControls.IsPocketGearBase(block)) {
                return false;
            }

            var logic = block.GameLogic?.GetAs<PocketGearBaseLogic>();
            var enabled = false;
            if (logic != null) {
                enabled = logic.CanPocketGearBeBuilt;
            }

            return enabled;
        }
    }
}