using Sandbox.Common.ObjectBuilders;
using Sisk.Utils.Logging;
using Sisk.Utils.Profiler;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using IMyLandingGear = SpaceEngineers.Game.ModAPI.IMyLandingGear;

namespace AutoMcD.PocketGear.Logic {
    // note: this LogicComponent can probaböy removed. At the moment there is a bug which prevents me from registering an event handler to IMyLandingGear.LockModeChange.
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_LandingGear), false, POCKETGEAR_PAD, POCKETGEAR_PAD_LARGE, POCKETGEAR_PAD_LARGE_SMALL, POCKETGEAR_PAD_SMALL)]
    public class PocketGearPadLogic : MyGameLogicComponent {
        public const string POCKETGEAR_PAD = "MA_PocketGear_Pad";
        public const string POCKETGEAR_PAD_LARGE = "MA_PocketGear_L_Pad";
        public const string POCKETGEAR_PAD_LARGE_SMALL = "MA_PocketGear_L_Pad_sm";
        public const string POCKETGEAR_PAD_SMALL = "MA_PocketGear_Pad_sm";
        private IMyLandingGear _pocketGearPad;
        protected ILogger Log { get; set; }

        public override void Close() {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearPadLogic), nameof(Close)) : null) {
                // bug: throws: Cannot bind to the target method because its signature or security transparency is not compatible with that of the delegate type.
                //_pocketGearPad.LockModeChanged -= OnLockModeChanged;
            }
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearPadLogic), nameof(Init)) : null) {
                Log = Mod.Static.Log.ForScope<PocketGearPadLogic>();
                _pocketGearPad = Entity as IMyLandingGear;

                NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            }
        }

        public override void UpdateOnceBeforeFrame() {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearPadLogic), nameof(UpdateOnceBeforeFrame)) : null) {
                // bug: throws: Cannot bind to the target method because its signature or security transparency is not compatible with that of the delegate type.
                //_pocketGearPad.LockModeChanged += OnLockModeChanged;
            }
        }

        private void OnLockModeChanged(IMyLandingGear gear, LandingGearMode mode) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearPadLogic), nameof(Close)) : null) {
                Log.Debug($"Landing gear mode: {mode}");
                if (mode == LandingGearMode.Locked) {
                    // note: maybe we can use it to stop or reduce clang if the bug gets fixed.
                    //_pocketGearPad.CubeGrid.Physics.ClearSpeed();
                }
            }
        }
    }
}