using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

// ReSharper disable UsePatternMatching

namespace AutoMcD.PocketGear.DamageSystem {
    public struct AttackerInfo {
        public AttackerInfo(IMyEntity entity) {
            LinearVelocity = GetLinearVelocity(entity);
        }

        public Vector3D LinearVelocity { get; }

        private static Vector3 GetLinearVelocity(IMyEntity entity) {
            if (entity.Physics != null) {
                return entity.Physics.LinearVelocity;
            }

            var cubeBlock = entity as IMyCubeBlock;
            if (cubeBlock != null) {
                return cubeBlock.CubeGrid.Physics.LinearVelocity;
            }

            return Vector3.Zero;
        }
    }
}