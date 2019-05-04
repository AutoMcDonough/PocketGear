using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI;
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
            var mass = cubeGrid.Physics.Mass;
            mass += MyAPIGateway.GridGroups.GetGroup(cubeGrid, GridLinkTypeEnum.Mechanical).Sum(x => x.Physics.Mass);
            return mass;
        }

        public void Close() {
            CubeGrid.OnBlockAdded -= OnBlockAdded;
            CubeGrid.OnBlockRemoved -= OnBlockRemoved;
            CubeGrid.OnPhysicsChanged -= OnPhysicsChanged;
        }

        public bool Contains(IMySlimBlock block) {
            return _protectedBlocks.Contains(block);
        }

        public void DisableProtection(IMySlimBlock slimBlock) {
            OnBlockRemoved(slimBlock);
        }

        public void EnableProtection(IMySlimBlock slimBlock) {
            OnBlockAdded(slimBlock);
        }

        public void RegisterNewAttack(IMyEntity attacker) {
            AttackStart = DateTime.UtcNow;
            Attacker = new AttackerInfo(attacker);
        }

        private void OnBlockAdded(IMySlimBlock block) {
            if (!_protectedBlocks.Contains(block)) {
                _protectedBlocks.Add(block);
            }

            if (block.CubeGrid?.Physics != null) {
                Mass = CalculateMass(CubeGrid);
            }
        }

        private void OnBlockRemoved(IMySlimBlock block) {
            if (_protectedBlocks.Contains(block)) {
                _protectedBlocks.Remove(block);
            }

            if (block.CubeGrid?.Physics != null) {
                Mass = CalculateMass(CubeGrid);
            }
        }

        private void OnPhysicsChanged(IMyEntity entity) {
            if (entity.Physics != null) {
                Mass = CalculateMass((IMyCubeGrid)entity);
            }
        }
    }
}