using System.Collections.Generic;
using AutoMcD.PocketGear.Logic;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Sisk.Utils.Profiler;
using SpaceEngineers.Game.ModAPI;

// ReSharper disable UsePatternMatching
// ReSharper disable InlineOutVariableDeclaration

namespace AutoMcD.PocketGear.TerminalControls {
    public static class PocketGearPadControls {
        private static readonly HashSet<string> HiddenActions = new HashSet<string> { "Autolock" };
        private static readonly HashSet<string> HiddenControl = new HashSet<string> { "Autolock" };
        public static bool AreTerminalControlsInitialized { get; private set; }

        public static void InitializeTerminalControls() {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearPadControls), nameof(InitializeTerminalControls)) : null) {
                if (AreTerminalControlsInitialized) {
                    return;
                }

                AreTerminalControlsInitialized = true;

                HideControls();
                HideActions();
            }
        }

        public static bool IsPocketGearPad(IMyTerminalBlock block) {
            return block != null && PocketGearPadLogic.PocketGearIds.Contains(block.BlockDefinition.SubtypeId);
        }

        private static void HideActions() {
            List<IMyTerminalAction> actions;
            MyAPIGateway.TerminalControls.GetActions<IMyLandingGear>(out actions);

            foreach (var action in actions) {
                if (HiddenActions.Contains(action.Id)) {
                    var original = action.Enabled;
                    action.Enabled = block => !IsPocketGearPad(block) && original.Invoke(block);
                }

                if (action.Id == "Lock") {
                    var orginal = action.Action;
                    action.Action = block => {
                        if (IsPocketGearPad(block)) {
                            PocketGearPadLogic.Lock(block as IMyLandingGear);
                        } else {
                            orginal.Invoke(block);
                        }
                    };
                }

                if (action.Id == "Unlock") {
                    var orginal = action.Action;
                    action.Action = block => {
                        if (IsPocketGearPad(block)) {
                            PocketGearPadLogic.Unlock(block as IMyLandingGear);
                        } else {
                            orginal.Invoke(block);
                        }
                    };
                }

                if (action.Id == "SwitchLock") {
                    var orginal = action.Action;
                    action.Action = block => {
                        if (IsPocketGearPad(block)) {
                            PocketGearPadLogic.SwitchLock(block as IMyLandingGear);
                        } else {
                            orginal.Invoke(block);
                        }
                    };
                }
            }
        }

        private static void HideControls() {
            List<IMyTerminalControl> controls;
            MyAPIGateway.TerminalControls.GetControls<IMyLandingGear>(out controls);
            foreach (var control in controls) {
                if (HiddenControl.Contains(control.Id)) {
                    var original = control.Visible;
                    control.Visible = block => !IsPocketGearPad(block) && original.Invoke(block);
                }

                if (control.Id == "Lock") {
                    var checkbox = control as IMyTerminalControlCheckbox;
                    if (checkbox != null) {
                        var setter = checkbox.Setter;
                        checkbox.Setter = (block, value) => {
                            if (IsPocketGearPad(block)) {
                                PocketGearPadLogic.Lock(block as IMyLandingGear);
                            } else {
                                setter.Invoke(block, value);
                            }
                        };
                    }
                }

                if (control.Id == "Unlock") {
                    var checkbox = control as IMyTerminalControlCheckbox;
                    if (checkbox != null) {
                        var setter = checkbox.Setter;
                        checkbox.Setter = (block, value) => {
                            if (IsPocketGearPad(block)) {
                                PocketGearPadLogic.Unlock(block as IMyLandingGear);
                            } else {
                                setter.Invoke(block, value);
                            }
                        };
                    }
                }
            }
        }
    }
}