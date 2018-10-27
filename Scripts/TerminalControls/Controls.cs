using System.Collections.Generic;
using AutoMcD.PocketGear.Logic;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;

// ReSharper disable InlineOutVariableDeclaration

namespace AutoMcD.PocketGear.TerminalControls {
    public class Controls {
        private static readonly HashSet<string> HiddenActions = new HashSet<string> { "Add Small Top Part", "IncreaseLowerLimit", "DecreaseLowerLimit", "ResetLowerLimit", "IncreaseUpperLimit", "DecreaseUpperLimit", "ResetUpperLimit", "IncreaseDisplacement", "DecreaseDisplacement", "ResetDisplacement", "RotorLock", "Reverse", "IncreaseVelocity", "DecreaseVelocity", "ResetVelocity" };
        private static readonly HashSet<string> HiddenControls = new HashSet<string> { "Add Small Top Part", "LowerLimit", "UpperLimit", "Displacement", "RotorLock", "Reverse", "Velocity" };
        public IMyTerminalControlOnOffSwitch DeployRetract => DeployRetractSwitch.Control;
        public IMyTerminalControlSlider DeployVelocity => DeployVelocitySlider.Control;
        public IMyTerminalControlCombobox LockRetractBehavior => LockRetractBehaviorCombobox.Control;
        public IMyTerminalControlButton PlacePocketGearPad => PlacePocketGearPadButton.Control;

        private static void CreateActions() {
            var actions = new List<IMyTerminalAction>();
            actions.AddRange(DeployRetractSwitch.Actions);
            actions.AddRange(DeployVelocitySlider.Actions);
            actions.AddRange(PlacePocketGearPadButton.Actions);

            foreach (var action in actions) {
                MyAPIGateway.TerminalControls.AddAction<IMyMotorAdvancedStator>(action);
            }
        }

        private static void CreateControls() {
            MyAPIGateway.TerminalControls.AddControl<IMyMotorAdvancedStator>(DeployRetractSwitch.Control);
            MyAPIGateway.TerminalControls.AddControl<IMyMotorAdvancedStator>(DeployVelocitySlider.Control);
            MyAPIGateway.TerminalControls.AddControl<IMyMotorAdvancedStator>(LockRetractBehaviorCombobox.Control);
            MyAPIGateway.TerminalControls.AddControl<IMyMotorAdvancedStator>(PlacePocketGearPadButton.Control);
        }

        private static bool IsPocketGearBase(IMyTerminalBlock block) {
            return block != null && Defs.Base.Ids.Contains(block.BlockDefinition.SubtypeId);
        }

        private static void ModifyVanillaActions() {
            List<IMyTerminalAction> actions;
            MyAPIGateway.TerminalControls.GetActions<IMyMotorAdvancedStator>(out actions);

            foreach (var action in actions) {
                if (HiddenActions.Contains(action.Id)) {
                    var original = action.Enabled;
                    action.Enabled = block => !IsPocketGearBase(block) && original.Invoke(block);
                }
            }
        }

        private static void ModifyVanillaControls() {
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

                            slider.Getter = block => IsPocketGearBase(block) ? PocketGearBase.FORCED_LOWER_LIMIT_DEG : getter.Invoke(block);
                            slider.Setter = (block, value) => setter.Invoke(block, IsPocketGearBase(block) ? PocketGearBase.FORCED_LOWER_LIMIT_DEG : value);
                        }
                    } else if (control.Id == "UpperLimit") {
                        var slider = control as IMyTerminalControlSlider;
                        if (slider != null) {
                            var getter = slider.Getter;
                            var setter = slider.Setter;

                            slider.Getter = block => IsPocketGearBase(block) ? PocketGearBase.FORCED_UPPER_LIMIT_DEG : getter.Invoke(block);
                            slider.Setter = (block, value) => setter.Invoke(block, IsPocketGearBase(block) ? PocketGearBase.FORCED_UPPER_LIMIT_DEG : value);
                        }
                    }
                }
            }
        }

        private static void RegisterProperties() {
            MyAPIGateway.TerminalControls.AddControl<IMyMotorAdvancedStator>(DeployRetractSwitch.Property);
            MyAPIGateway.TerminalControls.AddControl<IMyMotorAdvancedStator>(DeployVelocitySlider.Property);
        }

        public void Close() { }

        public void InitializePocketGearControls() {
            ModifyVanillaControls();
            ModifyVanillaActions();
            CreateActions();
            CreateControls();
            RegisterProperties();
        }
    }
}