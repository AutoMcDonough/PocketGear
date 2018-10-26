using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI;
using Sisk.Utils.Profiler;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace AutoMcD.PocketGear.DamageSystem {
    public class ProtectInfo {
        private readonly HashSet<IMySlimBlock> _protectedBlocks = new HashSet<IMySlimBlock>();

        public ProtectInfo(IMyCubeGrid cubeGrid) {
            CubeGrid = cubeGrid;
            CubeGrid.OnBlockAdded += OnBlockAdded;
            CubeGrid.OnBlockRemoved += OnBlockRemoved;
            CubeGrid.OnPhysicsChanged += OnPhysicsChanged;
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

        public void Close() {
            using (Mod.PROFILE ? Profiler.Measure(nameof(ProtectInfo), nameof(Close)) : null) {
                CubeGrid.OnBlockAdded -= OnBlockAdded;
                CubeGrid.OnBlockRemoved -= OnBlockRemoved;
                CubeGrid.OnPhysicsChanged -= OnPhysicsChanged;
            }
        }

        public bool Contains(IMySlimBlock block) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(ProtectInfo), nameof(Contains)) : null) {
                return _protectedBlocks.Contains(block);
            }
        }

        public void DisableProtection(IMySlimBlock slimBlock) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(ProtectInfo), nameof(DisableProtection)) : null) {
                OnBlockRemoved(slimBlock);
            }
        }

        public void EnableProtection(IMySlimBlock slimBlock) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(ProtectInfo), nameof(EnableProtection)) : null) {
                OnBlockAdded(slimBlock);
            }
        }

        public void RegisterNewAttack(IMyEntity attacker) {
            AttackStart = DateTime.UtcNow;
            Attacker = new AttackerInfo(attacker);
        }

        private void OnBlockAdded(IMySlimBlock block) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(ProtectInfo), nameof(OnBlockAdded)) : null) {
                if (!_protectedBlocks.Contains(block)) {
                    _protectedBlocks.Add(block);
                }

                if (block.CubeGrid?.Physics != null) {
                    Mass = CalculateMass(CubeGrid);
                }
            }
        }

        private void OnBlockRemoved(IMySlimBlock block) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(ProtectInfo), nameof(OnBlockRemoved)) : null) {
                if (_protectedBlocks.Contains(block)) {
                    _protectedBlocks.Remove(block);
                }

                if (block.CubeGrid?.Physics != null) {
                    Mass = CalculateMass(CubeGrid);
                }
            }
        }

        private void OnPhysicsChanged(IMyEntity entity) {
            using (Mod.PROFILE ? Profiler.Measure(nameof(ProtectInfo), nameof(OnPhysicsChanged)) : null) {
                if (entity.Physics != null) {
                    Mass = CalculateMass((IMyCubeGrid) entity);
                }
            }
        }
    }
}