using System;
using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sisk.Utils.Logging;
using Sisk.Utils.Profiler;
using SpaceEngineers.Game.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace AutoMcD.PocketGear.Logic {
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_MotorAdvancedRotor), false, POCKETGEAR_PART, POCKETGEAR_PART_LARGE, POCKETGEAR_PART_SMALL, POCKETGEAR_PART_LARGE_SMALL)]
    public class PocketGearPart : MyGameLogicComponent {
        public const string POCKETGEAR_PART = "MA_PocketGear_Rotor";
        public const string POCKETGEAR_PART_LARGE = "MA_PocketGear_L_Rotor";
        public const string POCKETGEAR_PART_LARGE_SMALL = "MA_PocketGear_L_Rotor_sm";
        public const string POCKETGEAR_PART_SMALL = "MA_PocketGear_Rotor_sm";
        public static readonly HashSet<string> PocketGearIds = new HashSet<string> { POCKETGEAR_PART, POCKETGEAR_PART_LARGE, POCKETGEAR_PART_LARGE_SMALL, POCKETGEAR_PART_SMALL };
        private IMyMotorRotor _pocketGearPart;

        protected ILogger Log { get; set; }

        public override void Close() {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearPart), nameof(Close)) : null) {
                _pocketGearPart.CubeGrid.OnBlockAdded -= OnBlockAdded;
                _pocketGearPart.CubeGrid.OnBlockRemoved -= OnBlockRemoved;
            }
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearPart), nameof(Init)) : null) {
                Log = Mod.Static.Log.ForScope<PocketGearPart>();

                _pocketGearPart = Entity as IMyMotorRotor;
                NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            }
        }

        public override void UpdateOnceBeforeFrame() {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearPart), nameof(UpdateOnceBeforeFrame)) : null) {
                if (_pocketGearPart.CubeGrid?.Physics == null) {
                    return;
                }

                PlacePocketGearPad();

                _pocketGearPart.CubeGrid.OnBlockAdded += OnBlockAdded;
                _pocketGearPart.CubeGrid.OnBlockRemoved += OnBlockRemoved;
            }
        }

        public void PlacePocketGearPad() {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearPart), nameof(PlacePocketGearPad)) : null) {
                var cubeGrid = _pocketGearPart.CubeGrid;
                var gridSize = cubeGrid.GridSize;
                var left = _pocketGearPart.WorldMatrix.Left;
                var forward = _pocketGearPart.WorldMatrix.Forward;
                var up = _pocketGearPart.WorldMatrix.Up;

                var position = _pocketGearPart.GetPosition();

                Vector3D origin;
                string pocketGearPadId;
                switch (_pocketGearPart.BlockDefinition.SubtypeId) {
                    case POCKETGEAR_PART:
                        pocketGearPadId = PocketGearPad.POCKETGEAR_PAD;
                        origin = position + left * gridSize * 2 + forward * gridSize;
                        break;
                    case POCKETGEAR_PART_LARGE:
                        pocketGearPadId = PocketGearPad.POCKETGEAR_PAD_LARGE;
                        origin = position + left * (gridSize * 5) + forward * gridSize;
                        break;
                    case POCKETGEAR_PART_SMALL:
                        pocketGearPadId = PocketGearPad.POCKETGEAR_PAD_SMALL;
                        origin = position + left + forward * gridSize;
                        break;
                    case POCKETGEAR_PART_LARGE_SMALL:
                        pocketGearPadId = PocketGearPad.POCKETGEAR_PAD_LARGE_SMALL;
                        origin = position + left * (gridSize * 5) + forward * gridSize;
                        break;
                    default:
                        throw new Exception($"Unknown PocketGearPart SubtypeId: {_pocketGearPart.BlockDefinition.SubtypeId}");
                }

                var padPosition = cubeGrid.WorldToGridInteger(origin);
                if (cubeGrid.CubeExists(padPosition)) {
                    return;
                }

                var canPlaceCube = cubeGrid.CanAddCube(padPosition);
                if (canPlaceCube) {
                    try {
                        var buildPercent = MyAPIGateway.Session.CreativeMode ? 1 : 0.00001525902f;
                        var landingGearBuilder = new MyObjectBuilder_LandingGear {
                            SubtypeName = pocketGearPadId,
                            Owner = _pocketGearPart.Base?.OwnerId ?? _pocketGearPart.OwnerId,
                            BuiltBy = _pocketGearPart.Base?.OwnerId ?? _pocketGearPart.OwnerId,
                            AutoLock = false,
                            BuildPercent = buildPercent,
                            IntegrityPercent = buildPercent
                        };

                        var cubeGridBuilder = new MyObjectBuilder_CubeGrid {
                            CreatePhysics = true,
                            GridSizeEnum = _pocketGearPart.CubeGrid.GridSizeEnum,
                            PositionAndOrientation = new MyPositionAndOrientation(origin, forward, up)
                        };

                        cubeGridBuilder.CubeBlocks.Add(landingGearBuilder);

                        var gridsToMerge = new List<MyObjectBuilder_CubeGrid> { cubeGridBuilder };
                        (cubeGrid as MyCubeGrid)?.PasteBlocksToGrid(gridsToMerge, 0, false, false);
                    } catch (Exception exception) {
                        Log.Error(exception);
                    }
                }
            }
        }

        private void OnBlockAdded(IMySlimBlock slimBlock) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearPart), nameof(OnBlockAdded)) : null) {
                if (_pocketGearPart.Base != null && PocketGearPad.PocketGearIds.Contains(slimBlock.BlockDefinition.Id.SubtypeId.String)) {
                    _pocketGearPart.Base.GameLogic?.GetAs<PocketGearBase>()?.OnPocketGearPadAdded((IMyLandingGear) slimBlock.FatBlock);
                }
            }
        }

        private void OnBlockRemoved(IMySlimBlock slimBlock) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(PocketGearPart), nameof(OnBlockRemoved)) : null) {
                if (_pocketGearPart.Base != null && PocketGearPad.PocketGearIds.Contains(slimBlock.BlockDefinition.Id.SubtypeId.String)) {
                    _pocketGearPart.Base.GameLogic?.GetAs<PocketGearBase>()?.OnPocketGearPadRemoved(slimBlock);
                }
            }
        }
    }
}