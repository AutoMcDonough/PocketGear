using System;
using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Sisk.Utils.Logging;
using Sisk.Utils.Profiler;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace AutoMcD.PocketGear.Logic {
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_MotorAdvancedStator), false, POCKETGEAR_BASE, POCKETGEAR_BASE_LARGE, POCKETGEAR_BASE_LARGE_SMALL, POCKETGEAR_BASE_SMALL)]
    public class PocketGearBaseLogic : MyGameLogicComponent {
        public const string POCKETGEAR_BASE = "MA_PocketGear_Base";
        public const string POCKETGEAR_BASE_LARGE = "MA_PocketGear_L_Base";
        public const string POCKETGEAR_BASE_LARGE_SMALL = "MA_PocketGear_L_Base_sm";
        public const string POCKETGEAR_BASE_SMALL = "MA_PocketGear_Base_sm";

        private const float DEFAULT_VELOCITY_RPM = -1f;
        private const float FORCED_LOWER_LIMIT_DEG = 333.5f;
        private const float FORCED_UPPER_LIMIT_DEG = 360.0f;
        public static readonly HashSet<string> HiddenActions = new HashSet<string> {"Add Small Top Part", "IncreaseLowerLimit", "DecreaseLowerLimit", "ResetLowerLimit", "IncreaseUpperLimit", "DecreaseUpperLimit", "ResetUpperLimit", "IncreaseDisplacement", "DecreaseDisplacement", "ResetDisplacement"};
        public static readonly HashSet<string> HiddenControl = new HashSet<string> {"Add Small Top Part", "LowerLimit", "UpperLimit", "Displacement"};
        private static readonly HashSet<string> PocketGearIds = new HashSet<string> {POCKETGEAR_BASE, POCKETGEAR_BASE_LARGE, POCKETGEAR_BASE_LARGE_SMALL, POCKETGEAR_BASE_SMALL};

        private bool _isJustPlaced;
        private IMyMotorStator _pocketGearBase;
        private static bool AreTerminalControlsInitialized { get; set; }

        protected ILogger Log { get; set; }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBaseLogic), nameof(Init)) : null) {
                Log = Mod.Static.Log.ForScope<PocketGearBaseLogic>();

                _pocketGearBase = Entity as IMyMotorStator;
                _isJustPlaced = _pocketGearBase?.CubeGrid?.Physics != null;
                NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            }
        }

        public override void UpdateOnceBeforeFrame() {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBaseLogic), nameof(UpdateOnceBeforeFrame)) : null) {
                try {
                    if (_pocketGearBase?.CubeGrid?.Physics == null) {
                        return;
                    }

                    if (_isJustPlaced && MyAPIGateway.Multiplayer.IsServer) {
                        _pocketGearBase.TargetVelocityRPM = DEFAULT_VELOCITY_RPM;
                    }

                    _pocketGearBase.LowerLimitDeg = FORCED_LOWER_LIMIT_DEG;
                    _pocketGearBase.UpperLimitDeg = FORCED_UPPER_LIMIT_DEG;

                    if (!AreTerminalControlsInitialized) {
                        InitializeTerminalControls();
                    }
                } catch (Exception exception) {
                    Log.Error(exception);
                }
            }
        }

        private void InitializeTerminalControls() {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBaseLogic), nameof(InitializeTerminalControls)) : null) {
                if (AreTerminalControlsInitialized) {
                    return;
                }

                AreTerminalControlsInitialized = true;

                List<IMyTerminalControl> controls;
                List<IMyTerminalAction> actions;
                MyAPIGateway.TerminalControls.GetControls<IMyMotorAdvancedStator>(out controls);
                MyAPIGateway.TerminalControls.GetActions<IMyMotorAdvancedStator>(out actions);

                foreach (var control in controls) {
                    if (HiddenControl.Contains(control.Id)) {
                        var original = control.Visible;
                        control.Visible = block => {
                            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBaseLogic), "IsControlVisible") : null) {
                                return !PocketGearIds.Contains(block.BlockDefinition.SubtypeId) && original.Invoke(block);
                            }
                        };

                        if (control.Id == "LowerLimit" && control is IMyTerminalControlSlider) {
                            var slider = control as IMyTerminalControlSlider;
                            var getter = slider.Getter;
                            var setter = slider.Setter;
                            slider.Getter = block => {
                                using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBaseLogic), "GetLowerLimit") : null) {
                                    return PocketGearIds.Contains(block.BlockDefinition.SubtypeId) ? FORCED_LOWER_LIMIT_DEG : getter.Invoke(block);
                                }
                            };
                            slider.Setter = (block, value) => {
                                using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBaseLogic), "SetLowerLimit") : null) {
                                    setter.Invoke(block, PocketGearIds.Contains(block.BlockDefinition.SubtypeId) ? FORCED_LOWER_LIMIT_DEG : value);
                                }
                            };
                        }

                        if (control.Id == "UpperLimit" && control is IMyTerminalControlSlider) {
                            var slider = control as IMyTerminalControlSlider;
                            var getter = slider.Getter;
                            var setter = slider.Setter;
                            slider.Getter = block => {
                                using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBaseLogic), "GetUpperLimit") : null) {
                                    return PocketGearIds.Contains(block.BlockDefinition.SubtypeId) ? FORCED_UPPER_LIMIT_DEG : getter.Invoke(block);
                                }
                            };
                            slider.Setter = (block, value) => {
                                using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBaseLogic), "SetUpperLimit") : null) {
                                    setter.Invoke(block, PocketGearIds.Contains(block.BlockDefinition.SubtypeId) ? FORCED_UPPER_LIMIT_DEG : value);
                                }
                            };
                        }
                    }
                }

                foreach (var action in actions) {
                    if (HiddenActions.Contains(action.Id)) {
                        var original = action.Enabled;
                        action.Enabled = block => {
                            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearBaseLogic), "IsActionEnabled") : null) {
                                return !PocketGearIds.Contains(block.BlockDefinition.SubtypeId) && original.Invoke(block);
                            }
                        };
                    }
                }
            }
        }
    }
}