﻿using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Sisk.PocketGear.Localization;
using Sisk.PocketGear.Logic;
using Sisk.Utils.TerminalControls;

namespace Sisk.PocketGear.TerminalControls {
    public static class DeployVelocitySlider {
        private const string ID = nameof(ModText.BlockPropertyTitle_DeployVelocity);

        private static IEnumerable<IMyTerminalAction> _actions;
        private static IMyTerminalControlSlider _control;
        private static IMyTerminalControlProperty<float> _property;

        public static IEnumerable<IMyTerminalAction> Actions => _actions ?? (_actions = CreateActions());

        public static IMyTerminalControlSlider Control => _control ?? (_control = CreateControl());

        public static IMyTerminalControlProperty<float> Property => _property ?? (_property = CreateProperty());

        private static IEnumerable<IMyTerminalAction> CreateActions() {
            var actions = new List<IMyTerminalAction> {
                Control.CreateResetAction<IMyMotorAdvancedStator>(DefaultValue),
                Control.CreateIncreaseAction<IMyMotorAdvancedStator>(0.1f, MinGetter, MaxGetter),
                Control.CreateDecreaseAction<IMyMotorAdvancedStator>(0.1f, MinGetter, MaxGetter)
            };

            return actions;
        }

        private static IMyTerminalControlSlider CreateControl() {
            var control = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyMotorAdvancedStator>(ID);
            control.Title = ModText.BlockPropertyTitle_DeployVelocity;
            control.Tooltip = ModText.BlockPropertyTooltip_DeployVelocity;
            control.Writer = Writer;
            control.Getter = Getter;
            control.Setter = Setter;
            control.SetLimits(MinGetter, MaxGetter);
            control.SupportsMultipleBlocks = true;
            control.Enabled = Controls.IsPocketGearBase;
            control.Visible = Controls.IsPocketGearBase;
            return control;
        }

        private static IMyTerminalControlProperty<float> CreateProperty() {
            return Control.CreateProperty<IMyMotorAdvancedStator>();
        }

        private static float DefaultValue(IMyTerminalBlock block) {
            return MinGetter(block);
        }

        private static float Getter(IMyTerminalBlock block) {
            var logic = block.GameLogic?.GetAs<PocketGearBase>();
            if (logic != null) {
                return logic.DeployVelocity;
            }

            return 0;
        }

        private static float MaxGetter(IMyTerminalBlock block) {
            return (block as IMyMotorAdvancedStator)?.MaxRotorAngularVelocity * 9.549296f ?? 1;
        }

        private static float MinGetter(IMyTerminalBlock block) {
            return 0;
        }

        private static void Setter(IMyTerminalBlock block, float value) {
            var logic = block.GameLogic?.GetAs<PocketGearBase>();
            if (logic != null) {
                logic.DeployVelocity = value;
            }
        }

        private static void Writer(IMyTerminalBlock block, StringBuilder builder) {
            builder.Append($"{Getter(block):F2} rpm");
        }
    }
}