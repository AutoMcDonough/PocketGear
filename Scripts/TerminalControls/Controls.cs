using System.Collections.Generic;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Sisk.PocketGear.Logic;

namespace Sisk.PocketGear.TerminalControls {
    /// <summary>
    ///     Class to modify terminal controls, actions and properties for blocks from this mod.
    /// </summary>
    public class Controls {
        private readonly HashSet<string> _hiddenActions = new HashSet<string> { "Add Small Top Part", "IncreaseLowerLimit", "DecreaseLowerLimit", "ResetLowerLimit", "IncreaseUpperLimit", "DecreaseUpperLimit", "ResetUpperLimit", "IncreaseDisplacement", "DecreaseDisplacement", "ResetDisplacement", "RotorLock", "Reverse", "IncreaseVelocity", "DecreaseVelocity", "ResetVelocity" };
        private readonly HashSet<string> _hiddenControls = new HashSet<string> { "Add Small Top Part", "LowerLimit", "UpperLimit", "Displacement", "RotorLock", "Reverse", "Velocity" };

        /// <summary>
        ///     Indicates if terminal controls are initialized.
        /// </summary>
        public bool AreTerminalControlsInitialized { get; private set; }

        public IMyTerminalControlOnOffSwitch DeployRetract => DeployRetractSwitch.Control;
        public IMyTerminalControlSlider DeployVelocity => DeployVelocitySlider.Control;
        public IMyTerminalControlCombobox LockRetractBehavior => LockRetractBehaviorCombobox.Control;
        public IMyTerminalControlButton PlacePocketGearPad => PlacePocketGearPadButton.Control;

        /// <summary>
        ///     Check if the given block is one of the pocket gear base blocks.
        /// </summary>
        /// <param name="block">The block that will be check if it's one of the pocket gear base blocks.</param>
        /// <returns>Return true if given block is one of the pocket gear bases.</returns>
        public static bool IsPocketGearBase(IMyTerminalBlock block) {
            return block != null && Defs.Base.Ids.Contains(block.BlockDefinition.SubtypeId);
        }

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

        private static void RegisterProperties() {
            MyAPIGateway.TerminalControls.AddControl<IMyMotorAdvancedStator>(DeployRetractSwitch.Property);
            MyAPIGateway.TerminalControls.AddControl<IMyMotorAdvancedStator>(DeployVelocitySlider.Property);
        }

        /// <summary>
        ///     Initialize the controls.
        /// </summary>
        public void InitializeControls() {
            ModifyVanillaControls();
            ModifyVanillaActions();
            CreateActions();
            CreateControls();
            RegisterProperties();
            AreTerminalControlsInitialized = true;
        }

        /// <summary>
        ///     Modify vanilla terminal actions to hide some actions for solar stator blocks.
        /// </summary>
        private void ModifyVanillaActions() {
            List<IMyTerminalAction> actions;
            MyAPIGateway.TerminalControls.GetActions<IMyMotorAdvancedStator>(out actions);

            foreach (var action in actions) {
                if (_hiddenActions.Contains(action.Id)) {
                    var original = action.Enabled;
                    action.Enabled = block => !IsPocketGearBase(block) && original.Invoke(block);
                }
            }
        }

        /// <summary>
        ///     Modify the vanilla controls to hide some controls for solar stator blocks.
        /// </summary>
        private void ModifyVanillaControls() {
            List<IMyTerminalControl> controls;
            MyAPIGateway.TerminalControls.GetControls<IMyMotorAdvancedStator>(out controls);

            foreach (var control in controls) {
                if (_hiddenControls.Contains(control.Id)) {
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
    }
}