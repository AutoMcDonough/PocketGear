using System.Collections.Generic;
using AutoMcD.PocketGear.Logic;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;

// ReSharper disable InlineOutVariableDeclaration

namespace AutoMcD.PocketGear.TerminalControls {
    public class Base : IControls {
        private static readonly HashSet<string> HiddenActions = new HashSet<string> { "Add Small Top Part", "IncreaseLowerLimit", "DecreaseLowerLimit", "ResetLowerLimit", "IncreaseUpperLimit", "DecreaseUpperLimit", "ResetUpperLimit", "IncreaseDisplacement", "DecreaseDisplacement", "ResetDisplacement", "RotorLock", "Reverse", "IncreaseVelocity", "DecreaseVelocity", "ResetVelocity" };
        private static readonly HashSet<string> HiddenControls = new HashSet<string> { "Add Small Top Part", "LowerLimit", "UpperLimit", "Displacement", "RotorLock", "Reverse", "Velocity" };
        private readonly List<IMyTerminalAction> _additionalActions = new List<IMyTerminalAction>();
        private readonly List<IMyTerminalControl> _additionalControls = new List<IMyTerminalControl>();

        public Base() {
            ModifyVanillaControls();
            CreateActions();
            CreateControls();
            RegisterProperties();
        }

        public IMyTerminalControlOnOffSwitch DeployRetract => DeployRetractSwitch.Control;
        public IMyTerminalControlSlider DeployVelocity => DeployVelocitySlider.Control;
        public IMyTerminalControlCombobox LockRetractBehavior => LockRetractBehaviorCombobox.Control;
        public IMyTerminalControlButton PlacePocketGearPad => PlacePocketGearPadButton.Control;

        public void OnCustomActionGetter(IMyTerminalBlock block, List<IMyTerminalAction> actions) {
            actions.RemoveAll(x => HiddenActions.Contains(x.Id));
            actions.AddRange(_additionalActions);
        }

        public void OnCustomControlGetter(IMyTerminalBlock block, List<IMyTerminalControl> controls) {
            controls.RemoveAll(x => HiddenControls.Contains(x.Id));
            controls.AddRange(_additionalControls);
        }

        private static bool IsPocketGearBase(IMyTerminalBlock block) {
            return block != null && Defs.Base.Ids.Contains(block.BlockDefinition.SubtypeId);
        }

        private void CreateActions() {
            _additionalActions.AddRange(DeployRetractSwitch.Actions);
            _additionalActions.AddRange(DeployVelocitySlider.Actions);
            _additionalActions.AddRange(PlacePocketGearPadButton.Actions);
        }

        private void CreateControls() {
            _additionalControls.Add(DeployRetractSwitch.Control);
            _additionalControls.Add(DeployVelocitySlider.Control);
            _additionalControls.Add(LockRetractBehaviorCombobox.Control);
            _additionalControls.Add(PlacePocketGearPadButton.Control);
        }

        private void ModifyVanillaControls() {
            List<IMyTerminalControl> controls;
            MyAPIGateway.TerminalControls.GetControls<IMyMotorAdvancedStator>(out controls);

            foreach (var control in controls) {
                switch (control.Id) {
                    case "LowerLimit": {
                        var slider = control as IMyTerminalControlSlider;
                        if (slider != null) {
                            var getter = slider.Getter;
                            var setter = slider.Setter;

                            slider.Getter = block => IsPocketGearBase(block) ? PocketGearBase.FORCED_LOWER_LIMIT_DEG : getter.Invoke(block);
                            slider.Setter = (block, value) => setter.Invoke(block, IsPocketGearBase(block) ? PocketGearBase.FORCED_LOWER_LIMIT_DEG : value);
                        }

                        break;
                    }
                    case "UpperLimit": {
                        var slider = control as IMyTerminalControlSlider;
                        if (slider != null) {
                            var getter = slider.Getter;
                            var setter = slider.Setter;

                            slider.Getter = block => IsPocketGearBase(block) ? PocketGearBase.FORCED_UPPER_LIMIT_DEG : getter.Invoke(block);
                            slider.Setter = (block, value) => setter.Invoke(block, IsPocketGearBase(block) ? PocketGearBase.FORCED_UPPER_LIMIT_DEG : value);
                        }

                        break;
                    }
                }
            }
        }

        private void RegisterProperties() {
            MyAPIGateway.TerminalControls.AddControl<IMyMotorAdvancedStator>(DeployRetractSwitch.Property);
            MyAPIGateway.TerminalControls.AddControl<IMyMotorAdvancedStator>(DeployVelocitySlider.Property);
        }
    }
}