using System.Text;
using AutoMcD.PocketGear.Localization;
using AutoMcD.PocketGear.Logic;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Sisk.Utils.Localization.Extensions;

// ReSharper disable ArgumentsStyleOther
// ReSharper disable ArgumentsStyleNamedExpression
// ReSharper disable ArgumentsStyleLiteral

namespace AutoMcD.PocketGear.TerminalControls {
    public static class DeployVelocitySlider {
        public static IMyTerminalControlSlider Create() {
            var slider = TerminalControlUtils.CreateSlider<IMyMotorAdvancedStator>(
                id: nameof(ModText.DeployVelocity),
                title: ModText.DeployVelocity.GetString(),
                tooltip: ModText.Tooltip_DeployVelocity.GetString(),
                writer: Writer,
                getter: Getter,
                setter: Setter,
                min: Min,
                max: Max,
                enabled: PocketGearBaseControls.IsPocketGearBase,
                visible: PocketGearBaseControls.IsPocketGearBase,
                supportsMultipleBlocks: true);
            return slider;
        }

        private static float Getter(IMyTerminalBlock block) {
            var logic = block.GameLogic?.GetAs<PocketGearBaseLogic>();
            if (logic != null) {
                return logic.DeployVelocity;
            }

            return 0;
        }

        private static float Max(IMyTerminalBlock block) {
            return (block as IMyMotorAdvancedStator)?.MaxRotorAngularVelocity * 9.549296f ?? 1;
        }

        private static float Min(IMyTerminalBlock block) {
            return 0;
        }

        private static void Setter(IMyTerminalBlock block, float value) {
            var logic = block.GameLogic?.GetAs<PocketGearBaseLogic>();
            if (logic != null) {
                logic.DeployVelocity = value;
            }
        }

        private static void Writer(IMyTerminalBlock block, StringBuilder builder) {
            builder.Append($"{block.GameLogic?.GetAs<PocketGearBaseLogic>()?.DeployVelocity:N2} rpm");
        }
    }
}