using System.Collections.Generic;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;

namespace AutoMcD.PocketGear.TerminalControls {
    public class Pad : IControls {
        private static readonly HashSet<string> HiddenActions = new HashSet<string> { "Autolock" };
        private static readonly HashSet<string> HiddenControls = new HashSet<string> { "Autolock" };

        public void OnCustomActionGetter(IMyTerminalBlock block, List<IMyTerminalAction> actions) {
            actions.RemoveAll(x => HiddenActions.Contains(x.Id));
        }

        public void OnCustomControlGetter(IMyTerminalBlock block, List<IMyTerminalControl> controls) {
            controls.RemoveAll(x => HiddenControls.Contains(x.Id));
        }
    }
}