using System.Collections.Generic;
using AutoMcD.PocketGear.Logic;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Sisk.Utils.Profiler;

namespace AutoMcD.PocketGear.TerminalControls {
    public class Controls {
        public Controls() {
            Base = new Base();
            Pad = new Pad();

            MyAPIGateway.TerminalControls.CustomControlGetter += OnCustomControlGetter;
            MyAPIGateway.TerminalControls.CustomActionGetter += OnCustomActionGetter;
        }

        public Base Base { get; }
        public Pad Pad { get; }

        public void Close() {
            MyAPIGateway.TerminalControls.CustomControlGetter -= OnCustomControlGetter;
            MyAPIGateway.TerminalControls.CustomActionGetter -= OnCustomActionGetter;
        }

        private void OnCustomActionGetter(IMyTerminalBlock block, List<IMyTerminalAction> actions) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(Controls), nameof(OnCustomActionGetter)) : null) {
                if (block.BlockDefinition.TypeId == typeof(MyObjectBuilder_MotorAdvancedStator) && PocketGearBase.PocketGearIds.Contains(block.BlockDefinition.SubtypeId)) {
                    Base.OnCustomActionGetter(block, actions);
                }

                if (block.BlockDefinition.TypeId == typeof(MyObjectBuilder_LandingGear) && PocketGearPad.PocketGearIds.Contains(block.BlockDefinition.SubtypeId)) {
                    Pad.OnCustomActionGetter(block, actions);
                }
            }
        }

        private void OnCustomControlGetter(IMyTerminalBlock block, List<IMyTerminalControl> controls) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(Controls), nameof(OnCustomControlGetter)) : null) {
                if (block.BlockDefinition.TypeId == typeof(MyObjectBuilder_MotorAdvancedStator) && PocketGearBase.PocketGearIds.Contains(block.BlockDefinition.SubtypeId)) {
                    Base.OnCustomControlGetter(block, controls);
                }

                if (block.BlockDefinition.TypeId == typeof(MyObjectBuilder_LandingGear) && PocketGearPad.PocketGearIds.Contains(block.BlockDefinition.SubtypeId)) {
                    Pad.OnCustomControlGetter(block, controls);
                }
            }
        }
    }
}