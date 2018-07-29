using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Sisk.Utils.Logging;
using Sisk.Utils.Profiler;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using IMyLandingGear = SpaceEngineers.Game.ModAPI.IMyLandingGear;

// ReSharper disable InlineOutVariableDeclaration
// ReSharper disable MergeCastWithTypeCheck

namespace AutoMcD.PocketGear.Logic {
    // bug: IMyLandingGear.LockModeChange throws "Cannot bind to the target method because its signature or security transparency is not compatible with that of the delegate type.". Once this is solved i should be able to create autolock.
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_LandingGear), false, POCKETGEAR_PAD, POCKETGEAR_PAD_LARGE, POCKETGEAR_PAD_LARGE_SMALL, POCKETGEAR_PAD_SMALL)]
    public class PocketGearPadLogic : MyGameLogicComponent {
        public const string POCKETGEAR_PAD = "MA_PocketGear_Pad";
        public const string POCKETGEAR_PAD_LARGE = "MA_PocketGear_L_Pad";
        public const string POCKETGEAR_PAD_LARGE_SMALL = "MA_PocketGear_L_Pad_sm";
        public const string POCKETGEAR_PAD_SMALL = "MA_PocketGear_Pad_sm";
        public static readonly HashSet<string> PocketGearIds = new HashSet<string> { POCKETGEAR_PAD, POCKETGEAR_PAD_LARGE, POCKETGEAR_PAD_LARGE_SMALL, POCKETGEAR_PAD_SMALL };
        private static readonly HashSet<string> HiddenActions = new HashSet<string> { "Autolock" };
        private static readonly HashSet<string> HiddenControl = new HashSet<string> { "Autolock" };

        private IMyLandingGear _pocketGearPad;
        private static bool AreTerminalControlsInitialized { get; set; }
        protected ILogger Log { get; set; }

        public static void Lock(IMyLandingGear landingGear) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearPadLogic), nameof(Lock)) : null) {
                if (landingGear.LockMode == LandingGearMode.ReadyToLock) {
                    var pocketGearBase = GetPocketGearBase(landingGear);
                    var logic = pocketGearBase.GameLogic.GetAs<PocketGearBaseLogic>();
                    logic.ManualRotorLock();
                    landingGear.Lock();
                }
            }
        }

        public static void SwitchLock(IMyLandingGear landingGear) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearPadLogic), nameof(SwitchLock)) : null) {
                if (landingGear.IsLocked) {
                    Unlock(landingGear);
                } else if (landingGear.LockMode == LandingGearMode.ReadyToLock) {
                    Lock(landingGear);
                }
            }
        }

        public static void Unlock(IMyLandingGear landingGear) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearPadLogic), nameof(Unlock)) : null) {
                if (landingGear.LockMode == LandingGearMode.Locked) {
                    var pocketGearBase = GetPocketGearBase(landingGear);
                    var logic = pocketGearBase.GameLogic.GetAs<PocketGearBaseLogic>();
                    logic.ManualRotorLock();
                    landingGear.Unlock();
                }
            }
        }

        private static IMyMotorStator GetPocketGearBase(IMyLandingGear landingGear) {
            var cubeGrid = landingGear.CubeGrid;
            var gridSize = cubeGrid.GridSize;
            var position = landingGear.GetPosition();
            var backward = landingGear.WorldMatrix.Backward;
            var origin = position + backward * gridSize;
            var rotorPosition = cubeGrid.WorldToGridInteger(origin);
            var slimBlock = cubeGrid.GetCubeBlock(rotorPosition);
            var rotor = slimBlock?.FatBlock as IMyMotorRotor;
            var stator = rotor?.Base as IMyMotorStator;
            return stator;
        }

        private static void InitializeTerminalControls() {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearPadLogic), nameof(InitializeTerminalControls)) : null) {
                if (AreTerminalControlsInitialized) {
                    return;
                }

                AreTerminalControlsInitialized = true;

                List<IMyTerminalControl> defaultControls;
                List<IMyTerminalAction> defaultActions;
                MyAPIGateway.TerminalControls.GetControls<IMyLandingGear>(out defaultControls);
                MyAPIGateway.TerminalControls.GetActions<IMyLandingGear>(out defaultActions);

                foreach (var control in defaultControls) {
                    if (HiddenControl.Contains(control.Id)) {
                        var original = control.Visible;
                        control.Visible = block => !PocketGearIds.Contains(block.BlockDefinition.SubtypeId) && original.Invoke(block);
                    }

                    if (control.Id == "Lock" && control is IMyTerminalControlCheckbox) {
                        var checkbox = control as IMyTerminalControlCheckbox;
                        var setter = checkbox.Setter;
                        checkbox.Setter = (block, value) => {
                            if (PocketGearIds.Contains(block.BlockDefinition.SubtypeId)) {
                                Lock(block as IMyLandingGear);
                            } else {
                                setter.Invoke(block, value);
                            }
                        };
                    }

                    if (control.Id == "Unlock" && control is IMyTerminalControlCheckbox) {
                        var checkbox = control as IMyTerminalControlCheckbox;
                        var setter = checkbox.Setter;
                        checkbox.Setter = (block, value) => {
                            if (PocketGearIds.Contains(block.BlockDefinition.SubtypeId)) {
                                Unlock(block as IMyLandingGear);
                            } else {
                                setter.Invoke(block, value);
                            }
                        };
                    }
                }

                foreach (var action in defaultActions) {
                    if (HiddenActions.Contains(action.Id)) {
                        var original = action.Enabled;
                        action.Enabled = block => !PocketGearIds.Contains(block.BlockDefinition.SubtypeId) && original.Invoke(block);
                    }

                    if (action.Id == "Lock") {
                        var orginal = action.Action;
                        action.Action = block => {
                            if (PocketGearIds.Contains(block.BlockDefinition.SubtypeId)) {
                                Lock(block as IMyLandingGear);
                            } else {
                                orginal.Invoke(block);
                            }
                        };
                    }

                    if (action.Id == "Unlock") {
                        var orginal = action.Action;
                        action.Action = block => {
                            if (PocketGearIds.Contains(block.BlockDefinition.SubtypeId)) {
                                Unlock(block as IMyLandingGear);
                            } else {
                                orginal.Invoke(block);
                            }
                        };
                    }

                    if (action.Id == "SwitchLock") {
                        var orginal = action.Action;
                        action.Action = block => {
                            if (PocketGearIds.Contains(block.BlockDefinition.SubtypeId)) {
                                SwitchLock(block as IMyLandingGear);
                            } else {
                                orginal.Invoke(block);
                            }
                        };
                    }
                }
            }
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearPadLogic), nameof(Init)) : null) {
                Log = Mod.Static.Log.ForScope<PocketGearPadLogic>();
                _pocketGearPad = Entity as IMyLandingGear;
                if (_pocketGearPad != null) {
                    _pocketGearPad.AutoLock = false;
                }

                if (!AreTerminalControlsInitialized) {
                    InitializeTerminalControls();
                }
            }
        }
    }
}