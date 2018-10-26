using System.Collections.Generic;

namespace AutoMcD.PocketGear {
    internal static class Defs {
        internal static class Base {
            internal const string LARGE_NORMAL = "MA_PocketGear_L_Base";
            internal const string LARGE_SMALL = "MA_PocketGear_L_Base_sm";
            internal const string NORMAL = "MA_PocketGear_Base";
            internal const string SMALL = "MA_PocketGear_Base_sm";
            internal static readonly HashSet<string> Ids = new HashSet<string> { NORMAL, LARGE_NORMAL, LARGE_SMALL, SMALL };
        }

        internal static class Pad {
            internal const string LARGE_NORMAL = "MA_PocketGear_L_Pad";
            internal const string LARGE_SMALL = "MA_PocketGear_L_Pad_sm";
            internal const string NORMAL = "MA_PocketGear_Pad";
            internal const string SMALL = "MA_PocketGear_Pad_sm";
            internal static readonly HashSet<string> Ids = new HashSet<string> { NORMAL, LARGE_NORMAL, LARGE_SMALL, SMALL };
        }

        internal static class Part {
            internal const string LARGE_NORMAL = "MA_PocketGear_L_Rotor";
            internal const string LARGE_SMALL = "MA_PocketGear_L_Rotor_sm";
            internal const string NORMAL = "MA_PocketGear_Rotor";
            internal const string SMALL = "MA_PocketGear_Rotor_sm";
            internal static readonly HashSet<string> Ids = new HashSet<string> { NORMAL, LARGE_NORMAL, LARGE_SMALL, SMALL };
        }
    }
}