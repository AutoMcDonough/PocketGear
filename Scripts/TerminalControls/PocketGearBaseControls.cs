using System.Collections.Generic;
using AutoMcD.PocketGear.Logic;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Sisk.Utils.Profiler;

// ReSharper disable InlineOutVariableDeclaration
// ReSharper disable UsePatternMatching

namespace AutoMcD.PocketGear.TerminalControls {
    public static class PocketGearBaseControls {
        private static readonly HashSet<string> HiddenActions = new HashSet<string> { "Add Small Top Part", "IncreaseLowerLimit", "DecreaseLowerLimit", "ResetLowerLimit", "IncreaseUpperLimit", "DecreaseUpperLimit", "ResetUpperLimit", "IncreaseDisplacement", "DecreaseDisplacement", "ResetDisplacement", "RotorLock", "Reverse", "IncreaseVelocity", "DecreaseVelocity", "ResetVelocity" };
        private static readonly HashSet<string> HiddenControls = new HashSet<string> { "Add Small Top Part", "LowerLimit", "UpperLimit", "Displacement", "RotorLock", "Reverse", "Velocity" };
        public static bool AreTerminalControlsInitialized { get; private set; }
        public static IMyTerminalControlOnOffSwitch DeployRetractSwitch { get; private set; }
        public static IMyTerminalControlSlider DeployVelocitySlider { get; private set; }
        public static IMyTerminalControlCombobox LockRetractBehaviorCombobox { get; private set; }
        public static IMyTerminalControlButton PlacePocketGearPadButton { get; private set; }

        public static void InitializeTerminalControls() {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBaseControls), nameof(InitializeTerminalControls)) : null) {
                if (AreTerminalControlsInitialized) {
                    return;
                }

                AreTerminalControlsInitialized = true;

                HideControls();
                HideActions();
                CreateAdditionalControls();
            }
        }

        public static bool IsPocketGearBase(IMyTerminalBlock block) {
            return block != null && PocketGearBaseLogic.PocketGearIds.Contains(block.BlockDefinition.SubtypeId);
        }

        private static void CreateAdditionalControls() {
            var controls = new List<IMyTerminalControl>();

            DeployVelocitySlider = TerminalControls.DeployVelocitySlider.Create();
            controls.Add(DeployVelocitySlider);

            LockRetractBehaviorCombobox = TerminalControls.LockRetractBehaviorCombobox.Create();
            controls.Add(LockRetractBehaviorCombobox);

            PlacePocketGearPadButton = TerminalControls.PlacePocketGearPadButton.Create();
            controls.Add(PlacePocketGearPadButton);

            DeployRetractSwitch = TerminalControls.DeployRetractSwitch.Create();
            controls.Add(DeployRetractSwitch);

            TerminalControlUtils.RegisterControls<IMyMotorAdvancedStator>(controls);
        }

        private static void HideActions() {
            List<IMyTerminalAction> defaultActions;
            MyAPIGateway.TerminalControls.GetActions<IMyMotorAdvancedStator>(out defaultActions);

            foreach (var action in defaultActions) {
                if (HiddenActions.Contains(action.Id)) {
                    var original = action.Enabled;
                    action.Enabled = block => !IsPocketGearBase(block) && original.Invoke(block);
                }
            }
        }

        private static void HideControls() {
            List<IMyTerminalControl> controls;
            MyAPIGateway.TerminalControls.GetControls<IMyMotorAdvancedStator>(out controls);

            foreach (var control in controls) {
                if (HiddenControls.Contains(control.Id)) {
                    var visible = control.Visible;
                    var enabled = control.Enabled;
                    control.Visible = block => !IsPocketGearBase(block) && visible.Invoke(block);
                    control.Enabled = block => !IsPocketGearBase(block) && enabled.Invoke(block);

                    if (control.Id == "LowerLimit") {
                        var slider = control as IMyTerminalControlSlider;
                        if (slider != null) {
                            var getter = slider.Getter;
                            var setter = slider.Setter;

                            slider.Getter = block => IsPocketGearBase(block) ? PocketGearBaseLogic.FORCED_LOWER_LIMIT_DEG : getter.Invoke(block);
                            slider.Setter = (block, value) => setter.Invoke(block, IsPocketGearBase(block) ? PocketGearBaseLogic.FORCED_LOWER_LIMIT_DEG : value);
                        }
                    }

                    if (control.Id == "UpperLimit") {
                        var slider = control as IMyTerminalControlSlider;
                        if (slider != null) {
                            var getter = slider.Getter;
                            var setter = slider.Setter;

                            slider.Getter = block => IsPocketGearBase(block) ? PocketGearBaseLogic.FORCED_UPPER_LIMIT_DEG : getter.Invoke(block);
                            slider.Setter = (block, value) => setter.Invoke(block, IsPocketGearBase(block) ? PocketGearBaseLogic.FORCED_UPPER_LIMIT_DEG : value);
                        }
                    }
                }
            }
        }
    }
}