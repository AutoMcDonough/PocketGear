using System.Collections.Generic;

namespace Sisk.PocketGear {
    public class Defs {
        public IReadOnlyDictionary<string, string> BaseToPad { get; } = new Dictionary<string, string> {
            { Base.LARGE_NORMAL, Pad.LARGE_NORMAL },
            { Base.LARGE_SMALL, Pad.LARGE_SMALL },
            { Base.NORMAL, Pad.NORMAL },
            { Base.SMALL, Pad.SMALL }
        };

        public static class Base {
            internal const string LARGE_NORMAL = "MA_PocketGear_L_Base";
            internal const string LARGE_SMALL = "MA_PocketGear_L_Base_sm";
            internal const string NORMAL = "MA_PocketGear_Base";
            internal const string SMALL = "MA_PocketGear_Base_sm";
            internal static readonly HashSet<string> Ids = new HashSet<string> { NORMAL, LARGE_NORMAL, LARGE_SMALL, SMALL };
        }

        public static class Pad {
            internal const string LARGE_NORMAL = "MA_PocketGear_L_Pad";
            internal const string LARGE_SMALL = "MA_PocketGear_L_Pad_sm";
            internal const string NORMAL = "MA_PocketGear_Pad";
            internal const string SMALL = "MA_PocketGear_Pad_sm";
            internal static readonly HashSet<string> Ids = new HashSet<string> { NORMAL, LARGE_NORMAL, LARGE_SMALL, SMALL };
        }

        public static class Part {
            internal const string LARGE_NORMAL = "MA_PocketGear_L_Rotor";
            internal const string LARGE_SMALL = "MA_PocketGear_L_Rotor_sm";
            internal const string NORMAL = "MA_PocketGear_Rotor";
            internal const string SMALL = "MA_PocketGear_Rotor_sm";
            internal static readonly HashSet<string> Ids = new HashSet<string> { NORMAL, LARGE_NORMAL, LARGE_SMALL, SMALL };
        }
    }
}