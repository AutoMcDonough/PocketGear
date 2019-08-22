using System.Collections.Generic;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;

namespace Sisk.PocketGear.TerminalControls {
    public interface IControls {
        void OnCustomActionGetter(IMyTerminalBlock block, List<IMyTerminalAction> actions);
        void OnCustomControlGetter(IMyTerminalBlock block, List<IMyTerminalControl> controls);
    }
}