using System;
using System.Collections.Generic;
using System.Linq;
using AutoMcD.PocketGear.Logic;
using Sandbox.ModAPI;
using Sisk.Utils.Profiler;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace AutoMcD.PocketGear {
    public class ProtectInfo {
        private const int PROTECTION_RADIUS = 2;
        private readonly HashSet<IMySlimBlock> _protectedBlocks = new HashSet<IMySlimBlock>();

        public ProtectInfo(IMyCubeGrid cubeGrid) {
            CubeGrid = cubeGrid;
            CubeGrid.OnBlockAdded += OnBlockAdded;
            CubeGrid.OnBlockRemoved += OnBlockRemoved;
        }

        public AttackerInfo Attacker { get; private set; }

        public DateTime AttackStart { get; private set; }
        public IMyCubeGrid CubeGrid { get; }

        public Vector3D LinearVelocity => CubeGrid.Physics.LinearVelocity;

        public float Mass { get; private set; }

        private static float CalculateMass(IMyCubeGrid cubeGrid) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(ProtectInfo), nameof(CalculateMass)) : null) {
                var mass = cubeGrid.Physics.Mass;
                mass += MyAPIGateway.GridGroups.GetGroup(cubeGrid, GridLinkTypeEnum.Mechanical).Sum(x => x.Physics.Mass);
                return mass;
            }
        }

        private static IEnumerable<IMySlimBlock> GetNearbyBlocks(Vector3D position, IMyCubeGrid cubeGrid) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(ProtectInfo), nameof(GetNearbyBlocks)) : null) {
                var gridSize = cubeGrid.GridSize;
                var center = position;
                var radius = PROTECTION_RADIUS * gridSize;
                var sphere = new BoundingSphereD(center, radius);
                var slimBlocks = cubeGrid.GetBlocksInsideSphere(ref sphere);
                return slimBlocks;
            }
        }

        public bool Contains(IMySlimBlock block) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(ProtectInfo), nameof(Contains)) : null) {
                return _protectedBlocks.Contains(block);
            }
        }

        public void Protect(IMyCubeBlock block) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(ProtectInfo), nameof(Protect)) : null) {
                OnBlockAdded(block.SlimBlock);
            }
        }

        public void RegisterNewAttack(IMyEntity attacker) {
            AttackStart = DateTime.UtcNow;
            Attacker = new AttackerInfo(attacker);
        }

        private void OnBlockAdded(IMySlimBlock block) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(ProtectInfo), nameof(OnBlockAdded)) : null) {
                if (!_protectedBlocks.Contains(block)) {
                    var subTypeId = block.BlockDefinition.Id.SubtypeId.String;
                    if (PocketGearBaseLogic.PocketGearIds.Contains(subTypeId)) {
                        foreach (var slimBlock in GetNearbyBlocks(block.FatBlock.GetPosition(), CubeGrid)) {
                            if (!_protectedBlocks.Contains(slimBlock)) {
                                _protectedBlocks.Add(slimBlock);
                            }
                        }
                    }

                    _protectedBlocks.Add(block);
                }

                Mass = CalculateMass(CubeGrid);
            }
        }

        private void OnBlockRemoved(IMySlimBlock block) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(ProtectInfo), nameof(OnBlockRemoved)) : null) {
                if (_protectedBlocks.Contains(block)) {
                    var subTypeId = block.BlockDefinition.Id.SubtypeId.String;
                    if (PocketGearBaseLogic.PocketGearIds.Contains(subTypeId)) {
                        foreach (var slimBlock in GetNearbyBlocks(block.FatBlock.GetPosition(), CubeGrid)) {
                            if (_protectedBlocks.Contains(slimBlock)) {
                                _protectedBlocks.Remove(slimBlock);
                            }
                        }
                    }

                    _protectedBlocks.Remove(block);
                }

                Mass = CalculateMass(CubeGrid);
            }
        }
    }
}